using Acornima;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Playwright;

namespace ChunkSearch;

public static class ArgHelper
{
    private static readonly HttpClient Client = new();

    public static async Task HandleFileArg(string file, bool verbose)
    {
        if (verbose)
            Console.WriteLine($"PROCESS {file}");

        var content = await File.ReadAllTextAsync(file);
        var parser = new Parser();
        var program = parser.ParseModule(content);
        foreach (var loader in program.GetChunkLoaders())
            PrintResult(file, loader);
    }

    public static async Task HandleDirectoryArg(string directory, bool verbose)
    {
        var matcher = new Matcher();
        matcher.AddInclude("**/*.js");
        foreach (var file in matcher.GetResultsInFullPath(directory))
            await HandleFileArg(file, verbose);
    }

    public static async Task HandleSiteArg(string url, bool verbose)
    {
        foreach (var jsUrl in await GetJsUrls(url, verbose))
        foreach (var loader in await GetLoaderFromUrl(jsUrl, verbose))
            PrintResult(jsUrl, loader);
    }

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

    private static void PrintResult(string url, ChunkLoader loader)
    {
        Console.WriteLine($"Found chunk loader in {url}:");
        Console.WriteLine(loader.ToString());
        Console.WriteLine(Environment.NewLine + "Chunk Files:");
        Console.WriteLine(string.Join(Environment.NewLine, loader.ChunkEntries.Select(x => x.ToString())));
        Console.WriteLine();
    }

    private static async Task<IEnumerable<ChunkLoader>> GetLoaderFromUrl(string url, bool verbose)
    {
        if (verbose)
            Console.WriteLine($"PROCESS {url}");

        var content = await Client.GetStringAsync(url);
        var parser = new Parser();
        var program = parser.ParseModule(content);
        return program.GetChunkLoaders();
    }
}
