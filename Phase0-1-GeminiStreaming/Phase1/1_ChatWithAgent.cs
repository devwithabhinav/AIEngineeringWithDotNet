using GeminiDotnet; // Core Gemini client types
using GeminiDotnet.Extensions.AI; // Extension methods and helpers to convert clients into agents
using Microsoft.Agents.AI; // Agent types (AIAgent, AgentSession, etc.)
using Microsoft.Extensions.AI; // AI related abstractions (IChatClient, AIFunctionFactory, etc.)

namespace Phase0_1_GeminiStreaming.Phase1
{
    // Small demo class that shows how to create and run a simple AI agent chat loop.
    internal class _1_ChatWithAgent
    {
        // --- Structured output target type ---
        record ReminderInfo(string Title, string DueDate, string Priority);

        // Entry point for this demo: creates a chat-based AI agent and runs an interactive loop.
        // apiKey: API key to authenticate with the Gemini service.
        // modelId: Identifier of the model to use (e.g., a fast/cheap model for learning).
        public static async Task RunAsync(string apiKey, string modelId)
        {
            // Create an instance that groups helper functions (tools) we want our agent to use.
            AgentTools tools = new AgentTools();

            // Configure the Gemini client (credentials + model selection).
            // This object is passed to the GeminiChatClient constructor to create a chat client.
            var options = new GeminiClientOptions
            {
                ApiKey = apiKey,
                ModelId = modelId // Use the provided model id — often a smaller/cheaper model for experiments.
            };

            // Create a chat client that speaks to Gemini using the configured options.
            IChatClient chatClient = new GeminiChatClient(options);

            // --- Turn the chat client into an agent and register tools ---
            // AsAIAgent wraps the chat client into an agent with a name, high-level instructions,
            // and a set of tool functions the agent can call when appropriate.
            // Agent #1: conversational, has tools, no structured output
            AIAgent agent = chatClient.AsAIAgent(
                name: "PersonalAssistant", // Friendly agent name useful for logs or tool labels.
                instructions: "You are a concise personal assistant. Use tools when relevant instead of guessing.", // System-like instruction guiding agent behavior.
                tools: [
                    // Register each tool (wrapped as an AI-callable function) so the agent can invoke them.
                    AIFunctionFactory.Create(tools.GetCurrentTime), // Tool to get current time
                    AIFunctionFactory.Create(tools.Add),            // Tool to perform arithmetic addition
                    AIFunctionFactory.Create(tools.LookUpFact)     // Tool to lookup facts from a datastore
                ]);

            // Agent #2: dedicated to structured extraction, NO tools attached
            AIAgent extractorAgent = chatClient.AsAIAgent(
                instructions: "You extract structured reminder data from user text.",
                name: "ReminderExtractor");

            // Create a session for the agent. A session may maintain state or conversation context.
            AgentSession session = await agent.CreateSessionAsync();

            // Inform the user the assistant is ready and how to exit.
            Console.WriteLine("Assistant ready. Type 'exit' to quit.\n");

            // Start a simple REPL: read user input, send to the agent, print the response.
            while (true)
            {
                Console.Write("You: ");
                var input = Console.ReadLine(); // Read a line of text from the console
                // Exit condition: empty input or the literal "exit"
                if (string.IsNullOrWhiteSpace(input) || input == "exit") break;

                if (input.StartsWith("remind:", StringComparison.OrdinalIgnoreCase))
                {
                    var description = input["remind:".Length..].Trim();

                    var structuredResponse = await extractorAgent.RunAsync<StructuredOutput_ResponseModel>(
                        $"Extract a reminder from this: \"{description}\". " +
                        "If no due date is mentioned, use \"unspecified\". " +
                        "Priority must be Low, Medium, or High.",
                        session);

                    var reminder = structuredResponse.Result;
                    Console.WriteLine($"Assistant (structured): Title='{reminder.Title}', Due='{reminder.DueDate}', Priority='{reminder.Priority}'\n");
                }
                else
                {
                    // Send the user's input to the agent and await its response.
                    // The session object provides conversation context so the agent can maintain continuity.
                    var response = await agent.RunAsync(input, session);

                    // Print the agent's textual reply to the console.
                    Console.WriteLine($"Assistant: {response.Text}\n");
                }
            }
        }
    }
}