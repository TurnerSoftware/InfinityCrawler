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
		public async Task NoHrefLinksAreIgnored()
		{
			var crawledContent = await PerformRequestAsync("CrawlLinkContent.html");
			Assert.AreEqual(6, crawledContent.Links.Count());
			Assert.IsFalse(crawledContent.Links.Any(l => l.Text == "No Href"));
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
		public async Task MetaNoFollowParsed()
		{
			var crawledContent = await PerformRequestAsync("MetaNoFollow.html");

			Assert.IsTrue(crawledContent.PageRobotRules.Any(r => r.Equals("nofollow", StringComparison.InvariantCultureIgnoreCase)));
		}
		[TestMethod]
		public async Task MetaNoIndexParsed()
		{
			var crawledContent = await PerformRequestAsync("MetaNoIndex.html");

			Assert.IsTrue(crawledContent.PageRobotRules.Any(r => r.Equals("noindex", StringComparison.InvariantCultureIgnoreCase)));
		}
		[TestMethod]
		public async Task MetaNoIndexNoFollowParsed()
		{
			var crawledContent = await PerformRequestAsync("MetaNoIndexNoFollow.html");

			Assert.IsTrue(crawledContent.PageRobotRules.Any(r =>
				r.IndexOf("noindex", StringComparison.InvariantCultureIgnoreCase) != -1 &&
				r.IndexOf("nofollow", StringComparison.InvariantCultureIgnoreCase) != -1
			));
		}
		[TestMethod]
		public async Task MetaNoneParsed()
		{
			var crawledContent = await PerformRequestAsync("MetaNone.html");

			Assert.IsTrue(crawledContent.PageRobotRules.Any(r => r.Equals("none", StringComparison.InvariantCultureIgnoreCase)));
		}
	}
}
