using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace Phase3_RAG
{
    public class KnowledgeBase : IKnowledgeBase
    {
        private readonly VectorStoreCollection<string, TextChunk> _collection;
        private readonly int _topResults;

        public KnowledgeBase(
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
            string collectionName,
            int embeddingDimensions,
            int topResults = 2)
        {
            _topResults = topResults;

            var vectorStore = new InMemoryVectorStore(new InMemoryVectorStoreOptions
            {
                EmbeddingGenerator = embeddingGenerator
            });

            var definition = new VectorStoreCollectionDefinition
            {
                Properties =
                [
                    new VectorStoreKeyProperty("SourceId", typeof(string)),
                new VectorStoreDataProperty("SourceName", typeof(string)),
                new VectorStoreDataProperty("Text", typeof(string)),
                new VectorStoreVectorProperty("Embedding", typeof(string), embeddingDimensions)
                ]
            };

            _collection = vectorStore.GetCollection<string, TextChunk>(collectionName, definition);
        }

        public async Task IngestAsync(IEnumerable<TextChunk> chunks)
        {
            await _collection.EnsureCollectionExistsAsync();

            // Embedding must mirror Text so the store auto-generates the real vector
            var chunkList = chunks.ToList();
            foreach (var chunk in chunkList)
                chunk.Embedding = chunk.Text;

            await _collection.UpsertAsync(chunkList);

            // Diagnostic probe: ensure a search against an ingested chunk returns results.
            if (chunkList.Any())
            {
                var sampleText = chunkList[0].Text;
                var found = new List<TextSearchProvider.TextSearchResult>();
                await foreach (var match in _collection.SearchAsync(sampleText, top: _topResults, cancellationToken: CancellationToken.None))
                {
                    found.Add(new TextSearchProvider.TextSearchResult
                    {
                        SourceName = match.Record.SourceName,
                        Text = match.Record.Text
                    });
                }

                if (!found.Any())
                {
                    Console.WriteLine("[RAG][Warning] Vector search returned 0 results for a sample ingested document. " +
                        "Possible causes: embedding dimension mismatch, embedding generator not configured, or upsert failed.");
                }
                else
                {
                    Console.WriteLine($"[RAG][Info] Diagnostic search returned {found.Count} result(s): {string.Join(", ", found.Select(f => f.SourceName))}");
                }
            }
        }

        public async Task<IEnumerable<TextSearchProvider.TextSearchResult>> SearchAsync(string query, CancellationToken ct)
        {
            var results = new List<TextSearchProvider.TextSearchResult>();

            await foreach (var match in _collection.SearchAsync(query, top: _topResults, cancellationToken: ct))
            {
                results.Add(new TextSearchProvider.TextSearchResult
                {
                    SourceName = match.Record.SourceName,
                    Text = match.Record.Text
                });
            }

            return results;
        }
    }
}