using System;
using System.Collections.Generic;
using System.Text;
using InfinityCrawler.LinkParser;

namespace InfinityCrawler
{
	public class CrawlSettings
	{
		public string UserAgent { get; set; } = "Mozilla/5.0 (Windows; U; Windows NT 6.1; rv:2.2) Gecko/20110201";
		public int NumberOfRetries { get; set; } = 3;

		public IContentParser ContentParser { get; set; } = new SimpleContentParser();

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
