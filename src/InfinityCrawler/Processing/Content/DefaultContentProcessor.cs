using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using InfinityCrawler.Internal;

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

			var document = new HtmlDocument();
			document.Load(contentStream);
			
			var pageRobotRules = new List<string>();
			if (headers.Contains("X-Robots-Tag"))
			{
				var robotsHeaderValues = headers.GetValues("X-Robots-Tag");
				pageRobotRules.AddRange(robotsHeaderValues);
			}

			var metaNodes = document.DocumentNode.SelectNodes("html/head/meta");
			if (metaNodes != null)
			{
				var robotsMetaValue = metaNodes
					.Where(n => n.Attributes.Any(a => a.Name == "name" && a.Value.Equals("robots", StringComparison.InvariantCultureIgnoreCase)))
					.SelectMany(n => n.Attributes.Where(a => a.Name == "content").Select(a => a.Value))
					.FirstOrDefault();
				if (robotsMetaValue != null)
				{
					pageRobotRules.Add(robotsMetaValue);
				}
			}

			crawledContent.PageRobotRules = pageRobotRules.ToArray();
			crawledContent.CanonicalUri = GetCanonicalUri(document);
			crawledContent.Links = GetLinks(document, requestUri).ToArray();

			return crawledContent;
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
}
