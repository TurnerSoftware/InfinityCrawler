using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TurnerSoftware.RobotsExclusionTools;
using TurnerSoftware.SitemapTools;

namespace InfinityCrawler
{
	public class Crawler
	{
		private HttpClient HttpClient { get; }

		public Crawler()
		{
			HttpClient = new HttpClient(new HttpClientHandler
			{
				AllowAutoRedirect = false,
				UseCookies = false
			});
		}

		public Crawler(HttpClient httpClient)
		{
			HttpClient = httpClient;
		}

		public async Task<CrawlResult> Crawl(Uri siteUri, CrawlSettings settings)
		{
			var baseUri = new Uri(siteUri.GetLeftPart(UriPartial.Authority));
			var robotsFile = await new RobotsParser(HttpClient).FromUriAsync(baseUri);

			var seedUris = new List<Uri>
			{
				baseUri
			};

			//Use any links referred to by the sitemap as a starting point
			seedUris.AddRange((await new SitemapQuery(HttpClient)
				.GetAllSitemapsForDomain(siteUri.Host))
				.SelectMany(s => s.Urls.Select(u => u.Location))
				.Distinct()
			);

			var crawledUris = new ConcurrentDictionary<Uri, CrawledUriResult>();

			await ParallelAsyncTask.For<Uri, CrawledUriContext>(seedUris, async (itemContext, pagesToCrawl) =>
			{
				CrawledUriResult entry = null;

				if (crawledUris.ContainsKey(itemContext.Model))
				{
					entry = crawledUris[itemContext.Model];

					var lastRequest = entry.Requests.LastOrDefault();
					if (lastRequest.IsSuccessfulStatus)
					{
						return;
					}

					if (entry.Requests.Count() == settings.NumberOfRetries)
					{
						return;
					}
				}
				else
				{
					entry = new CrawledUriResult
					{
						Location = itemContext.Model
					};
				}

				if (robotsFile.IsAllowedAccess(itemContext.Model, settings.UserAgent))
				{
					var crawlRequest = new CrawlRequest
					{
						RequestStart = DateTime.UtcNow,
					};
					
					var stopwatch = new Stopwatch();
					stopwatch.Start();

					using (var response = await HttpClient.GetAsync(itemContext.Model))
					{
						crawlRequest.StatusCode = response.StatusCode;
						crawlRequest.IsSuccessfulStatus = response.IsSuccessStatusCode;

						entry.Requests = entry.Requests.Concat(new[] { crawlRequest });

						if (!crawlRequest.IsSuccessfulStatus)
						{
							stopwatch.Stop();
							crawlRequest.ElapsedTime = stopwatch.Elapsed;
							pagesToCrawl.Enqueue(itemContext);
						}
						else if (response.StatusCode == HttpStatusCode.MovedPermanently || response.StatusCode == HttpStatusCode.Redirect)
						{
							stopwatch.Stop();
							crawlRequest.ElapsedTime = stopwatch.Elapsed;

							var crawlContext = itemContext.Context ?? new CrawledUriContext();
							crawlContext.RedirectChain.Add(new CrawledUriRedirect
							{
								Location = itemContext.Model,
								Requests = entry.Requests
							});

							pagesToCrawl.Enqueue(new ParallelAsyncTask.ItemContext<Uri, CrawledUriContext>
							{
								Model = response.Headers.Location,
								Context = crawlContext
							});
						}
						else
						{
							entry.RedirectChain = itemContext.Context?.RedirectChain;

							await response.Content.LoadIntoBufferAsync();

							var crawledContent = new CrawledContent
							{
								ContentType = response.Content.Headers.ContentType.MediaType,
								CharacterSet = response.Content.Headers.ContentType.CharSet,
								ContentEncoding = string.Join(",", response.Content.Headers.ContentEncoding)
							};

							var contentStream = new MemoryStream();
							await (await response.Content.ReadAsStreamAsync()).CopyToAsync(contentStream);
							crawledContent.ContentStream = contentStream;

							stopwatch.Stop();
							crawlRequest.ElapsedTime = stopwatch.Elapsed;

							//Find links to crawl
							var reader = new StreamReader(contentStream);
							var crawlLinks = await settings.LinkParser.Parse(reader);
							crawledContent.Links = crawlLinks;
							foreach (var crawlLink in crawlLinks)
							{
								if (!crawledUris.ContainsKey(crawlLink.Location))
								{
									pagesToCrawl.Enqueue(new ParallelAsyncTask.ItemContext<Uri, CrawledUriContext>(crawlLink.Location));
								}
							}

							entry.Content = crawledContent;
						}
					}
				}
				else
				{
					entry.IsCrawlBlocked = true;
					entry.BlockReason = $"{itemContext.Model} blocked by Robots file";
				}

				crawledUris.TryAdd(itemContext.Model, entry);
			});

			return new CrawlResult
			{
				
			};
		}
	}
}
