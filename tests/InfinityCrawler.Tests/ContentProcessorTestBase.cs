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
			using (var response = await httpClient.GetAsync(requestUri))
			{
				await response.Content.LoadIntoBufferAsync();
				using (var contentStream = await response.Content.ReadAsStreamAsync())
				{
					var headers = new CrawlHeaders(response.Headers, response.Content.Headers);
					return contentProcessor.Parse(requestUri, headers, contentStream);
				}
			}
		}
	}
}
