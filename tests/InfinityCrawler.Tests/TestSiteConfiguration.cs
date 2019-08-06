using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using InfinityCrawler.Tests.TestSite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InfinityCrawler.Tests
{
	[TestClass]
	public static class TestSiteConfiguration
	{
		private static Dictionary<string, TestSiteManager> TestSites { get; } = new Dictionary<string, TestSiteManager>();

		public static HttpClient GetHttpClient(SiteContext siteContext)
		{
			if (!TestSites.ContainsKey(siteContext.SiteFolder))
			{
				var testSiteManager = new TestSiteManager(siteContext);
				TestSites.Add(siteContext.SiteFolder, testSiteManager);
			}

			return TestSites[siteContext.SiteFolder].GetHttpClient();
		}

		public static void ShutdownSites()
		{
			foreach (var site in TestSites.Values)
			{
				site.Dispose();
			}

			TestSites.Clear();
		}

		[AssemblyCleanup]
		public static void AssemblyCleanup()
		{
			ShutdownSites();
		}
	}
}
