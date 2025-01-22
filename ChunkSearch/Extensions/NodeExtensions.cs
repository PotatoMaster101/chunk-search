using Acornima.Ast;

namespace ChunkSearch.Extensions;

/// <summary>
/// Extension methods for <see cref="Node"/>.
/// </summary>
public static class NodeExtensions
{
    /// <summary>
    /// Walks the node, visiting itself and all child nodes.
    /// </summary>
    /// <param name="node">The node to start the walk.</param>
    /// <returns>A collection of nodes visited.</returns>
    public static IEnumerable<Node> Walk(this Node node)
    {
        yield return node;
        foreach (var child in node.ChildNodes.SelectMany(Walk))
            yield return child;
    }

    /// <summary>
    /// Walks the node, visiting itself and all child nodes. Returns only the node of a specific type.
    /// </summary>
    /// <param name="node">The node to start the walk.</param>
    /// <typeparam name="T">The type of the node.</typeparam>
    /// <returns>A collection of specific type nodes visited.</returns>
    public static IEnumerable<T> Walk<T>(this Node node) where T : Node
    {
        return node.Walk().OfType<T>();
    }
}
