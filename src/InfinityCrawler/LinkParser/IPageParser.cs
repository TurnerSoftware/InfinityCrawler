using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace InfinityCrawler.LinkParser
{
	public interface IPageParser
	{
		Task<IEnumerable<CrawlLink>> Parse(TextReader reader);
	}
}
