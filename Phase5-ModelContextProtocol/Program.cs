using GeminiDotnet;
using GeminiDotnet.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace Phase5_ModelContextProtocol
{
    public class Program
    {
        static void Main(string[] args)
        {
            OnlyMCPAsync(args).GetAwaiter().GetResult();
        }
        public static async Task OnlyMCPAsync(string[] args)
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

            // --- Point this at a REAL project folder you want the assistant to understand ---
            // Example: your Phase 2 Web API project.
            var projectFolder = @"C:\Users\abhi8\OneDrive\Documents\Abhinav data\Desktop\HRMS\BGIR_HRMS\BGIR_HRMS\App_Code";

            if (!Directory.Exists(projectFolder))
                throw new DirectoryNotFoundException($"Update projectFolder to point at a real folder. Not found: {projectFolder}");

            // --- Connect to the filesystem MCP server, scoped to that project ---
            await using var mcpClient = await McpClient.CreateAsync(
                new StdioClientTransport(new StdioClientTransportOptions
                {
                    Command = "npx",
                    Arguments = ["-y", "@modelcontextprotocol/server-filesystem", projectFolder]
                }));

            var mcpTools = await mcpClient.ListToolsAsync();

            Console.WriteLine("Discovered MCP tools:");
            foreach (var tool in mcpTools)
                Console.WriteLine($" - {tool.Name}: {tool.Description}");
            Console.WriteLine();

            // --- Guardrail: this assistant should only ever READ the codebase, never modify it ---
            var readOnlyTools = mcpTools
                .Where(t => !t.Name.Contains("write", StringComparison.OrdinalIgnoreCase)
                         && !t.Name.Contains("delete", StringComparison.OrdinalIgnoreCase)
                         && !t.Name.Contains("move", StringComparison.OrdinalIgnoreCase)
                         && !t.Name.Contains("edit", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Console.WriteLine($"Allowing {readOnlyTools.Count} read-only tools out of {mcpTools.Count} discovered.\n");

            // --- Build the Codebase Assistant ---
            AIAgent codebaseAssistant = chatClient.AsAIAgent(
                instructions: "You are a senior .NET engineer helping a colleague understand a codebase. " +
                              "Use the available tools to explore files. Reference exact file names in your answers. " +
                              "You have READ-ONLY access — never claim to modify or create files.",
                name: "CodebaseAssistant",
                tools: readOnlyTools.Cast<AITool>().ToArray());

            AgentSession session = await codebaseAssistant.CreateSessionAsync();

            Console.WriteLine("Codebase Assistant ready. Type 'exit' to quit.\n");

            while (true)
            {
                Console.Write("You: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input) || input == "exit") break;

                var response = await codebaseAssistant.RunAsync(input, session);
                Console.WriteLine($"Assistant: {response.Text}\n");
            }
        }
    }
}


