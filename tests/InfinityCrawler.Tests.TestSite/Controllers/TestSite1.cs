using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Mvc;

namespace InfinityCrawler.Tests.TestSite.Controllers
{
	public class TestSite1 : ControllerBase
	{
		public IActionResult TestAction()
		{
			Thread.Sleep(1200);
			return new ContentResult
			{
				Content = "Hello world!"
			};
		}
	}
}
