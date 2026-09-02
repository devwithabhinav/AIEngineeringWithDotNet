using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Phase5_2_RagSequentialWorkFlow.Models;

namespace Phase5_2_RagSequentialWorkFlow.Executors;

/// <summary>
/// Deterministic, non-agent executor. Not every node needs an LLM behind it -
/// this is the point of MAF letting you mix agents and plain code in the
/// same graph.
///
/// STUB: replace the body of HandleAsync with your actual Phase 3 vector
/// store call (e.g. Qdrant/Pinecone/pgvector query + embedding client).
/// The in-memory doc list here exists only so the whole solution runs
/// end-to-end without external dependencies.
/// </summary>
internal sealed class RetrieverExecutor(): Executor<string, RetrievalContext>("Retriever")
//: ReflectingExecutor<RetrieverExecutor>("Retriever"), IMessageHandler<string, RetrievalContext>
{
    // Toy corpus standing in for your vector store.
    private static readonly (string Id, string Text)[] Corpus =
    [
        ("doc-1", "Microsoft Agent Framework workflows use a graph of executors " +
                  "connected by edges. Within a superstep, all triggered executors " +
                  "run in parallel, but the workflow does not advance to the next " +
                  "superstep until every executor in the current one completes."),
        ("doc-2", "Fan-out/fan-in in Agent Framework is done via AddFanOutEdge to " +
                  "send a message to multiple executors in parallel, and " +
                  "AddFanInBarrierEdge to wait for all of them before proceeding."),
        ("doc-3", "Agent Framework supports four built-in multi-agent orchestration " +
                  "patterns: sequential, concurrent, hand-off, and magentic."),
        ("doc-4", "Checkpointing lets a workflow save its state so long-running " +
                  "processes can be recovered and resumed, and workflows can run " +
                  "on a durable host without changing the executor code."),
    ];

    public override ValueTask<RetrievalContext> HandleAsync(
         string query, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // Naive keyword overlap "retrieval" - obviously not real semantic
        // search. Swap for: var hits = await _vectorStore.QueryAsync(query, topK: 4);
        string[] queryTerms = query.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var ranked = Corpus
            .Select(d => new
            {
                d.Id,
                d.Text,
                Score = queryTerms.Count(t => d.Text.ToLowerInvariant().Contains(t))
            })
            .Where(d => d.Score > 0)
            .OrderByDescending(d => d.Score)
            .Take(3)
            .ToList();

        var chunks = ranked
            .Select(r => new RetrievedChunk(r.Id, r.Text, r.Score / (double)queryTerms.Length))
            .ToList();

        return ValueTask.FromResult(new RetrievalContext(query, chunks));
    }
}