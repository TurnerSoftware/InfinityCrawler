using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfinityCrawler.Processing.Content;
using InfinityCrawler.Tests.TestSite;

namespace InfinityCrawler.Tests
{
	public class ContentProcessorTestBase : TestBase
	{
		protected async Task<CrawledContent> RequestAndProcessContentAsync(SiteContext siteContext, Uri requestUri, IContentProcessor contentProcessor)
		{
			var httpClient = TestSiteConfiguration.GetHttpClient(siteContext);
			var response = await httpClient.GetAsync(requestUri);
			await response.Content.LoadIntoBufferAsync();
			var contentStream = await response.Content.ReadAsStreamAsync();
			return contentProcessor.Parse(requestUri, response.Content.Headers, contentStream);
		}
	}
}
