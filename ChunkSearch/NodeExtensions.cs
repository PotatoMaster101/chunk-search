using System.Runtime.CompilerServices;
using Acornima.Ast;

namespace ChunkSearch;

public static class NodeExtensions
{
    public static IEnumerable<Node> Walk(this Node node)
    {
        yield return node;
        foreach (var child in node.ChildNodes.SelectMany(Walk))
            yield return child;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> Walk<T>(this Node node) where T : Node
    {
        return node.Walk().Where(x => x is T).Cast<T>();
    }

    public static IEnumerable<ChunkLoader> GetChunkLoaders(this Node node)
    {
        return node.Walk()
            .Where(x =>
            {
                if (x is not FunctionExpression && x is not ArrowFunctionExpression)
                    return false;
                if (((IFunction)x).Params.Count != 1)
                    return false;
                return x.GetLiterals<string>().Any(y => y.EndsWith(".js") && !y.Contains('/'));
            })
            .Select(ChunkLoader.CreateChunkLoader)
            .OfType<ChunkLoader>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IEnumerable<T> GetLiterals<T>(this Node node)
    {
        return node.Walk<Literal>().Where(x => x.Value is T).Select(x => x.Value).Cast<T>();
    }
}
