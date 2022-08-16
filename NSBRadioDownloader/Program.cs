using CommandLine;
using NSBRadioDownloader;

Parser.Default.ParseArguments<Options>(args)
    .WithParsed(o =>
    {
        new ShowDownloader(o).Download();
    });

