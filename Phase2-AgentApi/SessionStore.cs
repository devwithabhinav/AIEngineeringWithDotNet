using System.Text.Json;

namespace Phase2_AgentApi
{
    public class SessionStore
    {
        private readonly string _folder;

        public SessionStore(IWebHostEnvironment env)
        {
            _folder = Path.Combine(env.ContentRootPath, "sessions");
            Directory.CreateDirectory(_folder);
        }

        private string PathFor(string conversationId) => Path.Combine(_folder, $"{conversationId}.json");

        public async Task SaveAsync(string conversationId, JsonElement sessionState)
        {
            await File.WriteAllTextAsync(PathFor(conversationId), sessionState.GetRawText());
        }

        public async Task<JsonElement?> LoadAsync(string conversationId)
        {
            var path = PathFor(conversationId);
            if (!File.Exists(path)) return null;

            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
    }
}
