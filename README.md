# CLI-Scraper

A CLI tool for parallel web scraping and crawling.

---
```

  ▄      ▄   ██▄   ▄███▄   ▄████  ▄█    ▄   ▄███▄   ██▄              ▄▄▄▄▄   ▄█▄    █▄▄▄▄ ██   █ ▄▄  ▄███▄   █▄▄▄▄
   █      █  █  █  █▀   ▀  █▀   ▀ ██     █  █▀   ▀  █  █            █     ▀▄ █▀ ▀▄  █  ▄▀ █ █  █   █ █▀   ▀  █  ▄▀
█   █ ██   █ █   █ ██▄▄    █▀▀    ██ ██   █ ██▄▄    █   █         ▄  ▀▀▀▀▄   █   ▀  █▀▀▌  █▄▄█ █▀▀▀  ██▄▄    █▀▀▌
█   █ █ █  █ █  █  █▄   ▄▀ █      ▐█ █ █  █ █▄   ▄▀ █  █           ▀▄▄▄▄▀    █▄  ▄▀ █  █  █  █ █     █▄   ▄▀ █  █
█▄ ▄█ █  █ █ ███▀  ▀███▀    █      ▐ █  █ █ ▀███▀   ███▀                     ▀███▀    █      █  █    ▀███▀     █
 ▀▀▀  █   ██                 ▀       █   ██                                          ▀      █    ▀            ▀
                                                                                           ▀

Scraper 1.0.0
undeƒined (0x78f1935)
ERROR(S):
Required option 't, target' is missing.
Required option 's, scope' is missing.
Required option 'p, pattern' is missing.

  -v, --verbose            (Default: false) Set output to verbose messages.

  -t, --target             Required. Set target host.

  -s, --scope              Required. Allowed domain scope, use ; as delimiter.

  -a, --agent              (Default: Mozilla/5.0 (Windows; U; Windows NT 6.2) AppleWebKit/534.2.1 (KHTML, like Gecko)
                           Chrome/35.0.822.0 Safari/534.2.1) Set custom user agent.

  -p, --pattern            Required. Regex pattern to scrape with.

  -c, --crawlers           (Default: 4) Total concurrent tasks used for the Crawler.

  -x, --scrapers           (Default: 4) Total concurrent tasks used for the Scraper.

  -b, --downloaders        (Default: 2) Total concurrent downloaders used for downloading data.

  -q, --queryparameters    (Default: false) Strip query parameters from URL(s).

  -d, --download           (Default: false) Download found files.

  -j, --json               (Default: false) Generates output based on the pattern provided.

  -f, --filename           (Default: result.json) The file name of the generated output.

  -k, --checkpoints        (Default: false) Saves in between scraping pages, turn off to save time, might fail.

  --help                   Display this help screen.

  --version                Display version information.
```

## Workflow
![Flow](./.github/docs/schema.png)

## Paramaters

| Flag            	| Description                                                                                                                                                                                                       	|
|-----------------	|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------	|
| Verbose         	| Generates noise in STDout. Useful for debugging.                                                                                                                                                                  	|
| target          	| Entry point. The url provided will be the first url which goes through the crawler.                                                                                                                               	|
| scope           	| The scope can be delimited by `;`. The crawler only crawls page-urls which are starting with one of the available scopes.                                                                                         	|
| agent           	| You can provide a custom user-agent which is used when crawling / scraping. Defaults to: `Mozilla/5.0 (Windows; U; Windows NT 6.2) AppleWebKit/534.2.1 (KHTML, like Gecko) Chrome/35.0.822.0 Safari/534.2.1`.     	|
| pattern         	| The data you might look for can be defined by providing a regex format which works within .dotnet.                                                                                                                	|
| crawlers        	| Run the crawler task concurrent with X amount of workers. Defaults to 4.                                                                                                                                          	|
| scrapers        	| Run the scraper task concurrent with X amount of workers. Defaults to 4.                                                                                                                                          	|
| downloaders     	| Run the download task concurrent with X amount of workers. Defaults to 2                                                                                                                                          	|
| queryparameters 	| Safe time for big datasets by removing query parameters from urls which go through the crawler. `https://google.com/?a=1&b=2` becomes `https://google.com/` which greatly reduces the amount of duplicated calls. 	|
| download        	| When set, download files which have been found while crawling for links.                                                                                                                                          	|
| json            	| When set, save match results based on provided pattern into a JSON format.                                                                                                                                        	|
| filename        	| The name of the json file, Defaults to `result.json`.                                                                                                                                                             	|
| checkpoints     	| When set, save to json file every now and often while scraping / crawling is still in progress.                                                                                                                   	|

## Examples

```
.\Scraper.exe -v -j -k -i -t "https://oldschool.runescape.wiki/" -s "runescape.wiki;/" -c 32 -x 4 -d 4-p "(\b(http|ftp|https):(\/\/|\\\\)[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&:/~\+#]*[\w\-\@?^=%&/~\+#])?|\bwww\.[^\s])"
```

```
.\Scraper.exe -j -k -d -t "https://www.microsoft.com/" -s "microsoft.com;/" -p "(\b(http|ftp|https):(\/\/|\\\\)[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&:/~\+#]*[\w\-\@?^=%&/~\+#])?|\bwww\.[^\s])"
```
