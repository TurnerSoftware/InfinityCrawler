using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace InfinityCrawler.Processing.Requests
{
	public class RequestResult
	{
		public Uri RequestUri { get; set; }
		public DateTime RequestStart { get; set; }
		public double RequestStartDelay { get; set; }
		public HttpStatusCode? StatusCode { get; set; }
		public HttpResponseHeaders ResponseHeaders { get; set; }
		public HttpContentHeaders ContentHeaders { get; set; }
		public Stream Content { get; set; }
		public TimeSpan ElapsedTime { get; set; }
		public Exception Exception { get; set; }
	}
}
