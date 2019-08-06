using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfinityCrawler.Processing.Content;

namespace InfinityCrawler.Tests
{
	public class ContentProcessorTestBase : TestBase
	{
		protected async Task<CrawledContent> RequestAndProcessContentAsync(Uri requestUri, IContentProcessor contentProcessor)
		{
			var response = await TestSite.GetHttpClient().GetAsync(requestUri);
			await response.Content.LoadIntoBufferAsync();
			var contentStream = await response.Content.ReadAsStreamAsync();
			return contentProcessor.Parse(requestUri, response.Content.Headers, contentStream);
		}
	}
}
