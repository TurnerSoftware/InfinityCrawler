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
		public CrawledContent Parse(Uri requestUri, CrawlHeaders headers, Stream contentStream)
		{
			var crawledContent = new CrawledContent
			{
				ContentType = headers.ContentHeaders.ContentType?.MediaType,
				CharacterSet = headers.ContentHeaders.ContentType?.CharSet,
				ContentEncoding = headers.ContentHeaders.ContentEncoding != null ? string.Join(",", headers.ContentHeaders.ContentEncoding) : null
			};

			var document = new HtmlDocument();
			document.Load(contentStream);
			
			var pageRobotRules = new List<string>();
			if (headers.ResponseHeaders.Contains("X-Robots-Tag"))
			{
				var robotsHeaderValues = headers.ResponseHeaders.GetValues("X-Robots-Tag");
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
			crawledContent.CanonicalUri = GetCanonicalUri(document, requestUri);
			crawledContent.Links = GetLinks(document, requestUri).ToArray();

			return crawledContent;
		}
		
		private string GetBaseHref(HtmlDocument document)
		{
			var baseNode = document.DocumentNode.SelectSingleNode("html/head/base");
			return baseNode?.GetAttributeValue("href", string.Empty) ?? string.Empty;
		}

		private Uri GetCanonicalUri(HtmlDocument document, Uri requestUri)
		{
			var linkNodes = document.DocumentNode.SelectNodes("html/head/link");
			if (linkNodes != null)
			{
				var canonicalNode = linkNodes
					.Where(n => n.Attributes.Any(a => a.Name == "rel" && a.Value.Equals("canonical", StringComparison.InvariantCultureIgnoreCase)))
					.FirstOrDefault();
				if (canonicalNode != null)
				{
					var baseHref = GetBaseHref(document);
					var canonicalHref = canonicalNode.GetAttributeValue("href", null);
					return requestUri.BuildUriFromHref(canonicalHref, baseHref);
				}
			}

			return null;
		}

		private IEnumerable<CrawlLink> GetLinks(HtmlDocument document, Uri requestUri)
		{
			var anchorNodes = document.DocumentNode.SelectNodes("//a");
			if (anchorNodes != null)
			{
				var baseHref = GetBaseHref(document);
				
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
