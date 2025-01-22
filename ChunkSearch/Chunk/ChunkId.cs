using Acornima;
using Acornima.Ast;
using ChunkSearch.Extensions;

namespace ChunkSearch.Chunk;

/// <summary>
/// Represents the ID of a chunk loader entry.
/// </summary>
public class ChunkId
{
    private readonly string _content;
    private readonly bool _quote;

    /// <summary>
    /// Constructs a new instance of <see cref="ChunkId"/>.
    /// </summary>
    /// <param name="content">The ID value.</param>
    /// <param name="quote">Whether the ID is a string, hence added quote.</param>
    private ChunkId(string content, bool quote = true)
    {
        _content = content;
        _quote = quote;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return _quote ? $"\"{_content}\"" : _content;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ChunkId chunkId && _content == chunkId._content;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _content.GetHashCode();
    }


    /// <summary>
    /// Returns the chunk ID that is inside a comparison such as <c>if</c> and ternary (e.g., <c>e === 1</c>).
    /// </summary>
    /// <param name="expression">The comparison expression.</param>
    /// <returns>The chunk ID that is inside the comparison.</returns>
    public static ChunkId? FromBinaryExpression(BinaryExpression expression)
    {
        if (expression.Operator != Operator.Equality && expression.Operator != Operator.StrictEquality)
            return null;
        if (expression is { Left: Literal { Value: not null } left, Right: Identifier })
            return new ChunkId(left.Value.ToString()!, left.Kind is TokenKind.StringLiteral);
        if (expression is { Left: Identifier, Right: Literal { Value: not null } right })
            return new ChunkId(right.Value.ToString()!, right.Kind is TokenKind.StringLiteral);
        return null;
    }

    /// <summary>
    /// Returns the chunk IDs that are inside an object expression (e.g., <c>{1:'chunk'}[e] + '.js'</c>).
    /// </summary>
    /// <param name="expression">The object expressions.</param>
    /// <returns>The chunk IDs that are inside the object expressions.</returns>
    public static IReadOnlySet<ChunkId> FromObjectExpression(ObjectExpression expression)
    {
        var result = new HashSet<ChunkId>();
        foreach (var node in expression.Walk<Property>())
        {
            if (node.Key is Identifier identifier)
                result.Add(new ChunkId(identifier.Name));
            else if (node.Key is Literal { Value: not null } literal)
                result.Add(new ChunkId(literal.Value.ToString()!, literal.Kind is TokenKind.StringLiteral));
        }
        return result;
    }

    /// <summary>
    /// Returns the chunk IDs that are inside a <c>switch</c> (e.g., <c>switch(e) {case 1: return 'chunk.js'}</c>).
    /// </summary>
    /// <param name="statement">The switch statement.</param>
    /// <returns>The chunk IDs that are inside the switch statement.</returns>
    public static IReadOnlySet<ChunkId> FromSwitchStatement(SwitchStatement statement)
    {
        var result = new HashSet<ChunkId>();
        foreach (var node in statement.Cases)
        {
            if (node.Test is Literal { Value: not null } literal)
                result.Add(new ChunkId(literal.Value.ToString()!, literal.Kind is TokenKind.StringLiteral));
        }
        return result;
    }
}
