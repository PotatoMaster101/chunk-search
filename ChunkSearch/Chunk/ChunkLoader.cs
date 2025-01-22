using System.Dynamic;
using Acornima;
using Acornima.Ast;
using ChunkSearch.Extensions;
using Jint;

namespace ChunkSearch.Chunk;

/// <summary>
/// Represents a chunk loader.
/// </summary>
public class ChunkLoader
{
    private readonly Node _node;
    private readonly IReadOnlyDictionary<string, AssignmentExpression> _assignments;

    /// <summary>
    /// Constructs a new <see cref="ChunkLoader"/>.
    /// </summary>
    /// <param name="node">The node containing the loader.</param>
    /// <param name="assignments">The map containing the non-function assignments.</param>
    private ChunkLoader(Node node, IReadOnlyDictionary<string, AssignmentExpression> assignments)
    {
        _node = node;
        _assignments = assignments;
    }

    /// <summary>
    /// Gets a list of chunk IDs.
    /// </summary>
    /// <returns>The list of chunk IDs.</returns>
    public IReadOnlyCollection<ChunkId> GetChunkIds()
    {
        var result = new HashSet<ChunkId>();
        foreach (var child in _node.Walk())
        {
            if (child is ObjectExpression obj)
                result.UnionWith(ChunkId.FromObjectExpression(obj));
            else if (child is SwitchStatement swi)
                result.UnionWith(ChunkId.FromSwitchStatement(swi));
            else if (child is BinaryExpression bin && ChunkId.FromBinaryExpression(bin) is { } chunk)
                result.Add(chunk);
        }
        return result;
    }

    /// <summary>
    /// Executes the chunk loader and returns the result.
    /// </summary>
    /// <param name="args">The arguments for the chunk loader.</param>
    /// <returns>The result of execution.</returns>
    public IReadOnlyCollection<ChunkEntry> Run(IEnumerable<ChunkId> args)
    {
        using var engine = new Engine();
        var result = new HashSet<ChunkEntry>();
        try
        {
            DeclareVariables(engine);
            foreach (var arg in args)
            {
                var eval = engine.Evaluate($"({_node.ToJavaScript()})({arg});").ToString();
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

    /// <inheritdoc />
    public override string ToString()
    {
        return _node.ToJavaScript(true);
    }

    /// <summary>
    /// Creates the necessary variables required for the loader to run.
    /// </summary>
    /// <param name="engine">The JavaScript engine.</param>
    private void DeclareVariables(Engine engine)
    {
        var seen = new HashSet<string>();
        foreach (var member in _node.Walk<MemberExpression>())
        {
            if (member.Object is not Identifier obj)
                continue;
            if (!_assignments.TryGetValue(member.ToJavaScript(), out var assignment))
                continue;

            if (seen.Add(obj.Name))
                engine.SetValue(obj.Name, new ExpandoObject());
            engine.Execute(assignment.ToJavaScript());
        }
    }

    /// <summary>
    /// Gets a list of chunk loaders from a root node.
    /// </summary>
    /// <param name="root">The root node to get the chunk loaders.</param>
    /// <returns>The list of chunk loaders.</returns>
    public static IEnumerable<ChunkLoader> FromRootNode(Node root)
    {
        var loaders = new List<Node>();
        var assignments = new Dictionary<string, AssignmentExpression>();
        foreach (var node in root.Walk())
        {
            if (IsChunkLoader(node))
                loaders.Add(node);

            if (node is not AssignmentExpression { Operator: Operator.Assignment } assignment)
                continue;
            if (assignment.Left is not MemberExpression)
                continue;
            if (assignment.Right is FunctionExpression or ArrowFunctionExpression)
                continue;

            assignments[assignment.Left.ToJavaScript()] = assignment;
        }
        return loaders.Select(x => new ChunkLoader(x, assignments));
    }

    /// <summary>
    /// Determines whether the node is likely a chunk loader.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>Whether the node is likely a chunk loader.</returns>
    private static bool IsChunkLoader(Node node)
    {
        if (node is not FunctionExpression && node is not ArrowFunctionExpression)
            return false;
        if (((IFunction)node).Params.Count != 1)
            return false;

        var hasJsLiteral = false;
        foreach (var child in node.Walk())
        {
            if (child is ForStatement or ForOfStatement or WhileStatement or DoWhileStatement or CallExpression)
                return false;
            if (child is Literal { Value: string value } && value.EndsWith(".js") && !value.Contains('/'))
                hasJsLiteral = true;
        }
        return hasJsLiteral;
    }
}
