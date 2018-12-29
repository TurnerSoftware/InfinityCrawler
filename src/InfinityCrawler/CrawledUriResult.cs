using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace InfinityCrawler
{
	public class CrawledUriResult
	{
		public Uri Location { get; set; }

		public bool IsCrawlBlocked { get; set; }
		public string BlockReason { get; set; }
		
		public IEnumerable<CrawledUriRedirect> RedirectChain { get; set; }
		public IEnumerable<CrawlRequest> Requests { get; set; }

		public CrawledContent Content { get; set; }
	}
	
	public class CrawledUriRedirect
	{
		public Uri Location { get; set; }
		public IEnumerable<CrawlRequest> Requests { get; set; }
	}

	public class CrawlRequest
	{
		public DateTime RequestStart { get; set; }
		public TimeSpan ElapsedTime { get; set; }
		public HttpStatusCode StatusCode { get; set; }
		public bool IsSuccessfulStatus { get; set; }
	}

	public class CrawledContent
	{
		public string ContentType { get; set; }
		public string CharacterSet { get; set; }
		public string ContentEncoding { get; set; }

		public MemoryStream ContentStream { get; set; }
		public IEnumerable<CrawlLink> Links { get; set; }
	}
}
