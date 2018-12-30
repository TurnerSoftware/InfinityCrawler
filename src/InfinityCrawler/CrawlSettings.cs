using System;
using System.Collections.Generic;
using System.Text;
using InfinityCrawler.LinkParser;

namespace InfinityCrawler
{
	public class CrawlSettings
	{
		public string UserAgent { get; set; }
		public int NumberOfRetries { get; set; }
		public IPageParser LinkParser { get; set; }

		public ParallelAsyncTaskOptions ParallelAsyncTaskOptions { get; set; } = new ParallelAsyncTaskOptions
		{
			MaxNumberOfSimultaneousTasks = 5,
			DelayBetweenTaskStart = new TimeSpan(0, 0, 0, 0, 500),
			DelayJitter = new TimeSpan(0, 0, 0, 1),
			ThrottlingRequestBackoff = new TimeSpan(0, 0, 0, 2),
			TimeoutBeforeThrottle = new TimeSpan(0, 0, 0, 2),
			MinSequentialSuccessesToMinimiseThrottling = 5
		};

		public Func<CrawledUri, bool> PageCrawlComplete { get; set; }


		public Func<CrawledUri, bool> AcceptCrawledUri { get; set; }
	}
}
