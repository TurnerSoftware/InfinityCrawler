using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace InfinityCrawler.Processing.Content
{
	public interface IContentProcessor
	{
		CrawledContent Parse(Uri requestUri, HttpResponseHeaders responseHeaders, HttpContentHeaders contentHeaders, Stream contentStream);
	}
}
