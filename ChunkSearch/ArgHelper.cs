using Acornima;
using ChunkSearch.Chunk;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Playwright;

namespace ChunkSearch;

/// <summary>
/// Helper methods for handling CLI arguments.
/// </summary>
public static class ArgHelper
{
    private static readonly HttpClient Client = new();
    private static readonly Lock Lock = new();

    /// <summary>
    /// Handles file arguments.
    /// </summary>
    /// <param name="file">The file from user.</param>
    /// <param name="verbose">Whether to output verbose.</param>
    public static async Task HandleFileArg(string file, bool verbose)
    {
        if (verbose)
            Console.WriteLine($"PROCESS {file}");

        var content = await File.ReadAllTextAsync(file);
        var parser = new Parser();
        var loaders = ChunkLoader.FromRootNode(parser.ParseModule(content));
        foreach (var loader in loaders)
            PrintLoader(file, loader);
    }

    /// <summary>
    /// Handle directory arguments.
    /// </summary>
    /// <param name="directory">The directory from user.</param>
    /// <param name="verbose">Whether to output verbose.</param>
    public static async Task HandleDirectoryArg(string directory, bool verbose)
    {
        var matcher = new Matcher();
        matcher.AddInclude("**/*.js");
        foreach (var file in matcher.GetResultsInFullPath(directory))
            await HandleFileArg(file, verbose);
    }

    /// <summary>
    /// Handle website arguments.
    /// </summary>
    /// <param name="url">The site URL from user.</param>
    /// <param name="verbose">Whether to output verbose.</param>
    public static async Task HandleSiteArg(string url, bool verbose)
    {
        var jsUrls = await GetJsUrls(url, verbose);
        var parser = new Parser();
        await Task.WhenAll(jsUrls.Select(async x =>
        {
            if (verbose)
                lock(Lock) { Console.WriteLine($"PROCESS {x}"); }

            var content = await Client.GetStringAsync(x);
            foreach (var loader in ChunkLoader.FromRootNode(parser.ParseModule(content)))
                PrintLoader(x, loader);
        }));
    }

    /// <summary>
    /// Returns the JS URLs found on a site.
    /// </summary>
    /// <param name="url">The site URL.</param>
    /// <param name="verbose">Whether to output verbose.</param>
    /// <returns>The JS URLs found on the site.</returns>
    private static async Task<IEnumerable<string>> GetJsUrls(string url, bool verbose)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();
        var result = new HashSet<string>();
        page.Request += (_, request) =>
        {
            if (request.Method != "GET" || !request.Url.EndsWith(".js"))
                return;
            if (verbose)
                Console.WriteLine($"GET {request.Url}");
            result.Add(request.Url);
        };
        await page.GotoAsync(url.StartsWith("https://") ? url : "https://" + url);
        await page.WaitForLoadStateAsync();
        return result;
    }

    /// <summary>
    /// Outputs the chunk loader.
    /// </summary>
    /// <param name="url">The URL/file path of the loader JS file.</param>
    /// <param name="chunkLoader">The loader object.</param>
    private static void PrintLoader(string url, ChunkLoader chunkLoader)
    {
        var chunkIds = chunkLoader.GetChunkIds();
        if (chunkIds.Count == 0)
            return;     // no chunk ID detected - might not be a valid loader

        var files = chunkLoader.Run(chunkIds);
        lock (Lock)
        {
            Console.WriteLine($"Found chunk loader in {url}:");
            Console.WriteLine(chunkLoader);
            if (files.Count > 0)
                Console.WriteLine(Environment.NewLine + string.Join(Environment.NewLine, files));
            Console.WriteLine();
        }
    }
}
