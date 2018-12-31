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

		public ParallelAsyncTaskHandler(ILogger logger = null)
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
			var activeTasks = new ConcurrentDictionary<Task, TaskContext>();

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

						var taskContext = new TaskContext
						{
							TaskNumber = taskCount + 1,
							Timer = new Stopwatch()
						};
						var task = RunAction(item, action, itemsToProcess, (int)taskStartDelay, taskContext);

						Logger?.LogDebug($"Task #{taskContext.TaskNumber} started with {taskStartDelay}ms delay");

						activeTasks.TryAdd(task, taskContext);
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
					activeTasks.TryRemove(completedTask, out var taskContext);

					if (completedTask.IsFaulted)
					{
						if (options.BubbleUpExceptions)
						{
							throw completedTask.Exception;
						}
						else
						{
							Logger?.LogError(completedTask.Exception, $"Task #{taskContext.TaskNumber} has completed in a faulted state");
						}
					}

					Logger?.LogDebug($"Task #{taskContext.TaskNumber} completed in {taskContext.Timer.ElapsedMilliseconds}ms");

					//Manage the throttling based on timeouts and successes
					var throttlePoint = options.TimeoutBeforeThrottle;
					if (throttlePoint.TotalMilliseconds > 0 && taskContext.Timer.Elapsed > throttlePoint)
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

		private async Task RunAction<TModel>(TModel item, Func<TModel, ConcurrentQueue<TModel>, Task> action, ConcurrentQueue<TModel> itemsToProcess, int delay, TaskContext context)
		{
			if (delay > 0)
			{
				await Task.Delay(delay);
			}
			context.Timer.Start();
			await action(item, itemsToProcess);
			context.Timer.Stop();
		}

		private class TaskContext
		{
			public int TaskNumber { get; set; }
			public Stopwatch Timer { get; set; }
		}
	}
}
