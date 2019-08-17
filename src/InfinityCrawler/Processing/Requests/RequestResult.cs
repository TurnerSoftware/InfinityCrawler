using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace InfinityCrawler.Processing.Requests
{
	public class RequestResult
	{
		public Uri RequestUri { get; set; }
		public DateTime RequestStart { get; set; }
		public double RequestStartDelay { get; set; }
		public HttpResponseMessage ResponseMessage { get; set; }
		public TimeSpan ElapsedTime { get; set; }
		public Exception Exception { get; set; }
	}
}
