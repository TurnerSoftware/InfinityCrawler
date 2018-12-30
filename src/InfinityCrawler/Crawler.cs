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
				if (
					settings.HostAliases != null && 
					!(
						crawlState.Location.Host == baseUri.Host ||
						settings.HostAliases.Contains(crawlState.Location.Host)
					)
				)
				{
					//Current host is not in the list of allowed hosts or matches base host
					return;
				}
				else if (crawlState.Location.Host != baseUri.Host)
				{
					//Current host doesn't match base host
					return;
				}

				var lastRequest = crawlState.Requests.LastOrDefault();
				if (lastRequest.IsSuccessfulStatus)
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
					crawledUris.TryAdd(crawlState.Location, crawledUri);
					
					if (crawledUri.Content?.Links?.Any() == true)
					{
						foreach (var crawlLink in crawledUri.Content.Links)
						{
							if (!crawledUris.ContainsKey(crawlLink.Location))
							{
								pagesToCrawl.Enqueue(new UriCrawlState
								{
									Location = crawlLink.Location
								});
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

				if (!crawlRequest.IsSuccessfulStatus)
				{
					pagesToCrawl.Enqueue(crawlState);
					return null;
				}
				else if (response.StatusCode == HttpStatusCode.MovedPermanently || response.StatusCode == HttpStatusCode.Redirect)
				{
					var redirectCrawlState = new UriCrawlState
					{
						Location = response.Headers.Location,
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
				else
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
			}
		}
	}
}
