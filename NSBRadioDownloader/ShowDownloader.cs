namespace NSBRadioDownloader
{
    using HtmlAgilityPack;
    using System.Web;

    internal class ShowDownloader
    {
        public Options Options { get; private set; }

        public IHttpClientFactory HttpClientFactory { get; private set; }

        public ShowDownloader(Options options, IHttpClientFactory factory)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            HttpClientFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public void Download()
        {
            var shows = GetShows();
            DownloadShows(shows).Wait();
        }

        private IEnumerable<ShowInfo?> GetShows()
        {
            var web = new HtmlWeb();
            var doc = web.Load(Options.BaseUrl);
            const string selector = "//a[@class = 'noBreak']";
            return doc.DocumentNode.SelectNodes(selector).Select(ToShowInfo);
        }

        private ShowInfo? ToShowInfo(HtmlNode node)
        {
            const string hrefAttribute = "href";
            const string fileAttribute = "file";

            if (string.IsNullOrEmpty(Options.BaseUrl))
            {
                throw new Exception("Options.BaseUrl is null or empty.");
            }

            var href = HttpUtility.HtmlDecode(node.Attributes[hrefAttribute].Value);
            var uri = new Uri(new Uri(Options.BaseUrl), href);
            var name = HttpUtility.ParseQueryString(uri.Query)[fileAttribute];

            return new ShowInfo
            {
                Name = name,
                Url = uri.ToString()
            };
        }

        private async Task DownloadShows(IEnumerable<ShowInfo?> shows)
        {
            if (string.IsNullOrEmpty(Options.OutputDirectory))
            {
                throw new Exception("Options.OutputDirectory is null or empty.");
            }

            if (!Directory.Exists(Options.OutputDirectory))
            {
                Directory.CreateDirectory(Options.OutputDirectory);
                Console.WriteLine("Created directory {0} ", Options.OutputDirectory);
            }

            var client = HttpClientFactory.CreateClient();

            var throttler = new SemaphoreSlim(Options.ParallelDownloads);

            var tasks = shows.Select(async s =>
            {
                await throttler.WaitAsync();
                try
                {
                    await DownloadSignleShowAsync(client, s);
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

            if (string.IsNullOrEmpty(Options.OutputDirectory))
            {
                throw new Exception("Options.OutputDirectory is null or empty.");
            }

            var fileName = Path.Combine(Options.OutputDirectory, showInfo.Name);

            if (!Options.OverwriteFiles && File.Exists(fileName))
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
