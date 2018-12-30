using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InfinityCrawler
{
	public static class ParallelAsyncTask
	{
		public static async Task For<TModel>(
			IEnumerable<TModel> items, 
			Func<TModel, ConcurrentQueue<TModel>, Task> action, 
			ParallelAsyncTaskOptions options
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
						var task = action(item, itemsToProcess);
						var timer = new Stopwatch();
						timer.Start();
						activeTasks.TryAdd(task, timer);
						taskCount++;

						//Max task early exit
						if (options.MaxNumberOfTasks != 0 && taskCount == options.MaxNumberOfTasks)
						{
							await Task.WhenAll(activeTasks.Keys);
							return;
						}

						//Task delaying and backoff
						if (options.DelayBetweenTaskStart.TotalMilliseconds > 0)
						{
							var taskStartDelay = options.DelayBetweenTaskStart.TotalMilliseconds;
							taskStartDelay += random.NextDouble() * options.DelayJitter.TotalMilliseconds;
							taskStartDelay += currentBackoff;
							Thread.Sleep((int)taskStartDelay);
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
					timer.Stop();

					if (options.BubbleUpExceptions && completedTask.IsFaulted)
					{
						throw completedTask.Exception;
					}

					//Manage the throttling based on timeouts and successes
					var throttlePoint = options.TimeoutBeforeThrottle;
					if (throttlePoint.TotalMilliseconds > 0 && timer.Elapsed > throttlePoint)
					{
						successesSinceLastThrottle = 0;
						currentBackoff += (int)options.ThrottlingRequestBackoff.TotalMilliseconds;
					}
					else if (currentBackoff > 0)
					{
						successesSinceLastThrottle += 1;
						if (successesSinceLastThrottle > options.MinSequentialSuccessesToMinimiseThrottling)
						{
							var newBackoff = currentBackoff - options.ThrottlingRequestBackoff.TotalMilliseconds;
							currentBackoff = Math.Max(0, (int)newBackoff);
							successesSinceLastThrottle = 0;
						}
					}
				}
			}
		}
	}

	public class ParallelAsyncTaskOptions
	{
		/// <summary>
		/// Maximum number of simultaneous asynchronous tasks run at once. Default is 5 tasks.
		/// </summary>
		public int MaxNumberOfSimultaneousTasks { get; set; } = 5;
		/// <summary>
		/// Delay between one task starting and the next.
		/// </summary>
		public TimeSpan DelayBetweenTaskStart { get; set; }
		/// <summary>
		///	Maximum jitter applied to a task delay.
		/// </summary>
		public TimeSpan DelayJitter { get; set; }
		/// <summary>
		/// The task timeout length before throttling sets in. 
		/// </summary>
		public TimeSpan TimeoutBeforeThrottle { get; set; }
		/// <summary>
		/// The amount of throttling delay to add to subsequent tasks. This is added every time the timeout is hit.
		/// </summary>
		public TimeSpan ThrottlingRequestBackoff { get; set; }
		/// <summary>
		/// Minimum number of tasks below the timeout before minimising the applied throttling. Default is 5 tasks.
		/// </summary>
		public int MinSequentialSuccessesToMinimiseThrottling { get; set; } = 5;
		/// <summary>
		/// Maximum number of tasks to run before exiting. Zero means no limit.
		/// </summary>
		public int MaxNumberOfTasks { get; set; }
		/// <summary>
		/// Bubble up exceptions from faulted tasks.
		/// </summary>
		public bool BubbleUpExceptions { get; set; }
	}
}
