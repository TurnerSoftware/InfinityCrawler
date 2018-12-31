using System;
using System.Linq;
using System.Threading.Tasks;
using InfinityCrawler.TaskHandlers;
using InfinityCrawler.Tests.TestSite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InfinityCrawler.Tests
{
	[TestClass]
	public class UnitTest1 : TestBase
	{
		[TestMethod]
		public async Task TestMethod1()
		{
			var taskHandlerLogger = GetLogger<ParallelAsyncTaskHandler>();
			var taskHandler = new ParallelAsyncTaskHandler(taskHandlerLogger);

			var crawler = new Crawler(taskHandler);
			var settings = new CrawlSettings
			{

			};
			settings.TaskHandlerOptions.BubbleUpExceptions = true;
			//settings.ParallelAsyncTaskOptions.DelayBetweenTaskStart = new TimeSpan();
			//settings.ParallelAsyncTaskOptions.DelayJitter = new TimeSpan();
			//settings.ParallelAsyncTaskOptions.TimeoutBeforeThrottle = new TimeSpan();
			//settings.ParallelAsyncTaskOptions.MaxNumberOfSimultaneousTasks = 10;
			var result = await crawler.Crawl(new Uri("http://www.example.com/"), settings);

			var numberOfRequests = result.CrawledUris.Sum(c => c.Requests.Count + (c.RedirectChain?.Sum(rc => rc.Requests.Count) ?? 0));
			var requestTimeTicks = result.CrawledUris.Sum(c => c.Requests.Sum(r => r.ElapsedTime.Ticks));
			var requestTimeSpan = new TimeSpan(requestTimeTicks);
			var requestsPerSecond = Math.Max(1, numberOfRequests) / requestTimeSpan.TotalSeconds;
		}

		[TestMethod]
		public async Task TestMethod2()
		{
			var taskHandlerLogger = GetLogger<ParallelAsyncTaskHandler>();
			var taskHandler = new ParallelAsyncTaskHandler(taskHandlerLogger);
			var siteContext = new SiteContext
			{
				SiteFolder = "TestSite1"
			};

			using (var testSite = new TestSiteManager(siteContext))
			{
				var client = testSite.GetHttpClient();
				var crawler = new Crawler(client, taskHandler);
				var settings = new CrawlSettings
				{

				};
				settings.TaskHandlerOptions.BubbleUpExceptions = true;
				//settings.ParallelAsyncTaskOptions.DelayBetweenTaskStart = new TimeSpan();
				//settings.ParallelAsyncTaskOptions.DelayJitter = new TimeSpan();
				//settings.ParallelAsyncTaskOptions.TimeoutBeforeThrottle = new TimeSpan();
				//settings.ParallelAsyncTaskOptions.MaxNumberOfSimultaneousTasks = 10;
				var result = await crawler.Crawl(new Uri("http://localhost/"), settings);

				var numberOfRequests = result.CrawledUris.Sum(c => c.Requests.Count + (c.RedirectChain?.Sum(rc => rc.Requests.Count) ?? 0));
				var requestTimeTicks = result.CrawledUris.Sum(c => c.Requests.Sum(r => r.ElapsedTime.Ticks));
				var requestTimeSpan = new TimeSpan(requestTimeTicks);
				var requestsPerSecond = Math.Max(1, numberOfRequests) / requestTimeSpan.TotalSeconds;
			}
		}
	}
}
