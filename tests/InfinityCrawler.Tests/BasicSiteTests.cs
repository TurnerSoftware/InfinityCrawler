using System;
using System.Linq;
using System.Threading.Tasks;
using InfinityCrawler.TaskHandlers;
using InfinityCrawler.Tests.TestSite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InfinityCrawler.Tests
{
	[TestClass]
	public class BasicSiteTests : TestBase
	{
		private async Task<CrawlResult> GetCrawlResult()
		{
			var crawler = GetTestSiteCrawler(new SiteContext
			{
				SiteFolder = "BasicSite"
			});
			var settings = GetFastCrawlTestSettings();
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
			var uri = new Uri("http://localhost/malformed.html");
			Assert.IsTrue(result.CrawledUris.Any(c => c.Location == uri));
		}

		[TestMethod]
		public async Task FoundLinkFromMalformedPaged()
		{
			var result = await GetCrawlResult();
			var uri = new Uri("http://localhost/malformed-child.html");
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

			var externalUri = new Uri("http://www.example.com");

			Assert.IsTrue(crawledUri.Content.Links.Any(l => l.Location == externalUri));
			Assert.IsFalse(result.CrawledUris.Any(c => c.Location == externalUri));
		}
	}
}
