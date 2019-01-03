using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using InfinityCrawler.TaskHandlers;
using InfinityCrawler.Tests.TestSite;

namespace InfinityCrawler.Tests.Benchmarks
{
	[CoreJob, ClrJob(baseline: true)]
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
			Crawler = new Crawler(client, new ParallelAsyncTaskHandler());
		}

		[GlobalSetup]
		public async Task Setup()
		{
			await CrawlSite(); // benchmark warmup as a workaround for https://github.com/dotnet/BenchmarkDotNet/issues/837
		}

		[Benchmark]
		public async Task CrawlSite()
		{
			var result = await Crawler.Crawl(Uri, new CrawlSettings
			{
				TaskHandlerOptions = new TaskHandlerOptions
				{
					MaxNumberOfSimultaneousTasks = 5,
					DelayBetweenTaskStart = new TimeSpan(),
					DelayJitter = new TimeSpan(),
					TimeoutBeforeThrottle = new TimeSpan()
				}
			});
		}
	}
}
