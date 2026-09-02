using Microsoft.Agents.AI;

namespace Phase3_RAG
{
    public interface IKnowledgeBase
    {
        Task IngestAsync(IEnumerable<TextChunk> chunks);
        Task<IEnumerable<TextSearchProvider.TextSearchResult>> SearchAsync(string query, CancellationToken ct);
    }
}
