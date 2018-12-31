using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace InfinityCrawler.TaskHandlers
{
	public class ParallelAsyncTaskHandler : ITaskHandler
	{
		private ILogger Logger { get; }

		public ParallelAsyncTaskHandler(ILogger logger)
		{
			Logger = logger;
		}

		public async Task For<TModel>(
			IEnumerable<TModel> items, 
			Func<TModel, ConcurrentQueue<TModel>, Task> action, 
			TaskHandlerOptions options
		)
		{
			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			var random = new Random();
			var itemsToProcess = new ConcurrentQueue<TModel>(items);
			var activeTasks = new ConcurrentDictionary<Task, Stopwatch>();

			var currentBackoff = 0;
			var successesSinceLastThrottle = 0;
			var taskCount = 0;

			while (activeTasks.Count > 0 || !itemsToProcess.IsEmpty)
			{
				while (!itemsToProcess.IsEmpty)
				{
					if (itemsToProcess.TryDequeue(out var item))
					{
						var taskStartDelay = 0d;
						//Task delaying and backoff
						if (options.DelayBetweenTaskStart.TotalMilliseconds > 0)
						{
							taskStartDelay = options.DelayBetweenTaskStart.TotalMilliseconds;
							taskStartDelay += random.NextDouble() * options.DelayJitter.TotalMilliseconds;
						}

						taskStartDelay += currentBackoff;

						var timer = new Stopwatch();
						var task = RunAction(item, action, itemsToProcess, (int)taskStartDelay, timer);

						Logger?.LogDebug($"Task #{task.Id} started with {taskStartDelay}ms delay");

						activeTasks.TryAdd(task, timer);
						taskCount++;

						//Max task early exit
						if (options.MaxNumberOfTasks != 0 && taskCount == options.MaxNumberOfTasks)
						{
							Logger?.LogInformation($"Maximum number of {options.MaxNumberOfTasks} tasks reached");
							await Task.WhenAll(activeTasks.Keys);
							return;
						}

						if (activeTasks.Count == options.MaxNumberOfSimultaneousTasks)
						{
							break;
						}
					}
				}

				await Task.WhenAny(activeTasks.Keys).ConfigureAwait(false);
				
				var completedTasks = activeTasks.Keys.Where(t => t.IsCompleted);
				foreach (var completedTask in completedTasks)
				{
					activeTasks.TryRemove(completedTask, out var timer);

					if (completedTask.IsFaulted)
					{
						if (options.BubbleUpExceptions)
						{
							throw completedTask.Exception;
						}
						else
						{
							Logger?.LogError(completedTask.Exception, $"Task #{completedTask.Id} has completed in a faulted state");
						}
					}

					Logger?.LogDebug($"Task #{completedTask.Id} completed in {timer.ElapsedMilliseconds}ms");

					//Manage the throttling based on timeouts and successes
					var throttlePoint = options.TimeoutBeforeThrottle;
					if (throttlePoint.TotalMilliseconds > 0 && timer.Elapsed > throttlePoint)
					{
						successesSinceLastThrottle = 0;
						currentBackoff += (int)options.ThrottlingRequestBackoff.TotalMilliseconds;
						Logger?.LogInformation($"New backoff of {currentBackoff}ms");
					}
					else if (currentBackoff > 0)
					{
						successesSinceLastThrottle += 1;
						if (successesSinceLastThrottle > options.MinSequentialSuccessesToMinimiseThrottling)
						{
							var newBackoff = currentBackoff - options.ThrottlingRequestBackoff.TotalMilliseconds;
							currentBackoff = Math.Max(0, (int)newBackoff);
							successesSinceLastThrottle = 0;
							Logger?.LogInformation($"New backoff of {currentBackoff}ms");
						}
					}
				}
			}
		}

		private async Task RunAction<TModel>(TModel item, Func<TModel, ConcurrentQueue<TModel>, Task> action, ConcurrentQueue<TModel> itemsToProcess, int delay, Stopwatch timer)
		{
			if (delay > 0)
			{
				await Task.Delay(delay);
			}
			timer.Start();
			await action(item, itemsToProcess);
			timer.Stop();
		}
	}
}
