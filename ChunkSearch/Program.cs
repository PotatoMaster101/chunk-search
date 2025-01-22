using System.CommandLine;
using ChunkSearch;

var urlArg = new Argument<string>("url", "URL to the site to search for the Webpack chunk loader");
var fileOpt = new Option<bool>(["-f", "--file"], "Specifies that the URL is a file path");
var dirOpt = new Option<bool>(["-d", "--dir"], "Specifies that the URL is a directory path");
var verboseOpt = new Option<bool>(["-v", "--verbose"], "Specifies verbose output");
var rootCmd = new RootCommand("Webpack Chunk Loader Extractor") { urlArg, fileOpt, dirOpt, verboseOpt };
rootCmd.SetHandler(async (url, isFile, isDir, isVerbose) =>
{
    if (isFile)
        await ArgHelper.HandleFileArg(url, isVerbose);
    else if (isDir)
        await ArgHelper.HandleDirectoryArg(url, isVerbose);
    else
        await ArgHelper.HandleSiteArg(url, isVerbose);
}, urlArg, fileOpt, dirOpt, verboseOpt);

await rootCmd.InvokeAsync(args);
