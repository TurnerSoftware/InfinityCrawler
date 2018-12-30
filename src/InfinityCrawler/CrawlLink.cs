using System;
using System.Collections.Generic;
using System.Text;

namespace InfinityCrawler
{
	public class CrawlLink
	{
		public Uri Location { get; set; }
		public string Title { get; set; }
		public string Text { get; set; }
		public string Relationship { get; set; }
	}
}
