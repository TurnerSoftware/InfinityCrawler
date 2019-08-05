using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using InfinityCrawler.Processing.Content;
using InfinityCrawler.Processing.Requests;

namespace InfinityCrawler
{
	public class CrawlSettings
	{
		public string UserAgent { get; set; } = "Mozilla/5.0 (Windows; U; Windows NT 6.1; rv:2.2) Gecko/20110201";
		public IEnumerable<string> HostAliases { get; set; }
		public int NumberOfRetries { get; set; } = 3;
		public int MaxNumberOfRedirects { get; set; } = 3;
		public int MaxNumberOfPagesToCrawl { get; set; }

		public IContentProcessor ContentProcessor { get; set; } = new DefaultContentProcessor();
		public IRequestProcessor RequestProcessor { get; set; } = new DefaultRequestProcessor();
		public RequestProcessorOptions RequestProcessorOptions { get; set; } = new RequestProcessorOptions();

		public Func<CrawledUri, bool> PageCrawlComplete { get; set; }
		public Func<CrawledUri, bool> AcceptCrawledUri { get; set; }
	}
}
