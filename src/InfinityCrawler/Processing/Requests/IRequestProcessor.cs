using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InfinityCrawler.Processing.Requests
{
	public interface IRequestProcessor
	{
		void Add(Uri requestUri);

		int PendingRequests { get; }

		Task ProcessAsync(
			HttpClient httpClient,
			Func<RequestResult, Task> responseAction,
			RequestProcessorOptions options, 
			CancellationToken cancellationToken = default
		);
	}
}
