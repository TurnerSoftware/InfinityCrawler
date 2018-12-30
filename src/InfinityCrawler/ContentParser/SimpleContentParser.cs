using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace InfinityCrawler.LinkParser
{
	public class SimpleContentParser : IContentParser
	{
		public async Task<CrawledContent> Parse(HttpResponseMessage response, CrawlSettings settings)
		{
			var crawledContent = new CrawledContent
			{
				ContentType = response.Content.Headers.ContentType.MediaType,
				CharacterSet = response.Content.Headers.ContentType.CharSet,
				ContentEncoding = string.Join(",", response.Content.Headers.ContentEncoding)
			};

			var contentStream = new MemoryStream();
			await(await response.Content.ReadAsStreamAsync()).CopyToAsync(contentStream);
			crawledContent.ContentStream = contentStream;
			contentStream.Seek(0, SeekOrigin.Begin);
			
			crawledContent.Links = await GetLinks(contentStream);

			return crawledContent;
		}

		private async Task<IEnumerable<CrawlLink>> GetLinks(Stream contentStream)
		{
			//TODO
			await Task.Yield();
			return null;
		}
	}
}
