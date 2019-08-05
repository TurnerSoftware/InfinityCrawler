using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace InfinityCrawler
{
	public class CrawledUri
	{
		public Uri Location { get; set; }

		public CrawlStatus Status { get; set; }
		
		public IList<CrawledUriRedirect> RedirectChain { get; set; }
		public IList<CrawlRequest> Requests { get; set; }

		public CrawledContent Content { get; set; }
	}

	public enum CrawlStatus
	{
		Crawled,
		RobotsBlocked,
		MaxRetries,
		MaxRedirects
	}
	
	public class CrawledUriRedirect
	{
		public Uri Location { get; set; }
		public IList<CrawlRequest> Requests { get; set; }
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

		public string RawContent { get; set; }
		
		public Uri CanonicalUri { get; set; }
		public IEnumerable<CrawlLink> Links { get; set; } = Enumerable.Empty<CrawlLink>();
	}
}
