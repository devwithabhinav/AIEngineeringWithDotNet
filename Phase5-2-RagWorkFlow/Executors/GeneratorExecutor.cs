using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Phase5_2_RagSequentialWorkFlow.Models;

namespace Phase5_2_RagSequentialWorkFlow.Executors;

/// <summary>
/// Wraps the Generator agent. Custom executors that call an agent are the
/// standard way to bundle extra state (here: query + context text) into the
/// message that downstream fan-out targets need, since the raw agent alone
/// only knows how to turn a string prompt into a string reply.
/// </summary>
internal sealed class GeneratorExecutor(ChatClientAgent generatorAgent) : Executor<RetrievalContext, CandidateAnswer>("Generator")
{
    public override async ValueTask<CandidateAnswer> HandleAsync(
        RetrievalContext retrieval, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        string prompt = $"""
            QUERY: {retrieval.Query}

            CONTEXT:
            {retrieval.ContextText}
            """;

        var response = await generatorAgent.RunAsync(prompt, cancellationToken: cancellationToken);

        return new CandidateAnswer(
            RunId: Guid.NewGuid().ToString("N"),
            Query: retrieval.Query,
            ContextText: retrieval.ContextText,
            Answer: response.Text);
    }
}