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
		[TestMethod, Timeout(10000)]
		public async Task BasicSiteTest()
		{
			var crawler = GetTestSiteCrawler(new SiteContext
			{
				SiteFolder = "BasicSite"
			});
			var settings = GetFastCrawlTestSettings();

			var result = await crawler.Crawl(new Uri("http://localhost/"), settings);
		}
	}
}
