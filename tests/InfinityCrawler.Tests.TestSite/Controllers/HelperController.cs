using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace InfinityCrawler.Tests.TestSite.Controllers
{
	[Route("/")]
	public class HelperController : ControllerBase
	{
		private SiteContext Context { get; }

		public HelperController(SiteContext context)
		{
			Context = context;
		}

		[Route("delay/{delay}/{path}")]
		public async Task<IActionResult> Delay(int delay, string path)
		{
			await Task.Delay(delay);
			return new ContentResult
			{
				Content = path
			};
		}

		[Route("status/{statusCode}")]
		public IActionResult ReturnError(HttpStatusCode statusCode)
		{
			return new ContentResult
			{
				StatusCode = (int)statusCode,
				Content = statusCode.ToString()
			};
		}

		[Route("sitemap.xml")]
		public IActionResult DynamicSitemap()
		{
			var defaultFile = "index.html";

			if (!string.IsNullOrEmpty(Context.EntryPath))
			{
				defaultFile = Context.EntryPath + defaultFile;
			}

			return new ContentResult
			{
				ContentType = "text/xml",
				Content = $@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">
  <url>
	<loc>http://localhost/{defaultFile}</loc>
  </url>
</urlset>"
			};
		}
	}
}
