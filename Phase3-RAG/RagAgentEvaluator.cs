using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;

namespace Phase3_RagWithEvaluation
{
    public class RagAgentEvaluator
    {
        private readonly AIAgent _agent;
        private readonly ChatConfiguration _chatConfiguration;

        public RagAgentEvaluator(AIAgent agent, IChatClient judgeChatClient)
        {
            _agent = agent;
            _chatConfiguration = new ChatConfiguration(judgeChatClient);
        }

        public async Task RunAsync(string[] goldenQuestions)
        {
            Console.WriteLine($"\nRunning quality eval suite: {goldenQuestions.Length} questions...\n");

            int questionNumber = 1;
            foreach (var question in goldenQuestions)
            {
                Console.Write(""); 
                Console.Write("");
                Console.WriteLine($"Question {questionNumber}: question");
                AgentEvaluationResults results = await _agent.EvaluateAsync(
                    [question],
                    new CompositeEvaluator(new RelevanceEvaluator(), new GroundednessEvaluator(), new CoherenceEvaluator()),
                    chatConfiguration: _chatConfiguration);

                foreach (var item in results.Items)
                {
                    foreach (var metric in item.Metrics)
                    {
                        Console.Write($"");
                        Console.Write($"metric : {metric.Key}");
                        
                        if (metric.Value?.Metadata != null)
                        {
                            Console.WriteLine($"- MataData Details Below:");
                            foreach (var m in metric.Value?.Metadata)
                            {
                                Console.WriteLine($"-- {m.Key} : {m.Value}");
                            }
                        }
                        else { Console.WriteLine($"- MataData is null"); }

                        if (metric.Value?.Diagnostics != null)
                        {
                            Console.WriteLine($"- Diagnostics Details Below:");
                            foreach (var d in metric.Value?.Diagnostics)
                            {
                                Console.WriteLine($"-- Severity : {d.Severity}, message - {d.Message}");
                                Console.WriteLine($"-- Message : {d.Message}");
                            }
                        }
                        else { Console.WriteLine($"- Diagnostics is null"); }
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(10)); // give the free tier room to breathe
                questionNumber++;
            }
            Console.WriteLine();
        }
    }
}
