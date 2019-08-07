using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace InfinityCrawler.Processing.Requests
{
	public class DefaultRequestProcessor : IRequestProcessor
	{
		private ILogger Logger { get; }
		private ConcurrentQueue<Uri> RequestQueue { get; } = new ConcurrentQueue<Uri>();

		public DefaultRequestProcessor(ILogger logger = null)
		{
			Logger = logger;
		}

		public void Add(Uri uri)
		{
			RequestQueue.Enqueue(uri);
		}

		public async Task ProcessAsync(HttpClient httpClient, Func<RequestResult, Task> responseAction, RequestProcessorOptions options, CancellationToken cancellationToken = default)
		{
			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			var random = new Random();
			var activeRequests = new ConcurrentDictionary<Task, RequestContext>(options.MaxNumberOfSimultaneousRequests, options.MaxNumberOfSimultaneousRequests);

			var currentBackoff = 0;
			var successesSinceLastThrottle = 0;
			var requestCount = 0;

			while (activeRequests.Count > 0 || !RequestQueue.IsEmpty)
			{
				while (!RequestQueue.IsEmpty)
				{
					if (RequestQueue.TryDequeue(out var requestUri))
					{
						var requestStartDelay = 0d;
						//Request delaying and backoff
						if (options.DelayBetweenRequestStart.TotalMilliseconds > 0)
						{
							requestStartDelay = options.DelayBetweenRequestStart.TotalMilliseconds;
							requestStartDelay += random.NextDouble() * options.DelayJitter.TotalMilliseconds;
						}

						requestStartDelay += currentBackoff;

						var requestContext = new RequestContext
						{
							RequestNumber = requestCount + 1,
							Timer = new Stopwatch(),
							RequestStartDelay = requestStartDelay,
							TimeoutToken = new CancellationTokenSource(options.RequestTimeout).Token
						};
						var task = PerformRequestAsync(httpClient, requestUri, responseAction, requestContext);

						Logger?.LogDebug($"Request #{requestContext.RequestNumber} started with {requestStartDelay}ms delay");

						activeRequests.TryAdd(task, requestContext);
						requestCount++;

						if (cancellationToken.IsCancellationRequested)
						{
							Logger?.LogInformation($"Cancellation has been requested for processing");
							await Task.WhenAll(activeRequests.Keys);
							return;
						}

						if (activeRequests.Count == options.MaxNumberOfSimultaneousRequests)
						{
							break;
						}
					}
				}

				await Task.WhenAny(activeRequests.Keys).ConfigureAwait(false);

				var completedRequests = activeRequests.Keys.Where(t => t.IsCompleted);
				foreach (var completedRequest in completedRequests)
				{
					activeRequests.TryRemove(completedRequest, out var requestContext);

					if (completedRequest.IsFaulted)
					{
						throw completedRequest.Exception;
					}

					Logger?.LogDebug($"Request #{requestContext.RequestNumber} completed in {requestContext.Timer.ElapsedMilliseconds}ms");

					//Manage the throttling based on timeouts and successes
					var throttlePoint = options.TimeoutBeforeThrottle;
					if (throttlePoint.TotalMilliseconds > 0 && requestContext.Timer.Elapsed > throttlePoint)
					{
						successesSinceLastThrottle = 0;
						currentBackoff += (int)options.ThrottlingRequestBackoff.TotalMilliseconds;
						Logger?.LogInformation($"Increased backoff to {currentBackoff}ms");
					}
					else if (currentBackoff > 0)
					{
						successesSinceLastThrottle += 1;
						if (successesSinceLastThrottle == options.MinSequentialSuccessesToMinimiseThrottling)
						{
							var newBackoff = currentBackoff - options.ThrottlingRequestBackoff.TotalMilliseconds;
							currentBackoff = Math.Max(0, (int)newBackoff);
							successesSinceLastThrottle = 0;
							Logger?.LogInformation($"Decreased backoff to {currentBackoff}ms");
						}
					}
				}
			}

			Logger?.LogDebug($"Completed processing {requestCount} requests");
		}

		private async Task PerformRequestAsync(HttpClient httpClient, Uri requestUri, Func<RequestResult, Task> responseAction, RequestContext context)
		{
			if (context.RequestStartDelay > 0)
			{
				await Task.Delay((int)context.RequestStartDelay);
			}

			var requestStart = DateTime.UtcNow;
			context.Timer.Start();

			try
			{
				using (var response = await httpClient.GetAsync(requestUri, context.TimeoutToken))
				{
					await response.Content.LoadIntoBufferAsync();

					//We only want to time the request, not the handling of the response
					context.Timer.Stop();

					await responseAction(new RequestResult
					{
						RequestUri = requestUri,
						RequestStart = requestStart,
						RequestStartDelay = context.RequestStartDelay,
						ResponseMessage = response,
						ElapsedTime = context.Timer.Elapsed
					});
				}
			}
			catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
			{
				context.Timer.Stop();

				await responseAction(new RequestResult
				{
					RequestUri = requestUri,
					RequestStart = requestStart,
					RequestStartDelay = context.RequestStartDelay,
					ElapsedTime = context.Timer.Elapsed,
					Exception = ex
				});
			}
		}

		private class RequestContext
		{
			public int RequestNumber { get; set; }
			public Stopwatch Timer { get; set; }
			public double RequestStartDelay { get; set; }
			public CancellationToken TimeoutToken { get; set; }
		}
	}
}
