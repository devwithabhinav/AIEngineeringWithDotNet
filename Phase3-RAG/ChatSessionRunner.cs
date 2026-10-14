using Microsoft.Agents.AI;

namespace Phase3_RagWithEvaluation
{
    public class ChatSessionRunner
    {
        private readonly AIAgent _agent;

        public ChatSessionRunner(AIAgent agent)
        {
            _agent = agent;
        }

        public async Task RunAsync()
        {
            AgentSession session = await _agent.CreateSessionAsync();

            Console.WriteLine("RAG agent ready. Type 'exit' to quit.\n");

            while (true)
            {
                Console.Write("You: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input) || input == "exit") break;

                var response = await _agent.RunAsync(input, session);
                Console.WriteLine($"Assistant: {response.Text}\n");
            }
        }
    }
}