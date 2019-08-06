using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using InfinityCrawler.Processing.Content;
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
		protected TestSiteManager TestSite { get; private set; }
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

		protected void InitialiseTestSite(SiteContext context)
		{
			if (TestSite == null)
			{
				TestSite = new TestSiteManager(context);
			}
		}

		[TestCleanup]
		public void TestCleanup()
		{
			TestSite?.Dispose();
		}
	}
}
