using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TurnerSoftware.RobotsExclusionTools;
using TurnerSoftware.SitemapTools;

namespace InfinityCrawler
{
	public class Crawler
	{
		private HttpClient HttpClient { get; }

		public Crawler()
		{
			HttpClient = new HttpClient(new HttpClientHandler
			{
				AllowAutoRedirect = false,
				UseCookies = false
			});
		}

		public Crawler(HttpClient httpClient)
		{
			HttpClient = httpClient;
		}

		public async Task<CrawlResult> Crawl(Uri siteUri, CrawlSettings settings)
		{
			var result = new CrawlResult
			{
				CrawlStart = DateTime.UtcNow
			};
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var baseUri = new Uri(siteUri.GetLeftPart(UriPartial.Authority));
			var robotsFile = await new RobotsParser(HttpClient).FromUriAsync(baseUri);

			var seedUris = new List<UriCrawlState>
			{
				new UriCrawlState { Location = baseUri }
			};

			//Use any links referred to by the sitemap as a starting point
			seedUris.AddRange((await new SitemapQuery(HttpClient)
				.GetAllSitemapsForDomain(siteUri.Host))
				.SelectMany(s => s.Urls.Select(u => new UriCrawlState { Location = u.Location }))
				.Distinct()
			);

			var crawledUris = new ConcurrentDictionary<Uri, CrawledUri>();

			await ParallelAsyncTask.For(seedUris.Distinct().ToArray(), async (crawlState, pagesToCrawl) =>
			{
				if (!CanCrawlUri(crawlState.Location, baseUri, crawledUris, settings))
				{
					return;
				}

				var lastRequest = crawlState.Requests.LastOrDefault();
				if (lastRequest != null && lastRequest.IsSuccessfulStatus)
				{
					return;
				}
				else if (crawlState.Requests.Count() == settings.NumberOfRetries)
				{
					crawledUris.TryAdd(crawlState.Location, new CrawledUri
					{
						Location = crawlState.Location,
						Status = CrawlStatus.MaxRetries,
						Requests = crawlState.Requests,
						RedirectChain = crawlState.Redirects
					});
				}
				else if (robotsFile.IsAllowedAccess(crawlState.Location, settings.UserAgent))
				{
					var crawledUri = await PerformRequest(crawlState, pagesToCrawl, settings);

					if (crawledUri != null)
					{
						crawledUris.TryAdd(crawlState.Location, crawledUri);

						if (crawledUri.Content?.Links?.Any() == true)
						{
							foreach (var crawlLink in crawledUri.Content.Links)
							{
								if (CanCrawlUri(crawlLink.Location, baseUri, crawledUris, settings))
								{
									pagesToCrawl.Enqueue(new UriCrawlState
									{
										Location = crawlLink.Location
									});
								}
							}
						}
					}
				}
				else
				{
					crawledUris.TryAdd(crawlState.Location, new CrawledUri
					{
						Location = crawlState.Location,
						Status = CrawlStatus.RobotsBlocked
					});
				}
			}, settings.ParallelAsyncTaskOptions);

			stopwatch.Stop();
			result.ElapsedTime = stopwatch.Elapsed;
			result.CrawledUris = crawledUris.Values;
			return result;
		}

		private bool CanCrawlUri(Uri uriToCheck, Uri baseUri, ConcurrentDictionary<Uri, CrawledUri> crawledUris, CrawlSettings settings)
		{
			if (
				settings.HostAliases != null &&
				!(
					uriToCheck.Host == baseUri.Host ||
					settings.HostAliases.Contains(uriToCheck.Host)
				)
			)
			{
				//Current host is not in the list of allowed hosts or matches base host
				return false;
			}
			else if (uriToCheck.Host != baseUri.Host)
			{
				//Current host doesn't match base host
				return false;
			}
			else if (crawledUris.ContainsKey(uriToCheck))
			{
				return false;
			}

			return true;
		}

		private async Task<CrawledUri> PerformRequest(UriCrawlState crawlState, ConcurrentQueue<UriCrawlState> pagesToCrawl, CrawlSettings settings)
		{
			var crawlRequest = new CrawlRequest
			{
				RequestStart = DateTime.UtcNow,
			};

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			using (var response = await HttpClient.GetAsync(crawlState.Location))
			{
				crawlRequest.StatusCode = response.StatusCode;
				crawlRequest.IsSuccessfulStatus = response.IsSuccessStatusCode;
				
				await response.Content.LoadIntoBufferAsync();

				stopwatch.Stop();
				crawlRequest.ElapsedTime = stopwatch.Elapsed;

				crawlState.Requests.Add(crawlRequest);

				var redirectStatusCodes = new[]
				{
					HttpStatusCode.MovedPermanently,
					HttpStatusCode.Redirect,
					HttpStatusCode.TemporaryRedirect
				};

				if (redirectStatusCodes.Contains(crawlRequest.StatusCode))
				{
					var headerLocation = response.Headers.Location;
					var redirectCrawlState = new UriCrawlState
					{
						Location = new Uri(crawlState.Location, headerLocation.ToString()),
						Redirects = crawlState.Redirects ?? new List<CrawledUriRedirect>()
					};

					redirectCrawlState.Redirects.Add(new CrawledUriRedirect
					{
						Location = crawlState.Location,
						Requests = crawlState.Requests
					});

					pagesToCrawl.Enqueue(redirectCrawlState);
					return null;
				}
				else if (crawlRequest.IsSuccessfulStatus)
				{
					return new CrawledUri
					{
						Location = crawlState.Location,
						Status = CrawlStatus.Crawled,
						RedirectChain = crawlState.Redirects,
						Requests = crawlState.Requests,
						Content = await settings.ContentParser.Parse(crawlState.Location, response, settings)
					};
				}
				else if ((int)crawlRequest.StatusCode >= 500 && (int)crawlRequest.StatusCode <= 599)
				{
					//On server errors, try to crawl the page again later
					pagesToCrawl.Enqueue(crawlState);
					return null;
				}
				else
				{
					//On any other error, just save what we have seen and move on
					//Consider the content of the request irrelevant
					return new CrawledUri
					{
						Location = crawlState.Location,
						Status = CrawlStatus.Crawled,
						RedirectChain = crawlState.Redirects,
						Requests = crawlState.Requests
					};
				}
			}
		}
	}
}
