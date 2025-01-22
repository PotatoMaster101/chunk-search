namespace ChunkSearch;

public class ChunkEntry(string chunkId, string chunkFile)
{
    public string ChunkId { get; } = chunkId;
    public string ChunkFile { get; } = chunkFile;

    public override string ToString()
    {
        return $"{ChunkId}: {ChunkFile}";
    }
}
