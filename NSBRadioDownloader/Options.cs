namespace NSBRadioDownloader
{
    using CommandLine;

    internal class Options
    {
        [Option('u', "url", Required = true, HelpText = "Show archive URL, i.e. https://archives.nsbradio.co.uk/index.php?dir=The%20JJPinkman%20Show/")]
        public string? BaseUrl { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output directory for downloaded files")]
        public string? OutputDirectory { get; set; }

        [Option('d', "downloads", Required = false, HelpText = "Number of simultaneous downloads", Default = 1)]
        public int ParallelDownloads { get; set; }

        [Option('r', "overwrite", Required = false, HelpText = "Overwrite already existing files", Default = false)]
        public bool OverwriteFiles { get; set; }
    }
}
