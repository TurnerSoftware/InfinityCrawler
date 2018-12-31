using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InfinityCrawler.Tests
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public async Task TestMethod1()
		{
			var crawler = new Crawler();
			var settings = new CrawlSettings
			{

			};
			settings.ParallelAsyncTaskOptions.BubbleUpExceptions = true;
			//settings.ParallelAsyncTaskOptions.DelayBetweenTaskStart = new TimeSpan();
			//settings.ParallelAsyncTaskOptions.DelayJitter = new TimeSpan();
			//settings.ParallelAsyncTaskOptions.TimeoutBeforeThrottle = new TimeSpan();
			//settings.ParallelAsyncTaskOptions.MaxNumberOfSimultaneousTasks = 10;
			var result = await crawler.Crawl(new Uri("http://www.plastykstudios.com.au/"), settings);

			var numberOfRequests = result.CrawledUris.Sum(c => c.Requests.Count + c.RedirectChain?.Sum(rc => rc.Requests.Count));
			var requestTimeTicks = result.CrawledUris.Sum(c => c.Requests.Sum(r => r.ElapsedTime.Ticks));
			var requestTimeSpan = new TimeSpan(requestTimeTicks);
			var requestsPerSecond = requestTimeSpan.TotalSeconds / Math.Max(1, numberOfRequests ?? 1);
		}
	}
}
