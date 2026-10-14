using System.Text.Json;
using System.Text.Json.Serialization;

namespace Phase6A_FanOutFanIn_InMemory.Executors;

/// <summary>
/// Shape of the JSON the judge agents are prompted to emit (see
/// AgentFactory's instructions for GroundednessJudge / RelevanceJudge).
/// </summary>
internal sealed record JudgeScoreDto(
    [property: JsonPropertyName("score")] int Score,
    [property: JsonPropertyName("rationale")] string Rationale);

/// <summary>
/// Small shared helper so the two evaluator executors don't each hand-roll
/// JSON parsing.
/// </summary>
internal static class JudgeScoreParser
{
    public static JudgeScoreDto Parse(string raw)
    {
        // Judges are prompted to return bare JSON, but strip fences
        // defensively in case the model wraps it anyway.
        string cleaned = raw.Trim().Trim('`').Replace("json", "", StringComparison.OrdinalIgnoreCase).Trim();

        try
        {
            return JsonSerializer.Deserialize<JudgeScoreDto>(cleaned)
                ?? throw new InvalidOperationException("Judge returned null score.");
        }
        catch (JsonException ex)
        {
            // Fail loud rather than silently defaulting a quality score -
            // a mis-parsed judge is worse than a visible pipeline error.
            throw new InvalidOperationException(
                $"Could not parse judge response as JSON: '{raw}'", ex);
        }
    }
}