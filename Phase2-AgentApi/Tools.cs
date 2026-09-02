using System.ComponentModel;

namespace Phase2_AgentApi
{
    public static class Tools
    {
        [Description("Gets the current date and time")]
        public static string GetCurrentTime() => DateTime.Now.ToString("F");

        [Description("Adds two numbers together")]
        public static double Add(
            [Description("First number")] double a,
            [Description("Second number")] double b) => a + b;

        [Description("Looks up a fact from a small local dictionary")]
        public static string LookUpFact([Description("The topic to look up")] string topic)
        {
            var facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["dotnet"] = ".NET is a free, open-source developer platform maintained by Microsoft.",
                ["maf"] = "MAF (Microsoft Agent Framework) unifies Semantic Kernel and AutoGen into one agent SDK."
            };
            return facts.TryGetValue(topic, out var fact) ? fact : "No fact found for that topic.";
        }
    }
}
