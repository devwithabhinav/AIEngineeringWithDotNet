using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Embeddings;

namespace Phase6A_FanOutFanIn_VectorStore.Config;

/// <summary>
/// Builds an IEmbeddingGenerator&lt;string, Embedding&lt;float&gt;&gt; backed by
/// Gemini's embedding model, via the same OpenAI-compatible endpoint used
/// for chat (https://ai.google.dev/gemini-api/docs/openai). Google's
/// compatibility layer supports both /chat/completions and /embeddings, so
/// this is the same bridging trick as GeminiChatClientFactory, just with
/// EmbeddingClient instead of ChatClient.
///
/// Requires env var GEMINI_API_KEY (same key as chat). Optionally
/// GEMINI_EMBEDDING_MODEL (defaults to gemini-embedding-001).
/// </summary>
public static class GeminiEmbeddingGeneratorFactory
{
    private const string GeminiOpenAiBaseUrl =
        "https://generativelanguage.googleapis.com/v1beta/openai/";

    public static IEmbeddingGenerator<string, Embedding<float>> Create(string? modelId = null)
    {
        string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? throw new InvalidOperationException(
                "GEMINI_API_KEY is not set. Get a key from Google AI Studio " +
                "and set it as an environment variable before running.");

        string model = modelId
            ?? Environment.GetEnvironmentVariable("GEMINI_EMBEDDING_MODEL")
            ?? "gemini-embedding-001";

        var openAiOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(GeminiOpenAiBaseUrl)
        };

        EmbeddingClient rawClient = new(
            model: model,
            credential: new ApiKeyCredential(apiKey),
            options: openAiOptions);

        // AsIEmbeddingGenerator() bridges the OpenAI SDK's EmbeddingClient
        // into Microsoft.Extensions.AI's IEmbeddingGenerator abstraction,
        // which is what the InMemoryVectorStore expects for automatic
        // text-to-vector conversion on upsert and search.
        return rawClient.AsIEmbeddingGenerator();
    }
}