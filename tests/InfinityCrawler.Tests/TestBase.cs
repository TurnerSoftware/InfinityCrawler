using System;
using System.Collections.Generic;
using System.Text;
using InfinityCrawler.Processing.Requests;
using InfinityCrawler.Tests.TestSite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InfinityCrawler.Tests
{
	[TestClass]
	public class TestBase
	{
		private TestSiteManager TestSite { get; set; }
		private ILoggerFactory LoggerFactory { get; }

		public TestBase()
		{
			var serviceProvider = new ServiceCollection()
				.AddLogging(builder =>
				{
					builder.AddFilter("InfinityCrawler", LogLevel.Trace);
					builder.AddConsole();
					builder.AddDebug();
				})
				.BuildServiceProvider();

			LoggerFactory = serviceProvider.GetService<ILoggerFactory>();
		}

		protected ILogger<T> GetLogger<T>()
		{
			return LoggerFactory.CreateLogger<T>();
		}

		protected Crawler GetTestSiteCrawler(SiteContext context)
		{
			if (TestSite != null)
			{
				throw new InvalidOperationException("Test site already active - use the crawler previously created");
			}

			TestSite = new TestSiteManager(context);
			var client = TestSite.GetHttpClient();
			return new Crawler(client);
		}

		[TestCleanup]
		public void TestCleanup()
		{
			TestSite?.Dispose();
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
