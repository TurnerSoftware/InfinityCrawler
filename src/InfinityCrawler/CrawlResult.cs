using System;
using System.Collections.Generic;
using System.Text;

namespace InfinityCrawler
{
	public class CrawlResult
	{
		public DateTime CrawlStart { get; set; }
		public TimeSpan ElapsedTime { get; set; }
		public IEnumerable<CrawledUri> CrawledUris { get; set; }
	}
}
