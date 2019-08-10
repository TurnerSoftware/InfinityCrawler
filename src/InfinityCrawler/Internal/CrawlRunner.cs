using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InfinityCrawler.Processing.Requests;
using Microsoft.Extensions.Logging;
using TurnerSoftware.RobotsExclusionTools;

namespace InfinityCrawler.Internal
{
	internal class CrawlRunner
	{
		public Uri BaseUri { get; }
		public CrawlSettings Settings { get; }

		private RobotsFile RobotsFile { get; }
		private HttpClient HttpClient { get; }
		
		private ILogger Logger { get; }
		
		private ConcurrentDictionary<Uri, UriCrawlState> UriCrawlStates { get; } = new ConcurrentDictionary<Uri, UriCrawlState>();
		private ConcurrentDictionary<Uri, byte> SeenUris { get; } = new ConcurrentDictionary<Uri, byte>();
		private ConcurrentBag<CrawledUri> CrawledUris { get; } = new ConcurrentBag<CrawledUri>();

		public CrawlRunner(Uri baseUri, RobotsFile robotsFile, HttpClient httpClient, CrawlSettings crawlSettings, ILogger logger = null)
		{
			BaseUri = baseUri;
			RobotsFile = robotsFile;
			HttpClient = httpClient;
			Settings = crawlSettings;

			Logger = logger;

			AddRequest(baseUri);
		}

		public void AddLink(CrawlLink crawlLink)
		{
			if (crawlLink.Relationship != null && crawlLink.Relationship.Equals("nofollow", StringComparison.InvariantCultureIgnoreCase))
			{
				return;
			}

			if (SeenUris.ContainsKey(crawlLink.Location))
			{
				return;
			}

			AddRequest(crawlLink.Location, false);
		}

		public void AddRedirect(Uri requestUri, Uri redirectUri)
		{
			if (UriCrawlStates.TryRemove(requestUri, out var crawlState))
			{
				var redirectCrawlState = new UriCrawlState
				{
					Location = new Uri(requestUri, redirectUri.ToString()),
					Redirects = crawlState.Redirects ?? new List<CrawledUriRedirect>()
				};
				redirectCrawlState.Redirects.Add(new CrawledUriRedirect
				{
					Location = crawlState.Location,
					Requests = crawlState.Requests
				});

				UriCrawlStates.TryAdd(redirectCrawlState.Location, redirectCrawlState);
				AddRequest(redirectCrawlState.Location, true);
			}
		}

		public void AddResult(Uri requestUri, CrawledContent content)
		{
			if (UriCrawlStates.TryGetValue(requestUri, out var crawlState))
			{
				if (content != null)
				{
					//TODO: PageRobotRules should be run through the RobotsParser
					var noIndex = content.PageRobotRules.Any(s => s.Equals("noindex", StringComparison.InvariantCultureIgnoreCase));
					if (noIndex)
					{
						AddResult(new CrawledUri
						{
							Location = crawlState.Location,
							Status = CrawlStatus.RobotsBlocked,
							Requests = crawlState.Requests,
							RedirectChain = crawlState.Redirects
						});
						return;
					}

					var noFollow = content.PageRobotRules.Any(s => s.Equals("nofollow", StringComparison.InvariantCultureIgnoreCase));
					if (!noFollow)
					{
						foreach (var crawlLink in content.Links)
						{
							AddLink(crawlLink);
						}
					}
				}

				AddResult(new CrawledUri
				{
					Location = crawlState.Location,
					Status = CrawlStatus.Crawled,
					RedirectChain = crawlState.Redirects,
					Requests = crawlState.Requests,
					Content = content
				});
			}
		}

		public void AddRequest(Uri requestUri)
		{
			AddRequest(requestUri, false);
		}

		private void AddRequest(Uri requestUri, bool skipMaxPageCheck)
		{
			if (
				Settings.HostAliases != null &&
				!(
					requestUri.Host == BaseUri.Host ||
					Settings.HostAliases.Contains(requestUri.Host)
				)
			)
			{
				Logger?.LogDebug($"{requestUri.Host} is not in the list of allowed hosts.");
				return;
			}
			else if (requestUri.Host != BaseUri.Host)
			{
				Logger?.LogDebug($"{requestUri.Host} doesn't match the base host.");
				return;
			}

			if (!skipMaxPageCheck && Settings.MaxNumberOfPagesToCrawl > 0)
			{
				var expectedCrawlCount = CrawledUris.Count + Settings.RequestProcessor.PendingRequests;
				if (expectedCrawlCount == Settings.MaxNumberOfPagesToCrawl)
				{
					Logger?.LogDebug($"Page crawl limit blocks adding request for {requestUri}");
					return;
				}
			}

			SeenUris.TryAdd(requestUri, 0);

			if (UriCrawlStates.TryGetValue(requestUri, out var crawlState))
			{
				var lastRequest = crawlState.Requests.LastOrDefault();
				if (lastRequest != null && lastRequest.IsSuccessfulStatus)
				{
					return;
				}

				if (crawlState.Requests.Count() == Settings.NumberOfRetries)
				{
					AddResult(new CrawledUri
					{
						Location = crawlState.Location,
						Status = CrawlStatus.MaxRetries,
						Requests = crawlState.Requests,
						RedirectChain = crawlState.Redirects
					});
					return;
				}

				if (crawlState.Redirects != null && crawlState.Redirects.Count == Settings.MaxNumberOfRedirects)
				{
					AddResult(new CrawledUri
					{
						Location = crawlState.Location,
						RedirectChain = crawlState.Redirects,
						Status = CrawlStatus.MaxRedirects
					});
					return;
				}
			}

			if (RobotsFile.IsAllowedAccess(requestUri, Settings.UserAgent))
			{
				Settings.RequestProcessor.Add(requestUri);
			}
			else
			{
				AddResult(new CrawledUri
				{
					Location = requestUri,
					Status = CrawlStatus.RobotsBlocked
				});
			}
		}

		private void AddResult(CrawledUri result)
		{
			CrawledUris.Add(result);
		}

		public async Task<IEnumerable<CrawledUri>> ProcessAsync(
			Func<RequestResult, UriCrawlState, Task> responseAction,
			CancellationToken cancellationToken = default
		)
		{
			await Settings.RequestProcessor.ProcessAsync(
				HttpClient,
				async (requestResult) =>
				{
					var crawlState = UriCrawlStates.GetOrAdd(requestResult.RequestUri, new UriCrawlState
					{
						Location = requestResult.RequestUri
					});

					if (requestResult.ResponseMessage == null)
					{
						//Retry failed requests
						crawlState.Requests.Add(new CrawlRequest
						{
							RequestStart = requestResult.RequestStart,
							ElapsedTime = requestResult.ElapsedTime
						});
						AddRequest(requestResult.RequestUri);
					}
					else
					{
						await responseAction(requestResult, crawlState);
					}
				},
				Settings.RequestProcessorOptions,
				cancellationToken
			);

			return CrawledUris.ToArray();
		}
	}
}
