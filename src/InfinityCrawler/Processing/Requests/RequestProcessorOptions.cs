using System;
using System.Collections.Generic;
using System.Text;

namespace InfinityCrawler.Processing.Requests
{
	public class RequestProcessorOptions
	{
		/// <summary>
		/// Maximum number of simultaneous asynchronous requests to run at once.
		/// </summary>
		public int MaxNumberOfSimultaneousRequests { get; set; } = 10;
		/// <summary>
		/// Delay between one request starting and the next.
		/// </summary>
		public TimeSpan DelayBetweenRequestStart { get; set; } = new TimeSpan(0, 0, 0, 0, 1000);
		/// <summary>
		///	Maximum jitter applied to a request delay.
		/// </summary>
		public TimeSpan DelayJitter { get; set; } = new TimeSpan(0, 0, 0, 0, 1000);
		/// <summary>
		/// The request timeout length before throttling sets in. 
		/// </summary>
		public TimeSpan TimeoutBeforeThrottle { get; set; } = new TimeSpan(0, 0, 0, 0, 2500);
		/// <summary>
		/// The amount of throttling delay to add to subsequent requests. This is added every time the timeout is hit.
		/// </summary>
		public TimeSpan ThrottlingRequestBackoff { get; set; } = new TimeSpan(0, 0, 0, 5);
		/// <summary>
		/// Minimum number of requests below the timeout before minimising the applied throttling.
		/// </summary>
		public int MinSequentialSuccessesToMinimiseThrottling { get; set; } = 5;
		/// <summary>
		/// The amount of time before a request is cancelled and retried.
		/// </summary>
		public TimeSpan RequestTimeout { get; set; } = new TimeSpan(0, 0, 30);
	}
}
