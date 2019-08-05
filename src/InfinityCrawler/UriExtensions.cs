using System;
using System.Collections.Generic;
using System.Text;

namespace InfinityCrawler
{
	internal static class UriExtensions
	{
		public static Uri BuildUriFromHref(this Uri pageUri, string href, string baseHref = null)
		{
			if (Uri.IsWellFormedUriString(href, UriKind.RelativeOrAbsolute))
			{
				if (Uri.IsWellFormedUriString(baseHref, UriKind.Absolute))
				{
					//Allows <base href=""> to work
					return new Uri(baseHref);
				}

				return new Uri(pageUri, href);
			}

			return null;
		}
	}
}
