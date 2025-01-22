using System.Globalization;
using Acornima;
using Acornima.Ast;
using Jint;

namespace ChunkSearch;

public class ChunkLoader
{
    public required Node Loader { get; init; }
    public required ChunkEntry[] ChunkEntries { get; init; }

    private ChunkLoader() { }

    public override string ToString()
    {
        return Loader.ToJavaScript(true);
    }

    public static ChunkLoader? CreateChunkLoader(Node node)
    {
        var chunkIds = GetChunkIds(node);
        if (chunkIds.Count == 0)
            return null;

        var entries = Run(node, chunkIds);
        return entries.Count == 0 ? null : new ChunkLoader { Loader = node, ChunkEntries = entries.ToArray() };
    }

    private static HashSet<string> GetChunkIds(Node node)
    {
        var result = new HashSet<string>();
        var objs = new List<ObjectExpression>();
        var bins = new List<BinaryExpression>();
        foreach (var child in node.Walk())
        {
            if (child is ObjectExpression obj)
                objs.Add(obj);
            else if (child is BinaryExpression bin)
                bins.Add(bin);
            else if (child is ForStatement or ForOfStatement or WhileStatement or DoWhileStatement)
                return result;
        }

        if (objs.Count > 0)
            result.UnionWith(GetMapChunkIds(objs));
        if (bins.Count > 0)
            result.UnionWith(GetComparisonChunkIds(bins));
        return result;
    }

    // keeping this here for now
    // private static async Task<IReadOnlySet<ChunkEntry>> RunWithTimeout(Node node, IEnumerable<string> args, TimeSpan timeout)
    // {
    //     var evalTask = Task.Run(() => Run(node, args));
    //     var timeoutTask = Task.Delay(timeout);
    //     if (await Task.WhenAny(evalTask, timeoutTask) == timeoutTask)
    //         return new HashSet<ChunkEntry>();
    //     return await evalTask;
    // }

    private static HashSet<ChunkEntry> Run(Node node, IEnumerable<string> args)
    {
        using var engine = new Engine();
        var result = new HashSet<ChunkEntry>();
        try
        {
            foreach (var arg in args)
            {
                var argIsNum = double.TryParse(arg, out var argNum);
                var argStr = argIsNum ? argNum.ToString(CultureInfo.InvariantCulture) : $"'{arg}'";
                var eval = engine.Evaluate($"({node.ToJavaScript()})({argStr});").ToString();
                if (eval != "undefined")
                    result.Add(new ChunkEntry(arg, eval));
            }
            return result;
        }
        catch
        {
            return result;
        }
    }

    private static HashSet<string> GetMapChunkIds(IEnumerable<ObjectExpression> nodes)
    {
        var result = new HashSet<string>();
        foreach (var prop in nodes.SelectMany(x => x.Walk<Property>()))
        {
            if (prop.Key is Identifier identifier)
                result.Add(identifier.Name);
            else if (prop.Key is Literal { Value: not null } literal)
                result.Add(literal.Value.ToString()!);
        }
        return result;
    }

    private static HashSet<string> GetComparisonChunkIds(IEnumerable<BinaryExpression> nodes)
    {
        var result = new HashSet<string>();
        foreach (var node in nodes)
        {
            if (node.Operator != Operator.Equality && node.Operator != Operator.StrictEquality)
                continue;

            if (node is { Left: Literal { Value: not null } left, Right: Identifier })
                result.Add(left.Value.ToString()!);
            else if (node is { Left: Identifier, Right: Literal { Value: not null } right })
                result.Add(right.Value.ToString()!);
        }
        return result;
    }
}
