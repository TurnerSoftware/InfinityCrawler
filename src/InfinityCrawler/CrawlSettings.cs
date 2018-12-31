using System;
using System.Collections.Generic;
using System.Text;
using InfinityCrawler.ContentParsers;
using InfinityCrawler.TaskHandlers;

namespace InfinityCrawler
{
	public class CrawlSettings
	{
		public string UserAgent { get; set; } = "Mozilla/5.0 (Windows; U; Windows NT 6.1; rv:2.2) Gecko/20110201";
		public int NumberOfRetries { get; set; } = 3;
		public IEnumerable<string> HostAliases { get; set; }

		public IContentParser ContentParser { get; set; } = new SimpleContentParser();

		public TaskHandlerOptions TaskHandlerOptions { get; set; } = new TaskHandlerOptions
		{
			MaxNumberOfSimultaneousTasks = 10,
			DelayBetweenTaskStart = new TimeSpan(0, 0, 0, 0, 1000),
			DelayJitter = new TimeSpan(0, 0, 0, 0, 1000),
			ThrottlingRequestBackoff = new TimeSpan(0, 0, 0, 2),
			TimeoutBeforeThrottle = new TimeSpan(0, 0, 0, 5),
			MinSequentialSuccessesToMinimiseThrottling = 5
		};

		public Func<CrawledUri, bool> PageCrawlComplete { get; set; }


		public Func<CrawledUri, bool> AcceptCrawledUri { get; set; }
	}
}
