using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InfinityCrawler.Tests
{
	[TestClass]
	public class TestBase
	{
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
	}
}
