using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfinityCrawler.Processing.Content;
using InfinityCrawler.Tests.TestSite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InfinityCrawler.Tests
{
	[TestClass]
	public class DefaultContentProcessorTests : ContentProcessorTestBase
	{
		private async Task<CrawledContent> PerformRequestAsync(string path)
		{
			var requestUri = new UriBuilder("http://localhost/")
			{
				Path = path
			}.Uri;

			return await RequestAndProcessContentAsync(new SiteContext
			{
				SiteFolder = "DefaultContentProcessor"
			}, requestUri, new DefaultContentProcessor());
		}

		[TestMethod]
		public async Task NoMetaParsed()
		{
			var crawledContent = await PerformRequestAsync("CrawlLinkContent.html");
			Assert.AreEqual(0, crawledContent.PageRobotRules.Count());
		}

		[TestMethod]
		public async Task MissingHrefLinksAreIgnored()
		{
			var crawledContent = await PerformRequestAsync("CrawlLinkContent.html");
			Assert.AreEqual(6, crawledContent.Links.Count());
			Assert.IsFalse(crawledContent.Links.Any(l => l.Text == "No Href"));
		}
		
		[TestMethod]
		public async Task InvalidHrefLinksAreIgnored()
		{
			var crawledContent = await PerformRequestAsync("CrawlLinkContent.html");
			Assert.AreEqual(6, crawledContent.Links.Count());
			Assert.IsFalse(crawledContent.Links.Any(l => l.Text == "Invalid Href"));
		}

		[TestMethod]
		public async Task TitleAttributeIsParsed()
		{
			var crawledContent = await PerformRequestAsync("CrawlLinkContent.html");

			Assert.IsTrue(crawledContent.Links.Any(l => l.Title == "Title Attribute"));
			Assert.IsNull(crawledContent.Links.FirstOrDefault(l => l.Text == "Relative File").Title);
		}

		[TestMethod]
		public async Task RelAttributeIsParsed()
		{
			var crawledContent = await PerformRequestAsync("CrawlLinkContent.html");

			Assert.IsTrue(crawledContent.Links.Any(l => l.Relationship == "nofollow"));
			Assert.IsNull(crawledContent.Links.FirstOrDefault(l => l.Text == "Relative File").Relationship);
		}

		[TestMethod]
		public async Task MetaRobotsParsed()
		{
			var crawledContent = await PerformRequestAsync("MetaNoFollow.html");
			Assert.IsTrue(crawledContent.PageRobotRules.Any(r => r.Equals("nofollow", StringComparison.InvariantCultureIgnoreCase)));
			
			crawledContent = await PerformRequestAsync("MetaNoIndex.html");
			Assert.IsTrue(crawledContent.PageRobotRules.Any(r => r.Equals("noindex", StringComparison.InvariantCultureIgnoreCase)));

			crawledContent = await PerformRequestAsync("MetaNoIndexNoFollow.html");
			Assert.IsTrue(crawledContent.PageRobotRules.Any(r =>
				r.IndexOf("noindex", StringComparison.InvariantCultureIgnoreCase) != -1 &&
				r.IndexOf("nofollow", StringComparison.InvariantCultureIgnoreCase) != -1
			));

			crawledContent = await PerformRequestAsync("MetaNone.html");
			Assert.IsTrue(crawledContent.PageRobotRules.Any(r => r.Equals("none", StringComparison.InvariantCultureIgnoreCase)));
		}
		[TestMethod]
		public async Task HeaderRobotsParsed()
		{
			var crawledContent = await PerformRequestAsync("robots/header-page-noindex");
			Assert.IsTrue(crawledContent.PageRobotRules.Any(r => r.Equals("noindex", StringComparison.InvariantCultureIgnoreCase)));

			crawledContent = await PerformRequestAsync("robots/header-bot-specific");
			Assert.IsTrue(crawledContent.PageRobotRules.Any(r => r.Contains("onebot")));
			Assert.IsTrue(crawledContent.PageRobotRules.Any(r => r.Contains("twobot")));
		}

		[TestMethod]
		public async Task CanonicalUriParsing()
		{
			var crawledContent = await PerformRequestAsync("NoCanonicalUri.html");
			Assert.IsNull(crawledContent.CanonicalUri);

			crawledContent = await PerformRequestAsync("RelativeCanonicalUri.html");
			Assert.AreEqual(new Uri("http://localhost/RelativeCanonicalUri.html"), crawledContent.CanonicalUri);
			
			crawledContent = await PerformRequestAsync("AbsoluteCanonicalUri.html");
			Assert.AreEqual(new Uri("http://localhost/AbsoluteCanonicalUri.html"), crawledContent.CanonicalUri);
		}
		[TestMethod]
		public async Task BaseHrefLinks()
		{
			var crawledContent = await PerformRequestAsync("BaseHrefCrawlLink.html");
			var links = crawledContent.Links.ToArray();

			Assert.AreEqual(new Uri("http://test-domain.com/"), links[0].Location);
			Assert.AreEqual(new Uri("http://localhost/base/#RelativeFragment"), links[1].Location);
			Assert.AreEqual(new Uri("http://localhost/base/relative/RelativeFile.html"), links[2].Location);
			Assert.AreEqual(new Uri("http://localhost/base/relative/RelativeFile.html#Fragment"), links[3].Location);
			Assert.AreEqual(new Uri("http://localhost/RelativeBaseFile.html"), links[4].Location);
			Assert.AreEqual(new Uri("http://localhost/absolute/AbsoluteBaseFile.html"), links[5].Location);
		}
	}
}
