using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace InfinityCrawler.ContentParsers
{
	public interface IContentParser
	{
		Task<CrawledContent> Parse(Uri uri, HttpResponseMessage response, CrawlSettings settings);
	}
}
