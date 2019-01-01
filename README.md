# Infinity Crawler
A simple but powerful web crawler library in C#

[![AppVeyor](https://img.shields.io/appveyor/ci/Turnerj/infinitycrawler/master.svg)](https://ci.appveyor.com/project/Turnerj/infinitycrawler)
[![Codecov](https://img.shields.io/codecov/c/github/turnersoftware/infinitycrawler/master.svg)](https://codecov.io/gh/TurnerSoftware/infinitycrawler)

## Features
- Obeys robots.txt (crawl delay & allow/disallow)
- Uses sitemap.xml to seed the initial crawl of the site
- Built around a parllel task `async`/`await` system
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