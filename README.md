# Infinity Crawler
A simple but powerful web crawler library in C#

[![AppVeyor](https://img.shields.io/appveyor/ci/Turnerj/infinitycrawler/master.svg)](https://ci.appveyor.com/project/Turnerj/infinitycrawler)
[![Codecov](https://img.shields.io/codecov/c/github/turnersoftware/infinitycrawler/master.svg)](https://codecov.io/gh/TurnerSoftware/infinitycrawler)
[![NuGet](https://img.shields.io/nuget/v/InfinityCrawler.svg)](https://www.nuget.org/packages/InfinityCrawler)

## Features
- Obeys robots.txt (crawl delay & allow/disallow)
- Obeys in-page robots rules (`X-Robots-Tag` header and `<meta name="robots" />` tag)
- Uses sitemap.xml to seed the initial crawl of the site
- Built around a parallel task `async`/`await` system
- Swappable request and content processors, allowing greater customisation
- Auto-throttling (see below)

## Polite Crawling
The crawler is built around fast but "polite" crawling of website.
This is accomplished through a number of settings that allow adjustments of delays and throttles.

You can control:
- Number of simulatenous requests
- The delay between requests starting (Note: If a `crawl-delay` is defined for the User-agent, that will be the minimum)
- Artificial "jitter" in request delays (requests seem less "robotic")
- Timeout for a request before throttling will apply for new requests
- Throttling request backoff: The amount of time added to the delay to throttle requests (this is cumulative)
- Minimum number of requests under the throttle timeout before the throttle is gradually removed

## Other Settings
- Control the UserAgent used in the crawling process
- Set additional host aliases you want the crawling process to follow (for example, subdomains)
- The max number of retries for a specific URI
- The max number of redirects to follow
- The max number of pages to crawl

## Example Usage
```csharp
using InfinityCrawler;

var crawler = new Crawler();
var result = await crawler.Crawl(new Uri("http://example.org/"), new CrawlSettings {
	UserAgent = "MyVeryOwnWebCrawler/1.0",
	RequestProcessorOptions = new RequestProcessorOptions
	{
		MaxNumberOfSimultaneousRequests = 5
	}
});
```