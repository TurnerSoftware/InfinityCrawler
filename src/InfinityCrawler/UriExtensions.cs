using System;
using System.Collections.Generic;
using System.Text;

namespace InfinityCrawler
{
	internal static class UriExtensions
	{
		public static Uri BuildUriFromHref(this Uri pageUri, string href)
		{
			if (Uri.IsWellFormedUriString(href, UriKind.Absolute))
			{
				return new Uri(href, UriKind.Absolute);
			}
			else if (Uri.IsWellFormedUriString(href, UriKind.Relative))
			{
				return new Uri(pageUri, href);
			}

			return null;
		}
	}
}
