using System.ComponentModel; // Provides attributes such as DescriptionAttribute used to describe tools for the agent

namespace Phase0_1_ChatClientAndAgents.Phase1
{
    // AgentTools is a simple container of helper methods ("tools") the AI agent
    // can call. Each public method represents a callable tool and is annotated
    // with Description attributes so the agent and humans know what they do.
    public class AgentTools
    {
        // --- Tools: plain C# methods. [Description] tells the model what they do ---

        // Tool: GetCurrentTime
        // Purpose: Return the current date and time as a human-readable string.
        // When the agent needs to report the current time it can call this tool
        // rather than inventing a value.
        [Description("Gets the current date and time")]
        public string GetCurrentTime() => DateTime.Now.ToString("F");

        // Tool: Add
        // Purpose: Perform a simple arithmetic addition of two numbers. Parameter
        // descriptions help the agent provide the correct inputs when invoking.
        [Description("Adds two numbers together")]
        public double Add(
            [Description("First number")] double a,
            [Description("Second number")] double b) => a + b;

        // Tool: LookUpFact
        // Purpose: Perform a look up in a small in-memory dictionary of facts.
        // Demonstrates how the agent can query a local knowledge source.
        [Description("Looks up a fact from a small local dictionary")]
        public string LookUpFact([Description("The topic to look up")] string topic)
        {
            // Small example dictionary mapping topic keys to human-readable facts.
            // Using OrdinalIgnoreCase makes the lookup case-insensitive which is
            // helpful when the agent provides topics with unknown capitalization.
            var facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["dotnet"] = ".NET is a free, open-source developer platform maintained by Microsoft.",
                ["maf"] = "MAF (Microsoft Agent Framework) unifies Semantic Kernel and AutoGen into one agent SDK."
            };

            // Return the matching fact if found, otherwise return a friendly default
            // message indicating no fact was available for the requested topic.
            return facts.TryGetValue(topic, out var fact) ? fact : "No fact found for that topic.";
        }
    }
}
