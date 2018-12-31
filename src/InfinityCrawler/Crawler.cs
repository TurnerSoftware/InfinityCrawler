using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using InfinityCrawler.TaskHandlers;
using TurnerSoftware.RobotsExclusionTools;
using TurnerSoftware.SitemapTools;

namespace InfinityCrawler
{
	public class Crawler
	{
		private HttpClient HttpClient { get; }
		private ITaskHandler TaskHandler { get; }

		public Crawler()
		{
			HttpClient = new HttpClient(new HttpClientHandler
			{
				AllowAutoRedirect = false,
				UseCookies = false
			});
			TaskHandler = new ParallelAsyncTaskHandler(null);
		}

		public Crawler(ITaskHandler taskHandler) : this()
		{
			TaskHandler = taskHandler ?? throw new ArgumentNullException(nameof(taskHandler));
		}

		public Crawler(HttpClient httpClient, ITaskHandler taskHandler)
		{
			HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			TaskHandler = taskHandler ?? throw new ArgumentNullException(nameof(taskHandler));
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

			var crawlContext = new CrawlContext
			{
				Settings = settings
			};

			await TaskHandler.For(seedUris.Distinct().ToArray(), async (crawlState, pagesToCrawl) =>
			{
				if (!CheckUriValidity(crawlState.Location, baseUri, crawlContext))
				{
					return;
				}

				if (crawlContext.CrawledUris.ContainsKey(crawlState.Location))
				{
					return;
				}

				crawlContext.SeenUris.TryAdd(crawlState.Location, 0);

				var lastRequest = crawlState.Requests.LastOrDefault();
				if (lastRequest != null && lastRequest.IsSuccessfulStatus)
				{
					return;
				}
				else if (crawlState.Requests.Count() == settings.NumberOfRetries)
				{
					crawlContext.CrawledUris.TryAdd(crawlState.Location, new CrawledUri
					{
						Location = crawlState.Location,
						Status = CrawlStatus.MaxRetries,
						Requests = crawlState.Requests,
						RedirectChain = crawlState.Redirects
					});
				}
				else if (robotsFile.IsAllowedAccess(crawlState.Location, settings.UserAgent))
				{
					var crawledUri = await PerformRequest(crawlState, pagesToCrawl, crawlContext);
					if (crawledUri != null)
					{
						crawlContext.CrawledUris.TryAdd(crawlState.Location, crawledUri);

						if (crawledUri.Content?.Links?.Any() == true)
						{
							foreach (var crawlLink in crawledUri.Content.Links)
							{
								if (CheckUriValidity(crawlLink.Location, baseUri, crawlContext))
								{
									if (crawlContext.SeenUris.ContainsKey(crawlLink.Location))
									{
										continue;
									}

									crawlContext.SeenUris.TryAdd(crawlLink.Location, 0);
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
					crawlContext.CrawledUris.TryAdd(crawlState.Location, new CrawledUri
					{
						Location = crawlState.Location,
						Status = CrawlStatus.RobotsBlocked
					});
				}
			}, settings.TaskHandlerOptions);

			stopwatch.Stop();
			result.ElapsedTime = stopwatch.Elapsed;
			result.CrawledUris = crawlContext.CrawledUris.Values;
			return result;
		}

		private bool CheckUriValidity(Uri uriToCheck, Uri baseUri, CrawlContext context)
		{
			if (
				context.Settings.HostAliases != null &&
				!(
					uriToCheck.Host == baseUri.Host ||
					context.Settings.HostAliases.Contains(uriToCheck.Host)
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

			return true;
		}

		private async Task<CrawledUri> PerformRequest(UriCrawlState crawlState, ConcurrentQueue<UriCrawlState> pagesToCrawl, CrawlContext context)
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
					context.SeenUris.TryAdd(headerLocation, 0);
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
						Content = await context.Settings.ContentParser.Parse(crawlState.Location, response, context.Settings)
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

		private class CrawlContext
		{
			public CrawlSettings Settings { get; set; }
			public ConcurrentDictionary<Uri, CrawledUri> CrawledUris { get; } = new ConcurrentDictionary<Uri, CrawledUri>();
			public ConcurrentDictionary<Uri, byte> SeenUris { get; } = new ConcurrentDictionary<Uri, byte>();
		}
	}
}
