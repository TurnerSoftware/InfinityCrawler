using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfinityCrawler.Processing.Requests;
using InfinityCrawler.Tests.TestSite;

namespace InfinityCrawler.Tests
{
	public class CrawlerTestBase : TestBase
	{
		protected Crawler GetTestSiteCrawler(SiteContext siteContext)
		{
			var httpClient = TestSiteConfiguration.GetHttpClient(siteContext);
			return new Crawler(httpClient);
		}

		protected RequestProcessorOptions GetNoDelayRequestProcessorOptions()
		{
			return new RequestProcessorOptions
			{
				MaxNumberOfSimultaneousRequests = 10,
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
