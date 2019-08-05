using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InfinityCrawler.Processing.Requests;
using TurnerSoftware.RobotsExclusionTools;

namespace InfinityCrawler.Internal
{
	internal class CrawlRunner
	{
		public Uri BaseUri { get; }
		public CrawlSettings Settings { get; }

		private RobotsFile RobotsFile { get; }
		private HttpClient HttpClient { get; }
		
		private ConcurrentDictionary<Uri, UriCrawlState> UriCrawlStates { get; } = new ConcurrentDictionary<Uri, UriCrawlState>();
		private ConcurrentDictionary<Uri, byte> SeenUris { get; } = new ConcurrentDictionary<Uri, byte>();
		private ConcurrentBag<CrawledUri> CrawledUris { get; } = new ConcurrentBag<CrawledUri>();

		public CrawlRunner(Uri baseUri, RobotsFile robotsFile, HttpClient httpClient, CrawlSettings crawlSettings)
		{
			BaseUri = baseUri;
			RobotsFile = robotsFile;
			HttpClient = httpClient;
			Settings = crawlSettings;

			AddRequest(baseUri);
		}

		public void AddLink(CrawlLink crawlLink)
		{
			if (SeenUris.ContainsKey(crawlLink.Location))
			{
				return;
			}

			AddRequest(crawlLink.Location);
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
				AddRequest(redirectCrawlState.Location);
			}
		}

		public void AddResult(CrawledUri crawledUri)
		{
			CrawledUris.Add(crawledUri);
		}

		public void AddRequest(Uri requestUri)
		{
			if (!CheckUriValidity(requestUri))
			{
				return;
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

		private bool CheckUriValidity(Uri uriToCheck)
		{
			if (
				Settings.HostAliases != null &&
				!(
					uriToCheck.Host == BaseUri.Host ||
					Settings.HostAliases.Contains(uriToCheck.Host)
				)
			)
			{
				//Current host is not in the list of allowed hosts or matches base host
				return false;
			}
			else if (uriToCheck.Host != BaseUri.Host)
			{
				//Current host doesn't match base host
				return false;
			}

			return true;
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

					await responseAction(requestResult, crawlState);
				},
				Settings.RequestProcessorOptions,
				cancellationToken
			);

			return CrawledUris.ToArray();
		}
	}
}
