using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace InfinityCrawler.LinkParser
{
	public class SimpleContentParser : IContentParser
	{
		public async Task<CrawledContent> Parse(Uri uri, HttpResponseMessage response, CrawlSettings settings)
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

			var parsedContent = Parse(uri, contentStream);

			if (response.Headers.Contains("X-Robots-Tag"))
			{
				var robotsHeaderValues = response.Headers.GetValues("X-Robots-Tag");
				parsedContent.NoIndex = robotsHeaderValues.Any(r =>
					r.IndexOf("noindex", StringComparison.InvariantCultureIgnoreCase) != -1
				);
				parsedContent.NoFollow = robotsHeaderValues.Any(r =>
					r.IndexOf("nofollow", StringComparison.InvariantCultureIgnoreCase) != -1
				);
			}

			if (parsedContent.NoIndex)
			{
				crawledContent.ContentStream = null;
				contentStream.Dispose();
			}

			if (!parsedContent.NoFollow)
			{
				crawledContent.Links = parsedContent.Links;
			}

			return crawledContent;
		}

		private ParsedContent Parse(Uri uri, Stream contentStream)
		{
			var result = new ParsedContent();

			var document = new HtmlDocument();
			document.Load(contentStream);

			var robotsMetaNode = document.DocumentNode.SelectSingleNode("html/head/meta[@name=\"ROBOTS\"]");

			if (robotsMetaNode != null)
			{
				var robotsMetaContent = robotsMetaNode.GetAttributeValue("content", null);
				if (robotsMetaContent.IndexOf("noindex", StringComparison.InvariantCultureIgnoreCase) != -1)
				{
					result.NoIndex = true;
				}
				if (robotsMetaContent.IndexOf("nofollow", StringComparison.InvariantCultureIgnoreCase) != -1)
				{
					result.NoFollow = true;
					return result;
				}
			}

			var canonicalNode = document.DocumentNode.SelectSingleNode("html/head/link[@rel=\"canonical\"]");
			if (canonicalNode != null)
			{
				var canonicalHref = canonicalNode.GetAttributeValue("href", null);
				if (canonicalHref != null && Uri.TryCreate(canonicalHref, UriKind.Absolute, out var canonicalUri))
				{
					result.CanonicalUri = canonicalUri;
				}
			}

			string baseHref = null;
			var baseNode = document.DocumentNode.SelectSingleNode("html/head/base");
			if (baseNode != null)
			{
				baseHref = baseNode.GetAttributeValue("href", null);
			}
			
			var anchorNodes = document.DocumentNode.SelectNodes("//a");
			if (anchorNodes != null)
			{
				var crawledLinks = new List<CrawlLink>();
				foreach (var anchor in anchorNodes)
				{
					var href = anchor.GetAttributeValue("href", null);
					if (href == null)
					{
						continue;
					}

					var anchorLocation = uri.BuildUriFromHref(href, baseHref);
					if (anchorLocation == null)
					{
						//Invalid links are ignored
						continue;
					}

					crawledLinks.Add(new CrawlLink
					{
						Location = anchorLocation,
						Title = anchor.GetAttributeValue("title", null),
						Text = anchor.InnerText,
						Relationship = anchor.GetAttributeValue("rel", null),
					});
				}
				result.Links = crawledLinks;
			}

			return result;
		}

		private class ParsedContent
		{
			public bool NoIndex { get; set; }
			public bool NoFollow { get; set; }
			public Uri CanonicalUri { get; set; }
			public IEnumerable<CrawlLink> Links { get; set; }
		}
	}
}
