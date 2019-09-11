using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using InfinityCrawler.Processing.Requests;
using InfinityCrawler.Tests.TestSite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InfinityCrawler.Tests
{
	[TestClass]
	public class BasicSiteTests : CrawlerTestBase
	{
		private async Task<CrawlResult> GetCrawlResult()
		{
			var crawler = GetTestSiteCrawler(new SiteContext
			{
				SiteFolder = "BasicSite"
			});
			var settings = new CrawlSettings
			{
				RequestProcessor = GetLoggedRequestProcessor(),
				RequestProcessorOptions = GetNoDelayRequestProcessorOptions()
			};
			return await crawler.Crawl(new Uri("http://localhost/"), settings);
		}

		[TestMethod]
		public async Task DiscoverIndexPageFromSitemap()
		{
			var result = await GetCrawlResult();
			var uri = new Uri("http://localhost/index.html");
			Assert.IsTrue(result.CrawledUris.Any(c => c.Location == uri));
		}

		[TestMethod]
		public async Task CrawledLinksOnIndexPage()
		{
			var result = await GetCrawlResult();
			var uri = new Uri("http://localhost/basic-page.html");
			Assert.IsTrue(result.CrawledUris.Any(c => c.Location == uri));
		}

		[TestMethod]
		public async Task ObeysRobotsBlocking()
		{
			var result = await GetCrawlResult();
			var uri = new Uri("http://localhost/robots-blocked.html");
			var crawledUri = result.CrawledUris.Where(c => c.Location == uri).FirstOrDefault();

			var robotsChildUri = new Uri("http://localhost/robots-blocked-childs.html");

			Assert.AreEqual(CrawlStatus.RobotsBlocked, crawledUri.Status);
			Assert.IsFalse(result.CrawledUris.Any(c => c.Location == robotsChildUri));
		}

		[TestMethod]
		public async Task UrisOnlyAppearOnceInResults()
		{
			var result = await GetCrawlResult();
			var uri = new Uri("http://localhost/index.html");
			Assert.AreEqual(1, result.CrawledUris.Count(c => c.Location == uri));
		}

		[TestMethod]
		public async Task UrisAreRetriedOnServerErrors()
		{
			var result = await GetCrawlResult();
			var uri = new Uri("http://localhost/status/500");
			var crawledUri = result.CrawledUris.Where(c => c.Location == uri).FirstOrDefault();
			Assert.AreEqual(3, crawledUri.Requests.Count);
		}

		[TestMethod]
		public async Task UrisAreNotRetriedOn4xxErrors()
		{
			var result = await GetCrawlResult();
			var uris = new[]
			{
				new Uri("http://localhost/status/404"),
				new Uri("http://localhost/status/403"),
				new Uri("http://localhost/status/401")
			};
			Assert.IsTrue(uris.All(uri => result.CrawledUris.Any(c => c.Location == uri && c.Requests.Count == 1)));
		}

		[TestMethod]
		public async Task ExternalSitesAreNotCrawled()
		{
			var result = await GetCrawlResult();
			var uri = new Uri("http://localhost/index.html");
			var crawledUri = result.CrawledUris.Where(c => c.Location == uri).FirstOrDefault();

			var externalUri = new Uri("http://not-allowed-domain.com");

			Assert.IsTrue(crawledUri.Content.Links.Any(l => l.Location == externalUri));
			Assert.IsFalse(result.CrawledUris.Any(c => c.Location == externalUri));
		}
		
		[TestMethod]
		public async Task AllowedExternalSitesAreCrawled()
		{
			var crawler = GetTestSiteCrawler(new SiteContext
			{
				SiteFolder = "BasicSite"
			});
			var settings = new CrawlSettings
			{
				HostAliases = new[] { "test-domain.com" },
				RequestProcessor = GetLoggedRequestProcessor(),
				RequestProcessorOptions = GetNoDelayRequestProcessorOptions()
			};
			var result = await crawler.Crawl(new Uri("http://localhost/"), settings);
			var uri = new Uri("http://localhost/index.html");
			var crawledUri = result.CrawledUris.Where(c => c.Location == uri).FirstOrDefault();

			var externalUri = new Uri("http://test-domain.com");

			Assert.IsTrue(crawledUri.Content.Links.Any(l => l.Location == externalUri));

			var externalCrawl = result.CrawledUris.FirstOrDefault(c => c.Location == externalUri);
			Assert.IsNotNull(externalCrawl);
			Assert.AreEqual(HttpStatusCode.OK, externalCrawl.Requests.LastOrDefault().StatusCode);
		}

		[TestMethod]
		public async Task RelNoFollowLinksAreIgnored()
		{
			var result = await GetCrawlResult();
			var uri = new Uri("http://localhost/index.html?v=rel-no-follow");
			Assert.AreEqual(0, result.CrawledUris.Count(c => c.Location == uri));
		}

		[TestMethod]
		public async Task MaximumRedirectLimitFollowed()
		{
			var result = await GetCrawlResult();
			var uri = new Uri("http://localhost/redirect/2/five-redirects");
			var crawledUri = result.CrawledUris.Where(c => c.Location == uri).FirstOrDefault();

			Assert.AreEqual(CrawlStatus.MaxRedirects, crawledUri.Status);
			Assert.AreEqual(3, crawledUri.RedirectChain.Count);
		}
		
		[TestMethod]
		public async Task MaximumPagesCrawledFollowed()
		{
			var crawler = GetTestSiteCrawler(new SiteContext
			{
				SiteFolder = "BasicSite"
			});
			var settings = new CrawlSettings
			{
				RequestProcessor = GetLoggedRequestProcessor(),
				RequestProcessorOptions = GetNoDelayRequestProcessorOptions()
			};

			settings.MaxNumberOfPagesToCrawl = 4;
			var result = await crawler.Crawl(new Uri("http://localhost/"), settings);
			Assert.AreEqual(4, result.CrawledUris.Count());

			settings.MaxNumberOfPagesToCrawl = 2;
			result = await crawler.Crawl(new Uri("http://localhost/"), settings);
			Assert.AreEqual(2, result.CrawledUris.Count());
		}
		
		[TestMethod]
		public async Task AutoRetryOnFailure()
		{
			var crawler = GetTestSiteCrawler(new SiteContext
			{
				SiteFolder = "EmptySite"
			});
			var settings = new CrawlSettings
			{
				NumberOfRetries = 3,
				RequestProcessor = GetLoggedRequestProcessor(),
				RequestProcessorOptions = new RequestProcessorOptions
				{
					DelayBetweenRequestStart = new TimeSpan(),
					MaxNumberOfSimultaneousRequests = 4,
					TimeoutBeforeThrottle = new TimeSpan(),
					DelayJitter = new TimeSpan(),
					RequestTimeout = new TimeSpan(0, 0, 0, 0, 150)
				}
			};

			settings.RequestProcessor.Add(new Uri("http://localhost/delay/300/300ms-delay-1"));
			settings.RequestProcessor.Add(new Uri("http://localhost/delay/300/300ms-delay-2"));
			settings.RequestProcessor.Add(new Uri("http://localhost/delay/300/300ms-delay-3"));
			settings.RequestProcessor.Add(new Uri("http://localhost/delay/300/300ms-delay-4"));

			var results = await crawler.Crawl(new Uri("http://localhost/"), settings);
			var delayedCrawls = results.CrawledUris.Where(c => c.Location.PathAndQuery.Contains("delay")).ToArray();

			foreach (var crawledUri in delayedCrawls)
			{
				Assert.AreEqual(CrawlStatus.MaxRetries, crawledUri.Status);
				Assert.IsNull(crawledUri.Content);
			}
		}
	}
}
