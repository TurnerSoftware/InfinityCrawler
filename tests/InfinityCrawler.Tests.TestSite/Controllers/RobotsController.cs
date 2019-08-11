using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace InfinityCrawler.Tests.TestSite.Controllers
{
	[Route("/robots/")]
	public class RobotsController : ControllerBase
	{
		private string GetHtml(string path)
		{
			return $@"<!DOCTYPE html>
<html>
<head>
</head>
<body>
	<a class='/robots/result-path/{path}'>Test Path</a>
</body>
</html>";
		}
		
		private ContentResult GetResult(string path)
		{
			return new ContentResult
			{
				StatusCode = (int)HttpStatusCode.OK,
				ContentType = "text/html",
				Content = GetHtml(path)
			};
		}
		
		[Route("header-page-noindex")]
		public IActionResult AllNoIndex()
		{
			Response.Headers.Add("X-Robots-Tag", "noindex");
			return GetResult("header-page-no-index");
		}
		[Route("header-page-nofollow")]
		public IActionResult AllNoFollow()
		{
			Response.Headers.Add("X-Robots-Tag", "nofollow");
			return GetResult("header-page-no-follow");
		}
		[Route("header-page-none")]
		public IActionResult AllNone()
		{
			Response.Headers.Add("X-Robots-Tag", "none");
			return GetResult("header-page-none");
		}
		[Route("header-bot-specific")]
		public IActionResult BotSpecific()
		{
			Response.Headers.Add("X-Robots-Tag", "onebot: noindex");
			Response.Headers.Add("X-Robots-Tag", "twobot: nofollow");
			return GetResult("header-bot-specific");
		}
	}
}
