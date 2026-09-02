using GeminiDotnet;
using GeminiDotnet.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Phase2_AgentApi;

EnvLoader.LoadDotEnv(); // Try load .env from project tree (if present) so secrets can be kept out of source.

// Read config from environment
var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
var modelId = Environment.GetEnvironmentVariable("GEMINI_MODEL_ID") ?? "gemini-3-flash";


var builder = WebApplication.CreateBuilder(args);

// --- Register IChatClient as a singleton ---
builder.Services.AddSingleton<IChatClient>(_ =>
{
    return new GeminiChatClient(new GeminiClientOptions
    {
        ApiKey = apiKey,
        ModelId = modelId
    });
});

// --- Register both agents as keyed singletons ---
builder.Services.AddKeyedSingleton<AIAgent>("conversational", (sp, _) =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    return chatClient.AsAIAgent(
        instructions: "You are a concise personal assistant. Use tools when relevant instead of guessing.",
        name: "PersonalAssistant",
        tools: [
            AIFunctionFactory.Create(Tools.GetCurrentTime),
            AIFunctionFactory.Create(Tools.Add),
            AIFunctionFactory.Create(Tools.LookUpFact)
        ]);
});

builder.Services.AddKeyedSingleton<AIAgent>("extractor", (sp, _) =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    return chatClient.AsAIAgent(
        instructions: "You extract structured reminder data from user text.",
        name: "ReminderExtractor");
});

builder.Services.AddSingleton<SessionStore>();

var app = builder.Build();

/// --- Custom middleware: logs method, path, and latency for every request ---
app.Use(async (context, next) =>
{
    var start = DateTime.UtcNow;
    await next();
    var elapsedMs = (DateTime.UtcNow - start).TotalMilliseconds;
    Console.WriteLine($"[{context.Request.Method}] {context.Request.Path} -> {context.Response.StatusCode} ({elapsedMs:F0}ms)");
});

// --- /chat endpoint ---
app.MapPost("/chat", async (ChatRequest request, [FromKeyedServices("conversational")] AIAgent agent,SessionStore store) =>
{
    var conversationId = request.ConversationId ?? Guid.NewGuid().ToString();

    var existingState = await store.LoadAsync(conversationId);
    AgentSession session = existingState is { } state
        ? await agent.DeserializeSessionAsync(state)
        : await agent.CreateSessionAsync();

    var response = await agent.RunAsync(request.Message, session);

    var serialized = await agent.SerializeSessionAsync(session);
    await store.SaveAsync(conversationId, serialized);

    return Results.Ok(new ChatResponse(conversationId, response.Text));
});

// --- /remind endpoint using the extractor agent ---
app.MapPost("/remind", async (RemindRequest request,[FromKeyedServices("extractor")] AIAgent extractorAgent) =>
{
    var response = await extractorAgent.RunAsync<ReminderInfo>(
        $"Extract a reminder from this: \"{request.Description}\". " +
        "If no due date is mentioned, use \"unspecified\". " +
        "Priority must be Low, Medium, or High.");

    return Results.Ok(response.Result);
});

app.Run();


record ChatRequest(string? ConversationId, string Message);
record ChatResponse(string ConversationId, string Reply);
record RemindRequest(string Description);
record ReminderInfo(string Title, string DueDate, string Priority);