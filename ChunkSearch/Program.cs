using System.CommandLine;
using ChunkSearch;

var urlArg = new Argument<string>("url", "URL to the site to search for the Webpack chunk loader");
var fileOpt = new Option<bool>("--file", "Specifies the URL is a file path");
var dirOpt = new Option<bool>("--dir", "Specifies the URL is a directory path");
var verboseOpt = new Option<bool>("--verbose", "Specifies verbose output");
var rootCmd = new RootCommand("Webpack Chunk Loader Utility") { urlArg, fileOpt, dirOpt, verboseOpt };
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
