using System;
using System.Collections.Generic;
using System.Text;

namespace InfinityCrawler
{
	public class CrawledUriContext
	{
		public IList<CrawledUriRedirect> RedirectChain { get; set; } = new List<CrawledUriRedirect>();
	}
}
