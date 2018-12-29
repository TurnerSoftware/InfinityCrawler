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
		public Func<CrawledUriResult, bool> PageCrawlComplete { get; set; }


		public Func<CrawledUriResult, bool> AcceptCrawledUri { get; set; }
	}
}
