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
				var baseUri = pageUri;

				//Allows <base href=""> to work
				if (Uri.IsWellFormedUriString(baseHref, UriKind.RelativeOrAbsolute))
				{
					baseUri = new Uri(pageUri, baseHref);
				}

				return new Uri(baseUri, hrefWithoutFragment);
			}

			return null;
		}
	}
}
