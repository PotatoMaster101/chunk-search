namespace ChunkSearch.Chunk;

/// <summary>
/// Represents an entry in a chunk loader.
/// </summary>
/// <param name="chunkId">The ID of the entry.</param>
/// <param name="chunkFile">The chunk filename.</param>
public class ChunkEntry(ChunkId chunkId, string chunkFile)
{
    /// <inheritdoc />
    public override string ToString()
    {
        return $"{chunkId}: {chunkFile}";
    }
}
