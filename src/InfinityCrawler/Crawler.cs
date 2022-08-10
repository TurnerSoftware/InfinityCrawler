using InfinityCrawler.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using InfinityCrawler.Processing.Requests;
using TurnerSoftware.RobotsExclusionTools;
using TurnerSoftware.SitemapTools;
using Microsoft.Extensions.Logging;
using InfinityCrawler.Processing.Content;

namespace InfinityCrawler
{
	public class Crawler
	{
		private HttpClient HttpClient { get; }
		private ILogger Logger { get; }

		public Crawler()
		{
			HttpClient = new HttpClient(new HttpClientHandler
			{
				AllowAutoRedirect = false,
				UseCookies = false
			});
		}

		public Crawler(HttpClient httpClient, ILogger logger = null)
		{
			HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			Logger = logger;
		}

		public async Task<CrawlResult> Crawl(Uri siteUri, CrawlSettings settings)
		{
			var result = new CrawlResult
			{
				CrawlStart = DateTime.UtcNow
			};
			var overallCrawlStopwatch = new Stopwatch();
			overallCrawlStopwatch.Start();

			var baseUri = new Uri(siteUri.GetLeftPart(UriPartial.Authority));
			var robotsFile = await new RobotsFileParser(HttpClient).FromUriAsync(baseUri);

			UpdateCrawlDelay(robotsFile, settings.UserAgent, settings.RequestProcessorOptions);

			var crawlRunner = new CrawlRunner(baseUri, robotsFile, HttpClient, settings, Logger);

			//Use any links referred to by the sitemap as a starting point
			var urisFromSitemap = (await new SitemapQuery(HttpClient)
				.GetAllSitemapsForDomainAsync(siteUri.Host))
				.SelectMany(s => s.Urls.Select(u => u.Location).Distinct());
			foreach (var uri in urisFromSitemap)
			{
				crawlRunner.AddRequest(uri);
			}

			result.CrawledUris = await crawlRunner.ProcessAsync(async (requestResult, crawlState) =>
			{
				using (requestResult.Content)
				{
					var headers = new CrawlHeaders(requestResult.ResponseHeaders, requestResult.ContentHeaders);
					var content = settings.ContentProcessor.Parse(crawlState.Location, headers, requestResult.Content);
					requestResult.Content.Seek(0, SeekOrigin.Begin);
					content.RawContent = await new StreamReader(requestResult.Content).ReadToEndAsync();
					crawlRunner.AddResult(crawlState.Location, content);
				}
			});

			overallCrawlStopwatch.Stop();
			result.ElapsedTime = overallCrawlStopwatch.Elapsed;
			return result;
		}

		private void UpdateCrawlDelay(RobotsFile robotsFile, string userAgent, RequestProcessorOptions requestProcessorOptions)
		{
			var minimumCrawlDelayInMilliseconds = 0;

			//Apply Robots.txt crawl-delay (if defined)
			if (robotsFile.TryGetEntryForUserAgent(userAgent, out var accessEntry))
			{
				minimumCrawlDelayInMilliseconds = accessEntry.CrawlDelay ?? 0 * 1000;
			}

			var taskDelay = Math.Max(minimumCrawlDelayInMilliseconds, requestProcessorOptions.DelayBetweenRequestStart.TotalMilliseconds);
			requestProcessorOptions.DelayBetweenRequestStart = new TimeSpan(0, 0, 0, 0, (int)taskDelay);
		}
	}
}
