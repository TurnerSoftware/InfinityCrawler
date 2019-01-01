using System;
using System.Collections.Generic;
using System.Text;
using InfinityCrawler.TaskHandlers;
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

		protected ITaskHandler GetDefaultTaskHandler()
		{
			var taskHandlerLogger = GetLogger<ParallelAsyncTaskHandler>();
			return new ParallelAsyncTaskHandler(taskHandlerLogger);
		}

		protected Crawler GetTestSiteCrawler(SiteContext context)
		{
			if (TestSite != null)
			{
				throw new InvalidOperationException("Test site already active - use the crawler previously created");
			}

			TestSite = new TestSiteManager(context);
			var client = TestSite.GetHttpClient();
			return new Crawler(client, GetDefaultTaskHandler());
		}

		[TestCleanup]
		public void TestCleanup()
		{
			TestSite?.Dispose();
		}

		protected CrawlSettings GetTestSettings()
		{
			var settings = new CrawlSettings();
			settings.TaskHandlerOptions.BubbleUpExceptions = true;
			return settings;
		}

		protected CrawlSettings GetFastCrawlTestSettings()
		{
			var settings = GetTestSettings();
			settings.TaskHandlerOptions.DelayBetweenTaskStart = new TimeSpan(0, 0, 0, 0, 100);
			settings.TaskHandlerOptions.DelayJitter = new TimeSpan();
			settings.TaskHandlerOptions.TimeoutBeforeThrottle = new TimeSpan();
			return settings;
		}
	}
}
