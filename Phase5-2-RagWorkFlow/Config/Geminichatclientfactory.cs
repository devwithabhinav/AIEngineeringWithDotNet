using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;

namespace Phase5_2_RagSequentialWorkFlow.Config;

/// <summary>
/// Builds an IChatClient backed by Gemini, using Google's OpenAI-compatible
/// endpoint (https://ai.google.dev/gemini-api/docs/openai) so we can lean on
/// the official OpenAI .NET SDK + Microsoft.Extensions.AI.OpenAI's
/// AsIChatClient() instead of a third-party Gemini package.
///
/// Requires env var GEMINI_API_KEY. Optionally GEMINI_MODEL (defaults below).
/// </summary>
public static class GeminiChatClientfactory
{
    private const string GeminiOpenAiBaseUrl =
        "https://generativelanguage.googleapis.com/v1beta/openai/";

    public static IChatClient Create(string? modelId = null)
    {
        string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? throw new InvalidOperationException(
                "GEMINI_API_KEY is not set. Get a key from Google AI Studio " +
                "and set it as an environment variable before running.");

        string model = modelId
            ?? Environment.GetEnvironmentVariable("GEMINI_MODEL_ID")
            ?? "gemini-2.5-flash";

        var openAiOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(GeminiOpenAiBaseUrl)
        };

        ChatClient rawClient = new(
            model: model,
            credential: new ApiKeyCredential(apiKey),
            options: openAiOptions);

        // AsIChatClient() bridges the OpenAI SDK's ChatClient into
        // Microsoft.Extensions.AI's IChatClient abstraction, which is what
        // Microsoft.Agents.AI expects.
        return rawClient.AsIChatClient();
    }
}