namespace Phase6A_FanOutFanIn_InMemory.Models;

/// <summary>
/// Output of the retrieval step. Carries the original query plus whatever
/// context chunks were pulled from your vector store. Swap RetrieverExecutor's
/// internals for your real Phase 3 retrieval call and this shape probably
/// still holds.
/// </summary>
public sealed record RetrievalContext(
    string Query,
    IReadOnlyList<RetrievedChunk> Chunks)
{
    public string ContextText => string.Join(
        "\n---\n",
        Chunks.Select(c => $"[Source: {c.SourceId}]\n{c.Text}"));
}

public sealed record RetrievedChunk(string SourceId, string Text, double SimilarityScore);

/// <summary>
/// Output of the generation step: a candidate answer plus everything the
/// downstream evaluators need to judge it, without re-fetching anything.
/// This is the message type that gets fanned out to both evaluators.
/// </summary>
public sealed record CandidateAnswer(
    string RunId,
    string Query,
    string ContextText,
    string Answer);

/// <summary>
/// Output of each evaluator (Groundedness, Relevance). Mirrors the scoring
/// shape from your Phase 3 eval suite: 1-5 scale + rationale, plus a pass
/// threshold so the synthesizer can make an accept/flag decision.
/// </summary>
public sealed record EvaluationResult(
    string RunId,
    string Metric,      // "Groundedness" | "Relevance"
    int Score,           // 1-5
    string Rationale,
    CandidateAnswer Candidate)
{
    public bool Passed => Score >= 4;
}

/// <summary>
/// Final workflow output.
/// </summary>
public sealed record PipelineResult(
    string Query,
    string FinalAnswer,
    int GroundednessScore,
    int RelevanceScore,
    bool Accepted,
    string SynthesizerNotes);