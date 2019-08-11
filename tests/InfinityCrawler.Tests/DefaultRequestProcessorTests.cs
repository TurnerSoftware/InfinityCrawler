using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InfinityCrawler.Processing.Requests;
using InfinityCrawler.Tests.TestSite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InfinityCrawler.Tests
{
	[TestClass]
	public class DefaultRequestProcessorTests : TestBase
	{
		[TestMethod]
		public async Task ThrottlingTest()
		{
			var httpClient = TestSiteConfiguration.GetHttpClient(new SiteContext
			{
				SiteFolder = "DefaultRequestProcessor"
			});
			
			var processor = new DefaultRequestProcessor(GetLogger<DefaultRequestProcessor>());

			//Warmup
			processor.Add(new Uri("http://localhost/delay/50/warmup"));
			await processor.ProcessAsync(httpClient, requestResult => Task.CompletedTask, new RequestProcessorOptions
			{
				DelayJitter = new TimeSpan(),
				DelayBetweenRequestStart = new TimeSpan(0, 0, 0, 0, 50)
			});

			processor.Add(new Uri("http://localhost/delay/50/50ms-delay-1"));
			processor.Add(new Uri("http://localhost/delay/50/50ms-delay-2"));
			processor.Add(new Uri("http://localhost/delay/300/300ms-delay-1"));
			processor.Add(new Uri("http://localhost/delay/300/300ms-delay-2"));
			processor.Add(new Uri("http://localhost/delay/50/50ms-delay-3"));
			processor.Add(new Uri("http://localhost/delay/50/50ms-delay-4"));
			processor.Add(new Uri("http://localhost/delay/50/50ms-delay-5"));
			processor.Add(new Uri("http://localhost/delay/50/50ms-delay-6"));
			processor.Add(new Uri("http://localhost/delay/50/50ms-delay-7"));

			var results = new List<RequestResult>();
			await processor.ProcessAsync(httpClient, requestResult =>
			{
				results.Add(requestResult);
				return Task.CompletedTask;
			}, new RequestProcessorOptions
			{
				MaxNumberOfSimultaneousRequests = 1,
				MinSequentialSuccessesToMinimiseThrottling = 2,
				DelayBetweenRequestStart = new TimeSpan(),
				DelayJitter = new TimeSpan(),
				TimeoutBeforeThrottle = new TimeSpan(0, 0, 0, 0, 270),
				ThrottlingRequestBackoff = new TimeSpan(0, 0, 0, 0, 100)
			});

			Assert.AreEqual(0, results[0].RequestStartDelay);
			Assert.AreEqual(0, results[1].RequestStartDelay);
			Assert.AreEqual(0, results[2].RequestStartDelay);
			Assert.AreEqual(100, results[3].RequestStartDelay);
			Assert.AreEqual(200, results[4].RequestStartDelay);
			Assert.AreEqual(200, results[5].RequestStartDelay);
			Assert.AreEqual(100, results[6].RequestStartDelay);
			Assert.AreEqual(100, results[7].RequestStartDelay);
			Assert.AreEqual(0, results[8].RequestStartDelay);
		}

		[TestMethod]
		public async Task ProcessCancellationTest()
		{
			var httpClient = TestSiteConfiguration.GetHttpClient(new SiteContext
			{
				SiteFolder = "DefaultRequestProcessor"
			});

			var processor = new DefaultRequestProcessor(GetLogger<DefaultRequestProcessor>());

			processor.Add(new Uri("http://localhost/delay/300/300ms-delay-1"));
			processor.Add(new Uri("http://localhost/delay/300/300ms-delay-2"));
			processor.Add(new Uri("http://localhost/delay/300/300ms-delay-3"));
			processor.Add(new Uri("http://localhost/delay/300/300ms-delay-4"));

			var results = new ConcurrentBag<RequestResult>();
			var tokenSource = new CancellationTokenSource(300);

			try
			{
				await processor.ProcessAsync(httpClient, requestResult =>
				{
					results.Add(requestResult);
					return Task.CompletedTask;
				}, new RequestProcessorOptions
				{
					DelayBetweenRequestStart = new TimeSpan(),
					MaxNumberOfSimultaneousRequests = 2,
					TimeoutBeforeThrottle = new TimeSpan(),
					DelayJitter = new TimeSpan()
				}, tokenSource.Token);
			}
			catch (OperationCanceledException)
			{

			}

			Assert.AreNotEqual(3, results.Count);
			Assert.AreNotEqual(4, results.Count);
		}

		[TestMethod]
		public async Task RequestTimeoutTest()
		{
			var httpClient = TestSiteConfiguration.GetHttpClient(new SiteContext
			{
				SiteFolder = "DefaultRequestProcessor"
			});

			var processor = new DefaultRequestProcessor(GetLogger<DefaultRequestProcessor>());

			processor.Add(new Uri("http://localhost/delay/300/300ms-delay-1"));
			processor.Add(new Uri("http://localhost/delay/300/300ms-delay-2"));
			processor.Add(new Uri("http://localhost/delay/300/300ms-delay-3"));
			processor.Add(new Uri("http://localhost/delay/300/300ms-delay-4"));

			var results = new ConcurrentBag<RequestResult>();

			await processor.ProcessAsync(httpClient, requestResult =>
			{
				results.Add(requestResult);
				return Task.CompletedTask;
			}, new RequestProcessorOptions
			{
				DelayBetweenRequestStart = new TimeSpan(),
				MaxNumberOfSimultaneousRequests = 4,
				TimeoutBeforeThrottle = new TimeSpan(),
				DelayJitter = new TimeSpan(),
				RequestTimeout = new TimeSpan(0, 0, 0, 0, 150)
			});

			Assert.AreEqual(4, results.Count);

			foreach (var requestResult in results)
			{
				Assert.IsInstanceOfType(requestResult.Exception, typeof(OperationCanceledException));
			}
		}
		[TestMethod, ExpectedExceptionPattern(typeof(Exception), nameof(FaultedTaskThrowsException))]
		public async Task FaultedTaskThrowsException()
		{
			var httpClient = TestSiteConfiguration.GetHttpClient(new SiteContext
			{
				SiteFolder = "DefaultRequestProcessor"
			});

			var processor = new DefaultRequestProcessor(GetLogger<DefaultRequestProcessor>());

			processor.Add(new Uri("http://localhost/"));

			await processor.ProcessAsync(httpClient, requestResult =>
			{
				throw new Exception(nameof(FaultedTaskThrowsException));
			}, new RequestProcessorOptions
			{
				DelayBetweenRequestStart = new TimeSpan(),
				DelayJitter = new TimeSpan()
			});
		}
	}
}
