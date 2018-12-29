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
		public Func<CrawledUri, bool> PageCrawlComplete { get; set; }


		public Func<CrawledUri, bool> AcceptCrawledUri { get; set; }
	}
}
