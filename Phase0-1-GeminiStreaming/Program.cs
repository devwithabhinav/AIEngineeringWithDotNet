// --- Setup: build the IChatClient for Gemini ---
//var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
//    ?? throw new InvalidOperationException("Set GEMINI_API_KEY environment variable first.");

using Phase0_1_ChatClientAndAgents;

EnvLoader.LoadDotEnv(); // Try load .env from project tree (if present) so secrets can be kept out of source.

// Read config from environment
var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
var modelId = Environment.GetEnvironmentVariable("GEMINI_MODEL_ID") ?? "gemini-3-flash";

/// First Chapter: Just Chat with Gemini without any agent or framework. This is the simplest way to use Gemini.
//Phase0_1_ChatClientAndAgents.Phase0._0_JustChatClient.RunAsync(apiKey, modelId).GetAwaiter().GetResult();

/// Second Chapter: Chat with Gemini using an Agent. This is the simplest way to use Gemini with an agent.
/// In This AgentSession is used to maintain the context of the conversation and history. The agent can use tools to answer questions or perform actions.
Phase0_1_ChatClientAndAgents.Phase1._1_ChatWithAgent.RunAsync(apiKey, modelId).GetAwaiter().GetResult();


Console.WriteLine();