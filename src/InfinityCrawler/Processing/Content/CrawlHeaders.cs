using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace InfinityCrawler.Processing.Content
{
	public class CrawlHeaders
	{
		public HttpResponseHeaders ResponseHeaders { get; }
		public HttpContentHeaders ContentHeaders { get; }
		
		public CrawlHeaders(HttpResponseHeaders responseHeaders, HttpContentHeaders contentHeaders)
		{
			ResponseHeaders = responseHeaders;
			ContentHeaders = contentHeaders;
		}
	}
}
