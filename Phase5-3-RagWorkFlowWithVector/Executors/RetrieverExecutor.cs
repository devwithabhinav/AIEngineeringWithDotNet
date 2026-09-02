using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.VectorData;
using Phase5_3_RagWorkFlowWithVector.Models;
using Phase5_3_RagWorkFlowWithVector.VectorStore;

namespace Phase5_3_RagWorkFlowWithVector.Executors;

/// <summary>
/// Real semantic retrieval via a vector store collection, injected in.
/// Program.cs owns creating/seeding the collection (an in-memory store for
/// now) - this executor just queries it, so swapping the in-memory
/// provider for Qdrant/pgvector/etc. later is a change to Program.cs only,
/// not here.
/// </summary>
internal sealed class RetrieverExecutor(VectorStoreCollection<string, RetrievedDocument> documents)
    : Executor<string, RetrievalContext>("Retriever")
{
    private const int TopK = 3;

    public override async ValueTask<RetrievalContext> HandleAsync(
        string query, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var chunks = new List<RetrievedChunk>();

        // SearchAsync takes the raw query string directly - the collection's
        // registered IEmbeddingGenerator (wired up in Program.cs) embeds it
        // automatically, same as it did for each document text on upsert.
        await foreach (var result in documents.SearchAsync(query, top: TopK, cancellationToken: cancellationToken))
        {
            chunks.Add(new RetrievedChunk(result.Record.SourceId, result.Record.Text, result.Score ?? 0));
        }

        return new RetrievalContext(query, chunks);
    }
}