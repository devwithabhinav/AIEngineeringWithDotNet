using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Phase5_2_RagSequentialWorkFlow.Models;

namespace Phase5_2_RagSequentialWorkFlow.Executors;

internal sealed class RelevanceEvaluatorExecutor(ChatClientAgent judgeAgent)
    : Executor<CandidateAnswer, EvaluationResult>("RelevanceEvaluator")
{
    public override async ValueTask<EvaluationResult> HandleAsync(
        CandidateAnswer candidate, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        string prompt = $"""
            QUERY: {candidate.Query}

            ANSWER:
            {candidate.Answer}
            """;

        var response = await judgeAgent.RunAsync(prompt, cancellationToken: cancellationToken);
        JudgeScoreDto score = JudgeScoreParser.Parse(response.Text);

        return new EvaluationResult(
            RunId: candidate.RunId,
            Metric: "Relevance",
            Score: score.Score,
            Rationale: score.Rationale,
            Candidate: candidate);
    }
}