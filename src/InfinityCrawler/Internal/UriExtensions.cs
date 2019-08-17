using System;
using System.Collections.Generic;
using System.Text;

namespace InfinityCrawler.Internal
{
	internal static class UriExtensions
	{
		public static Uri BuildUriFromHref(this Uri pageUri, string href, string baseHref = null)
		{
			var hrefPieces = href.Split(new[] { '#' }, 2);
			var hrefWithoutFragment = hrefPieces[0];
			var hrefFragment = hrefPieces.Length > 1 ? hrefPieces[1] : null;

			if (Uri.IsWellFormedUriString(hrefWithoutFragment, UriKind.RelativeOrAbsolute))
			{
				var baseUri = pageUri;

				//Allows <base href=""> to work
				if (Uri.IsWellFormedUriString(baseHref, UriKind.RelativeOrAbsolute))
				{
					baseUri = new Uri(pageUri, baseHref);
				}

				return new UriBuilder(new Uri(baseUri, hrefWithoutFragment))
				{
					Fragment = hrefFragment
				}.Uri;
			}

			return null;
		}
	}
}
