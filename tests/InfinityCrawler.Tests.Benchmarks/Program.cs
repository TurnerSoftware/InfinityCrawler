using System;
using BenchmarkDotNet.Running;

namespace InfinityCrawler.Tests.Benchmarks
{
	class Program
	{
		static void Main(string[] args)
		{
			BenchmarkRunner.Run<BasicSiteCrawlBenchmark>();
		}
	}
}
