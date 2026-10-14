using GeminiDotnet;
using GeminiDotnet.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Phase4_SequentialMultiAgentWorkflow
{
    public class Program
    {
        static void Main(string[] args)
        {
            CallAsync().GetAwaiter().GetResult();
        }
        static async Task CallAsync()
        {
            EnvLoader.LoadDotEnv();

            // Read configuration from environment variables.
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")!;
            var modelId = Environment.GetEnvironmentVariable("GEMINI_MODEL_ID") ?? "gemini-3-flash";

            IChatClient chatClient = new GeminiChatClient(new GeminiClientOptions
            {
                ApiKey = apiKey,
                ModelId = modelId
            });

            // --- Two narrow, single-purpose agents ---
            AIAgent researcherAgent = chatClient.AsAIAgent(
                instructions: "You are a researcher. Given a topic, produce 4-5 concise bullet-point findings. " +
                              "No fluff, no writing style — just factual findings.",
                name: "Researcher");

            AIAgent writerAgent = chatClient.AsAIAgent(
                instructions: "You are a writer. Given research findings, turn them into a short, " +
                              "engaging 3-paragraph summary suitable for a blog post.",
                name: "Writer");

            // --- Human-in-the-loop gate: pauses the workflow, waits for approval ---
            // NOTE: exact factory method unconfirmed — try RequestPort.Create<string, bool>("publish-approval")
            // first; if that doesn't compile, check IntelliSense on "RequestPort." for the real static member.
            RequestPort<string, bool> approvalPort = RequestPort.Create<string, bool>("publish-approval");

            // --- Sequential pipeline: known-good, documented pattern ---
            var workflow = AgentWorkflowBuilder.BuildSequential([researcherAgent, writerAgent]);

            Console.Write("Enter a topic to research and write about: ");
            var topic = Console.ReadLine() ?? "Microsoft Agent Framework";

            var messages = new List<ChatMessage> { new(ChatRole.User, topic) };

            await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, messages);
            await run.TrySendMessageAsync(new TurnToken(emitEvents: true)); // <-- the missing piece

            string? draftForApproval = null;

            await foreach (WorkflowEvent evt in run.WatchStreamAsync())
            {
                switch (evt)
                {
                    case AgentResponseUpdateEvent update:
                        Console.Write(update.Update.Text); // live streaming output
                        break;

                    case ExecutorFailedEvent failed:
                        Console.WriteLine($"\n[FAILED] {failed.ExecutorId}: {failed}");
                        break;

                    case WorkflowErrorEvent error:
                        Console.WriteLine($"\n[WORKFLOW ERROR] {error.Exception?.Message}");
                        break;

                    case WorkflowOutputEvent outputEvt:
                        draftForApproval = outputEvt.Data?.ToString();
                        break;
                }
            }

            // --- Human-in-the-loop, done as plain code after the pipeline finishes ---
            if (draftForApproval != null)
            {
                Console.WriteLine("\n\n--- FINAL DRAFT ---\n");
                Console.WriteLine(draftForApproval);
                Console.WriteLine("\n-------------------\n");

                Console.Write("Approve this for publishing? (y/n): ");
                var input = Console.ReadLine();
                bool approved = input?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) == true;

                Console.WriteLine(approved ? "\n✅ Published." : "\n❌ Rejected — not published.");
            }
            else
            {
                Console.WriteLine("\nWorkflow ended without a draft.");
            }
        }
    }
}
