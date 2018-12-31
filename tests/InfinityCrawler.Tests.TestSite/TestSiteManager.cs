using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace InfinityCrawler.Tests.TestSite
{
	public class TestSiteManager : IDisposable
	{
		private TestServer Server { get; set; }
		private HttpClient Client { get; set; }

		public TestSiteManager(SiteContext context)
		{
			var builder = new WebHostBuilder()
				.ConfigureServices(s =>
				{
					s.AddSingleton(context);
				})
				.UseStartup<Startup>();

			Server = new TestServer(builder);
			Client = Server.CreateClient();
		}

		public HttpClient GetHttpClient()
		{
			return Client;
		}

		public void Dispose()
		{
			if (Server != null)
			{
				Server.Dispose();
				Server = null;

				Client.Dispose();
				Client = null;
			}
		}
	}
}
