using GeminiDotnet;
using GeminiDotnet.Extensions.AI;
using Microsoft.Extensions.AI;

namespace Phase0_1_ChatClientAndAgents.Phase0
{
    internal class _0_JustChatClient
    {
        public static async Task RunAsync(string apiKey, string modelId)
        {
            var options = new GeminiClientOptions
            {
                ApiKey = apiKey,
                ModelId = modelId // fast + cheap, good for learning
            };

            IChatClient chatClient = new GeminiChatClient(options);

            // --- The actual call: no agent, no framework, just IChatClient ---
            Console.Write("You: ");
            var userPrompt = Console.ReadLine() ?? "What is AI engineering?";
            var chatOptions = new ChatOptions
            {
                Temperature = 0, // 0 means deterministic, 1 means more creative
            };

            Console.Write("\nGemini: ");

            //await foreach (var update in chatClient.GetStreamingResponseAsync(userPrompt, chatOptions))
            //{
            //    Console.Write(update); // prints tokens as they arrive
            //}
        }
    }
}
