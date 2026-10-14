using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Phase6A_FanOutFanIn_VectorStore.Models;

namespace Phase6A_FanOutFanIn_VectorStore.Executors;

internal sealed class GroundednessEvaluatorExecutor(ChatClientAgent judgeAgent)
    : Executor<CandidateAnswer, EvaluationResult>("GroundednessEvaluator")
{
    public override async ValueTask<EvaluationResult> HandleAsync(
        CandidateAnswer candidate, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        string prompt = $"""
            QUERY: {candidate.Query}

            CONTEXT:
            {candidate.ContextText}

            ANSWER:
            {candidate.Answer}
            """;

        var response = await judgeAgent.RunAsync(prompt, cancellationToken: cancellationToken);
        JudgeScoreDto score = JudgeScoreParser.Parse(response.Text);

        return new EvaluationResult(
            RunId: candidate.RunId,
            Metric: "Groundedness",
            Score: score.Score,
            Rationale: score.Rationale,
            Candidate: candidate);
    }
}