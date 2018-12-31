using System;
using System.Collections.Generic;
using System.Text;

namespace InfinityCrawler.TaskHandlers
{
	public class TaskHandlerOptions
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
