namespace NSBRadioDownloader
{
    using HtmlAgilityPack;
    using System.Web;

    internal class ShowDownloader
    {
        private readonly Options _options;

        private readonly HttpClient _httpClient;

        public ShowDownloader(Options options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _httpClient = new HttpClient()
            {
                Timeout = new TimeSpan(0, 30, 0)
            };
        }

        public void Download()
        {
            DownloadShows(GetShows()).Wait();
        }

        // <a href="/index.php?dir=The%20JJPinkman%20Show/&amp;file=The_JJPinkman_Show_NO174_23-07-2022_01.mp3" class="noBreak">DOWNLOAD</a>
        private IEnumerable<ShowInfo?> GetShows()
        {
            var web = new HtmlWeb();
            var doc = web.Load(_options.BaseUrl);
            const string selector = "//a[@class = 'noBreak']";
            return doc.DocumentNode.SelectNodes(selector).Select(ToShowInfo);
        }

        private ShowInfo? ToShowInfo(HtmlNode node)
        {
            const string hrefAttribute = "href";
            const string fileAttribute = "file";

            if (string.IsNullOrEmpty(_options.BaseUrl))
            {
                throw new Exception("BaseUrl is null or empty.");
            }

            var href = HttpUtility.HtmlDecode(node.Attributes[hrefAttribute].Value);
            var uri = new Uri(new Uri(_options.BaseUrl), href);
            var name = HttpUtility.ParseQueryString(uri.Query)[fileAttribute];

            return new ShowInfo
            {
                Name = name,
                Url = uri.ToString()
            };
        }

        private async Task DownloadShows(IEnumerable<ShowInfo?> shows)
        {
            if (string.IsNullOrEmpty(_options.OutputDirectory))
            {
                throw new Exception("OutputDirectory is null or empty.");
            }

            if (!Directory.Exists(_options.OutputDirectory))
            {
                Directory.CreateDirectory(_options.OutputDirectory);
                Console.WriteLine("Created directory {0} ", _options.OutputDirectory);
            }

            var throttler = new SemaphoreSlim(_options.ParallelDownloads);

            var tasks = shows.Select(async s =>
            {
                await throttler.WaitAsync();
                try
                {
                    await DownloadSignleShowAsync(_httpClient, s);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to download {0}", s.Name);
                    Console.WriteLine(ex);
                }
                finally
                {
                    throttler.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        private async Task DownloadSignleShowAsync(HttpClient client, ShowInfo? showInfo)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (showInfo == null)
            {
                throw new ArgumentNullException(nameof(showInfo));
            }

            if (string.IsNullOrEmpty(showInfo.Name))
            {
                throw new Exception("Show name is empty");
            }

            if (string.IsNullOrEmpty(showInfo.Url))
            {
                throw new Exception("Show Url is empty");
            }

            Console.WriteLine("Downloading {0} ...", showInfo.Name);

            if (string.IsNullOrEmpty(_options.OutputDirectory))
            {
                throw new Exception("OutputDirectory is null or empty.");
            }

            var fileName = Path.Combine(_options.OutputDirectory, showInfo.Name);

            if (!_options.OverwriteFiles && File.Exists(fileName))
            {
                Console.WriteLine("{0} already exists", showInfo.Name);
                return;
            }

            using var s = await client.GetStreamAsync(new Uri(showInfo.Url));
            using var fs = new FileStream(fileName, FileMode.Create);
            await s.CopyToAsync(fs);

            Console.WriteLine("Downloaded {0}", showInfo.Name);
        }
    }
}
