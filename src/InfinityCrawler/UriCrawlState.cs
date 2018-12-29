using System;
using System.Collections.Generic;
using System.Text;

namespace InfinityCrawler
{
	public class UriCrawlState
	{
		public Uri Location { get; set; }
		public IList<CrawlRequest> Requests { get; set; } = new List<CrawlRequest>();
		public IList<CrawledUriRedirect> Redirects { get; set; }
	}
}
