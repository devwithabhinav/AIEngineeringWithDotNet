using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Phase3_RAG
{
    public class RagAgentFactory
    {
        private readonly IChatClient _chatClient;

        public RagAgentFactory(IChatClient chatClient)
        {
            _chatClient = chatClient;
        }

        public AIAgent Create(IKnowledgeBase knowledgeBase, string instructions)
        {
            var textSearchOptions = new TextSearchProviderOptions
            {
                SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke
            };

            var provider = new TextSearchProvider(knowledgeBase.SearchAsync, textSearchOptions);

            return _chatClient.AsAIAgent(new ChatClientAgentOptions
            {
                ChatOptions = new ChatOptions { Instructions = instructions , Temperature = 0.0f },
                AIContextProviders = [provider]
            });
        }
    }
}