using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace InfinityCrawler.Processing.Requests
{
	public class RequestContext
	{
		public int RequestNumber { get; set; }
		public Uri RequestUri { get; set; }
		public Stopwatch Timer { get; set; }
		public double RequestStartDelay { get; set; }
		public TimeSpan RequestTimeout { get; set; }
		public CancellationToken CancellationToken { get; set; }
	}
}
