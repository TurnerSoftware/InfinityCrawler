using System;
using System.Collections.Generic;
using System.Text;

namespace InfinityCrawler
{
	internal static class UriExtensions
	{
		public static Uri BuildUriFromHref(this Uri pageUri, string href, string baseHref = null)
		{
			if (Uri.IsWellFormedUriString(href, UriKind.Absolute))
			{
				return new Uri(href, UriKind.Absolute);
			}
			else if (Uri.IsWellFormedUriString(href, UriKind.Relative))
			{
				if (baseHref != null && Uri.IsWellFormedUriString(baseHref, UriKind.Absolute))
				{
					//Allows <base href=""> to work
					return new Uri(new Uri(baseHref), href);
				}
				else
				{
					return new Uri(pageUri, href);
				}
			}

			return null;
		}
	}
}
