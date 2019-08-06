using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfinityCrawler.Processing.Requests;

namespace InfinityCrawler.Tests
{
	public class CrawlerTestBase : TestBase
	{
		protected Crawler GetTestSiteCrawler()
		{
			if (TestSite == null)
			{
				throw new InvalidOperationException("Test site is not initialised!");
			}

			var client = TestSite.GetHttpClient();
			return new Crawler(client);
		}

		protected RequestProcessorOptions GetNoDelayRequestProcessorOptions()
		{
			return new RequestProcessorOptions
			{
				MaxNumberOfSimultaneousRequests = 5,
				DelayBetweenRequestStart = new TimeSpan(0, 0, 0, 0, 100),
				DelayJitter = new TimeSpan(),
				TimeoutBeforeThrottle = new TimeSpan()
			};
		}

		protected DefaultRequestProcessor GetLoggedRequestProcessor()
		{
			var requestProcessorLogger = GetLogger<DefaultRequestProcessor>();
			return new DefaultRequestProcessor(requestProcessorLogger);
		}
	}
}
