using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Phase5_2_RagSequentialWorkFlow.Agents;
using Phase5_2_RagSequentialWorkFlow.Config;
using Phase5_2_RagSequentialWorkFlow.Executors;
using Phase5_2_RagSequentialWorkFlow.Models;
using System.Threading.Tasks;

namespace Phase5_2_RagSequentialWorkFlow
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
            // 1. Wire up Gemini as the underlying IChatClient for every agent.
            // ---------------------------------------------------------------------
            IChatClient geminiClient = GeminiChatClientfactory.Create();

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
            var retriever = new RetrieverExecutor();
            var generator = new GeneratorExecutor(generatorAgent);
            var groundednessEvaluator = new GroundednessEvaluatorExecutor(groundednessAgent);
            var relevanceEvaluator = new RelevanceEvaluatorExecutor(relevanceAgent);
            var aggregator = new AggregatorExecutor(synthesizerAgent);
            //var synthesizer = new SynthesizerExecutor(synthesizerAgent);

            // ---------------------------------------------------------------------
            // 4. Build the graph:
            //
            //    Retriever -> Generator -> [fan-out] -> GroundednessEvaluator -\
            //                            \                                       -> Aggregator -> Synthesizer -> (output)
            //                             -> RelevanceEvaluator ------------------/
            //
            //    Remember the superstep rule: the workflow will not advance past the
            //    fan-out superstep until BOTH evaluators finish, even though they're
            //    running concurrently.
            // ---------------------------------------------------------------------
            Workflow workflow = new WorkflowBuilder(retriever)
                .AddEdge(retriever, generator)
                .AddFanOutEdge(generator, [groundednessEvaluator, relevanceEvaluator])
                .AddFanInBarrierEdge([groundednessEvaluator, relevanceEvaluator], aggregator)
                //.AddEdge(aggregator, synthesizer)
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