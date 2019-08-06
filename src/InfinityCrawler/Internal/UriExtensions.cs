using System;
using System.Collections.Generic;
using System.Text;

namespace InfinityCrawler.Internal
{
	internal static class UriExtensions
	{
		public static Uri BuildUriFromHref(this Uri pageUri, string href, string baseHref = null)
		{
			var hrefWithoutFragment = href.Split('#')[0];

			if (Uri.IsWellFormedUriString(hrefWithoutFragment, UriKind.RelativeOrAbsolute))
			{
				if (Uri.IsWellFormedUriString(baseHref, UriKind.Absolute))
				{
					//Allows <base href=""> to work
					return new Uri(baseHref);
				}

				return new Uri(pageUri, hrefWithoutFragment);
			}

			return null;
		}
	}
}
