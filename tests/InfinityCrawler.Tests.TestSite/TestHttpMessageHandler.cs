using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;

namespace InfinityCrawler.Tests.TestSite
{
	public class TestHttpMessageHandler : HttpMessageHandler
	{
		private HttpMessageHandler InternalHandler { get; }

		public TestHttpMessageHandler(HttpMessageHandler internalHandler)
		{
			InternalHandler = internalHandler;
		}

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			try
			{
				return await InternalSendAsync(request, cancellationToken);
			}
			catch (IOException ex) when (ex.Message == "The request was aborted or the pipeline has finished")
			{
				//This error only happens because the test server isn't actually called via HTTP, it is called directly
				//In reality, it would actually throw a `TaskCanceledException`
				throw new TaskCanceledException(null, ex);
			}
		}

		private async Task<HttpResponseMessage> InternalSendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var method = typeof(HttpMessageHandler).GetMethod("SendAsync", BindingFlags.NonPublic | BindingFlags.Instance);
			var invokedTask = (Task<HttpResponseMessage>)method.Invoke(InternalHandler, new object[] { request, cancellationToken });
			return await invokedTask;
		}
	}
}
