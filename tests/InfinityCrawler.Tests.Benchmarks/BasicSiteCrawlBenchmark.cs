using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using InfinityCrawler.Processing.Requests;
using InfinityCrawler.Tests.TestSite;

namespace InfinityCrawler.Tests.Benchmarks
{
	[SimpleJob(RuntimeMoniker.Net60)]
	[MemoryDiagnoser]
	public class BasicSiteCrawlBenchmark
	{
		private TestSiteManager TestSite { get; }
		private Crawler Crawler { get; }
		private Uri Uri { get; } = new Uri("http://localhost/");

		public BasicSiteCrawlBenchmark()
		{
			TestSite = new TestSiteManager(new SiteContext
			{
				SiteFolder = "BasicSite"
			});

			var client = TestSite.GetHttpClient();
			Crawler = new Crawler(client);
		}

		[GlobalSetup]
		public async Task Setup()
		{
			await CrawlSite(); // benchmark warmup as a workaround for https://github.com/dotnet/BenchmarkDotNet/issues/837
		}

		[Benchmark]
		public async Task CrawlSite()
		{
			_ = await Crawler.Crawl(Uri, new CrawlSettings
			{
				RequestProcessorOptions = new RequestProcessorOptions
				{
					MaxNumberOfSimultaneousRequests = 5,
					DelayBetweenRequestStart = new TimeSpan(),
					DelayJitter = new TimeSpan(),
					TimeoutBeforeThrottle = new TimeSpan()
				}
			});
		}
	}
}
