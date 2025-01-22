using System.Runtime.CompilerServices;
using Acornima.Ast;

namespace ChunkSearch;

public static class ModuleExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<ChunkLoader> GetChunkLoaders(this Module module)
    {
        return module.Body.SelectMany(x => x.GetChunkLoaders());
    }
}
