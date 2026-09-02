//using System.Collections.Concurrent;
//using Microsoft.Agents.AI.Workflows;
//using Phase5_2_RagSequentialWorkFlow.Models;

//namespace Phase5_2_RagSequentialWorkFlow.Executors;

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Phase5_2_RagSequentialWorkFlow.Models;

namespace Phase5_2_RagSequentialWorkFlow.Executors;

internal sealed record SynthesizerVerdictDto(
    [property: JsonPropertyName("finalAnswer")] string FinalAnswer,
    [property: JsonPropertyName("accepted")] bool Accepted,
    [property: JsonPropertyName("notes")] string Notes);

/// <summary>
/// Fan-in target AND terminal node, in one executor - this is the
/// documented shape (see Microsoft's own ConcurrentAggregationExecutor in
/// microsoft/Agent-Framework-Samples/07.Workflow), not two separate nodes.
///
/// Earlier this was split into AggregatorExecutor (accumulate, then
/// context.SendMessageAsync to a separate SynthesizerExecutor). That threw
/// "Executor 'Aggregator' cannot send messages of type 'SynthesisInput'" -
/// every official sample that manually calls context.SendMessageAsync
/// between two CUSTOM executors only ever sends the type the sender itself
/// declares (or is a special-cased raw-agent node accepting ChatMessage/
/// TurnToken). There's no confirmed example of a custom executor manually
/// forwarding an unrelated type to another custom executor mid-graph.
/// context.YieldOutputAsync(...), by contrast, is a separate mechanism for
/// producing the workflow's terminal output directly - no edge, no type
/// authorization needed - which is what the official pattern actually uses.
///
/// So: accumulate both evaluator results here, run the synthesis step
/// in-line (no separate node to hand off to), and yield the final
/// PipelineResult directly.
/// </summary>
internal sealed class AggregatorExecutor(ChatClientAgent synthesizerAgent)
    : Executor<EvaluationResult>("Aggregator")
{
    private readonly Dictionary<string, EvaluationResult> _pending = [];
    private readonly object _lock = new();

    [YieldsOutput(typeof(PipelineResult))]
    public override async ValueTask HandleAsync(
        EvaluationResult result, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        EvaluationResult? groundedness = null;
        EvaluationResult? relevance = null;

        lock (_lock)
        {
            if (_pending.Remove(result.RunId, out EvaluationResult? other))
            {
                // Second half of the pair has arrived.
                (groundedness, relevance) = result.Metric == "Groundedness"
                    ? (result, other)
                    : (other, result);
            }
            else
            {
                // First half of the pair - stash it and wait for the second.
                _pending[result.RunId] = result;
            }
        }

        if (groundedness is null || relevance is null)
        {
            return;
        }

        PipelineResult pipelineResult = await SynthesizeAsync(groundedness, relevance, cancellationToken);
        await context.YieldOutputAsync(pipelineResult);
    }

    private async Task<PipelineResult> SynthesizeAsync(
        EvaluationResult groundedness, EvaluationResult relevance, CancellationToken cancellationToken)
    {
        CandidateAnswer candidate = groundedness.Candidate;

        string prompt = $"""
            ANSWER:
            {candidate.Answer}

            GROUNDEDNESS_SCORE: {groundedness.Score}
            GROUNDEDNESS_RATIONALE: {groundedness.Rationale}

            RELEVANCE_SCORE: {relevance.Score}
            RELEVANCE_RATIONALE: {relevance.Rationale}
            """;

        var response = await synthesizerAgent.RunAsync(prompt, cancellationToken: cancellationToken);
        SynthesizerVerdictDto verdict = ParseVerdict(response.Text);

        return new PipelineResult(
            Query: candidate.Query,
            FinalAnswer: verdict.FinalAnswer,
            GroundednessScore: groundedness.Score,
            RelevanceScore: relevance.Score,
            Accepted: verdict.Accepted,
            SynthesizerNotes: verdict.Notes);
    }

    private static SynthesizerVerdictDto ParseVerdict(string raw)
    {
        string cleaned = raw.Trim().Trim('`').Replace("json", "", StringComparison.OrdinalIgnoreCase).Trim();

        try
        {
            return JsonSerializer.Deserialize<SynthesizerVerdictDto>(cleaned)
                ?? throw new InvalidOperationException("Synthesizer returned null verdict.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Could not parse synthesizer response as JSON: '{raw}'", ex);
        }
    }
}