using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace InfinityCrawler.Processing.Content
{
	public class DefaultContentProcessor : IContentProcessor
	{
		public CrawledContent Parse(Uri requestUri, HttpContentHeaders headers, Stream contentStream)
		{
			var crawledContent = new CrawledContent
			{
				ContentType = headers.ContentType.MediaType,
				CharacterSet = headers.ContentType.CharSet,
				ContentEncoding = string.Join(",", headers.ContentEncoding)
			};

			var noFollow = false;
			if (headers.Contains("X-Robots-Tag"))
			{
				var robotsHeaderValues = headers.GetValues("X-Robots-Tag");
				var noIndex = robotsHeaderValues.Any(r => r.IndexOf("noindex", StringComparison.InvariantCultureIgnoreCase) != -1);

				if (noIndex)
				{
					return null;
				}

				noFollow = robotsHeaderValues.Any(r =>
					r.IndexOf("nofollow", StringComparison.InvariantCultureIgnoreCase) != -1
				);
			}

			var document = new HtmlDocument();
			document.Load(contentStream);

			crawledContent.Links = GetLinks(document, requestUri).ToArray();

			var robotsMetaNode = document.DocumentNode.SelectSingleNode("html/head/meta[@name=\"ROBOTS\"]");
			if (robotsMetaNode != null)
			{
				var robotsMetaContent = robotsMetaNode.GetAttributeValue("content", null);

				var noIndex = robotsMetaContent.IndexOf("noindex", StringComparison.InvariantCultureIgnoreCase) != -1;
				if (noIndex)
				{
					return null;
				}

				noFollow = noFollow || robotsMetaContent.IndexOf("nofollow", StringComparison.InvariantCultureIgnoreCase) != -1;
				if (!noFollow)
				{
					crawledContent.Links = Enumerable.Empty<CrawlLink>();
				}
			}

			crawledContent.CanonicalUri = GetCanonicalUri(document);
			return crawledContent;
		}

		private IEnumerable<CrawlLink> GetLinks(HtmlDocument document, Uri requestUri)
		{
			var baseHref = string.Empty;
			var baseNode = document.DocumentNode.SelectSingleNode("html/head/base");
			if (baseNode != null)
			{
				baseHref = baseNode.GetAttributeValue("href", null);
			}

			var anchorNodes = document.DocumentNode.SelectNodes("//a");
			if (anchorNodes != null)
			{
				foreach (var anchor in anchorNodes)
				{
					var href = anchor.GetAttributeValue("href", null);
					if (href == null)
					{
						continue;
					}

					var anchorLocation = requestUri.BuildUriFromHref(href, baseHref);
					if (anchorLocation == null)
					{
						//Invalid links are ignored
						continue;
					}

					var rel = anchor.GetAttributeValue("rel", null);
					if (rel == null || !rel.Contains("nofollow"))
					{
						yield return new CrawlLink
						{
							Location = anchorLocation,
							Title = anchor.GetAttributeValue("title", null),
							Text = anchor.InnerText,
							Relationship = anchor.GetAttributeValue("rel", null),
						};
					}
				}
			}
		}

		private Uri GetCanonicalUri(HtmlDocument document)
		{
			var canonicalNode = document.DocumentNode.SelectSingleNode("html/head/link[@rel=\"canonical\"]");
			if (canonicalNode != null)
			{
				var canonicalHref = canonicalNode.GetAttributeValue("href", null);
				if (canonicalHref != null && Uri.TryCreate(canonicalHref, UriKind.Absolute, out var canonicalUri))
				{
					return canonicalUri;
				}
			}

			return null;
		}
	}
}
