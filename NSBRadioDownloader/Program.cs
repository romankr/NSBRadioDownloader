using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using NSBRadioDownloader;

var serviceProvider = new ServiceCollection()
    .AddHttpClient()
    .BuildServiceProvider();

Parser.Default.ParseArguments<Options>(args)
    .WithParsed(o =>
    {
        var factory = serviceProvider.GetService<IHttpClientFactory>();

        if (factory == null)
        {
            throw new Exception("Failed to retreive Http Client Factory.");
        }

        var downloader = new ShowDownloader(o, factory);
        downloader.Download();
    });

