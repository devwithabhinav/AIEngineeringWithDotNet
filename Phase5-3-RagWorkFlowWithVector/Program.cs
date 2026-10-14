using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Phase6A_FanOutFanIn_VectorStore.Agents;
using Phase6A_FanOutFanIn_VectorStore.Config;
using Phase6A_FanOutFanIn_VectorStore.Executors;
using Phase6A_FanOutFanIn_VectorStore.Models;
using Phase6A_FanOutFanIn_VectorStore.VectorStore;

namespace Phase6A_FanOutFanIn_VectorStore
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            // Load environment variables from a .env file (helper outside this file).
            EnvLoader.LoadDotEnv();

            await MainAsync(args);
        }
        public static async Task MainAsync(string[] args)
        {


            // ---------------------------------------------------------------------
            // 1. Wire up Gemini as the underlying IChatClient for every agent, and a
            //    separate Gemini embedding generator for the vector store.
            // ---------------------------------------------------------------------
            IChatClient geminiClient = GeminiChatClientFactory.Create();
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = GeminiEmbeddingGeneratorFactory.Create();

            // ---------------------------------------------------------------------
            // 1b. Create the in-memory vector store, get a collection, and seed it.
            //     This runs once at startup. Swapping the provider later (Qdrant,
            //     pgvector, SQLite, etc.) means changing this block only - everything
            //     downstream talks to VectorStoreCollection<string, RetrievedDocument>,
            //     which is provider-agnostic.
            // ---------------------------------------------------------------------
            var vectorStore = new InMemoryVectorStore(new InMemoryVectorStoreOptions
            {
                EmbeddingGenerator = embeddingGenerator
            });

            VectorStoreCollection<string, RetrievedDocument> documents =
                vectorStore.GetCollection<string, RetrievedDocument>("documents");

            await documents.EnsureCollectionExistsAsync();

            foreach ((string id, string text) in SeedDocuments.All)
            {
                await documents.UpsertAsync(new RetrievedDocument
                {
                    Id = id,
                    SourceId = id,
                    Text = text
                });
            }

            // ---------------------------------------------------------------------
            // 2. Create the four role-specific agents (same chat client, different
            //    system instructions - see Agents/AgentFactory.cs).
            // ---------------------------------------------------------------------
            ChatClientAgent generatorAgent = AgentFactory.CreateGenerator(geminiClient);
            ChatClientAgent groundednessAgent = AgentFactory.CreateGroundednessJudge(geminiClient);
            ChatClientAgent relevanceAgent = AgentFactory.CreateRelevanceJudge(geminiClient);
            ChatClientAgent synthesizerAgent = AgentFactory.CreateSynthesizer(geminiClient);

            // ---------------------------------------------------------------------
            // 3. Wrap agents (and plain logic) in executors.
            // ---------------------------------------------------------------------
            var retriever = new RetrieverExecutor(documents);
            var generator = new GeneratorExecutor(generatorAgent);
            var groundednessEvaluator = new GroundednessEvaluatorExecutor(groundednessAgent);
            var relevanceEvaluator = new RelevanceEvaluatorExecutor(relevanceAgent);
            var aggregator = new AggregatorExecutor(synthesizerAgent);

            // ---------------------------------------------------------------------
            // 4. Build the graph:
            //
            //    Retriever -> Generator -> [fan-out] -> GroundednessEvaluator -\
            //                            \                                       -> Aggregator (also runs synthesis, yields output)
            //                             -> RelevanceEvaluator ------------------/
            //
            //    Aggregator is both the fan-in target AND the terminal node - it
            //    accumulates both evaluator results, then runs the synthesis step
            //    in-line and yields the final PipelineResult directly via
            //    context.YieldOutputAsync, rather than forwarding to a separate
            //    Synthesizer executor. See AggregatorExecutor.cs for why.
            //
            //    Remember the superstep rule: the workflow will not advance past the
            //    fan-out superstep until BOTH evaluators finish, even though they're
            //    running concurrently.
            // ---------------------------------------------------------------------
            Workflow workflow = new WorkflowBuilder(retriever)
                .AddEdge(retriever, generator)
                .AddFanOutEdge(generator, [groundednessEvaluator, relevanceEvaluator])
                .AddFanInBarrierEdge([groundednessEvaluator, relevanceEvaluator], aggregator)
                .WithOutputFrom(aggregator)
                .Build();

            // ---------------------------------------------------------------------
            // 5. Run it.
            // ---------------------------------------------------------------------
            string query = args.Length > 0
                ? string.Join(' ', args)
                : "How does fan-out and fan-in work in Agent Framework workflows?";
            //negative : What pizza toppings pair well with fan-out and executors?

            Console.WriteLine($"Query: {query}\n");

            // InProcessExecution.RunAsync (non-streaming) + run.NewEvents does not
            // reliably drive the workflow to completion when the graph contains
            // agent-backed executors - see microsoft/agent-framework#1315. It can
            // return having only captured the first superstep or two, which is why
            // Aggregator/Synthesizer never showed up. RunStreamingAsync + WatchStreamAsync
            // is the documented, working path.
            await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, query);

            await foreach (WorkflowEvent evt in run.WatchStreamAsync())
            {
                switch (evt)
                {
                    // Fires once per executor as it finishes. Data is whatever THAT
                    // executor returned - EvaluationResult for the two evaluators,
                    // null for Aggregator specifically (it yields its result via
                    // context.YieldOutputAsync instead of returning a value), and so
                    // on. This is a trace of the graph, not the final answer.
                    case ExecutorCompletedEvent completed:
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine($"[{completed.ExecutorId}] completed. Data: {completed.Data}");
                        break;

                    // Fires once, when Aggregator calls context.YieldOutputAsync(...).
                    // This is the actual final answer, not ExecutorCompletedEvent.
                    case WorkflowOutputEvent output when output.Data is PipelineResult result:
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine("=== Final Result ===");
                        Console.WriteLine($"Query:            {result.Query}");
                        Console.WriteLine($"Groundedness:     {result.GroundednessScore}/5");
                        Console.WriteLine($"Relevance:        {result.RelevanceScore}/5");
                        Console.WriteLine($"Accepted as-is:   {result.Accepted}");
                        Console.WriteLine($"Synthesizer note: {result.SynthesizerNotes}");
                        Console.WriteLine();
                        Console.WriteLine("Final answer:");
                        Console.WriteLine(result.FinalAnswer);
                        break;

                    case WorkflowErrorEvent error:
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.Error.WriteLine($"Workflow error: {error.Exception}");
                        break;
                }
            }
        }
    }
}


