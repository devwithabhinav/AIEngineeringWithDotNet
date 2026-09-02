namespace Phase3_RAG
{
    public class EnvLoader
    {
        public static void LoadDotEnv(string fileName = ".env", int maxDepth = 5)
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var dir = new DirectoryInfo(baseDir);
                for (int i = 0; i <= maxDepth && dir != null; i++)
                {
                    var candidate = Path.Combine(dir.FullName, fileName);
                    if (File.Exists(candidate))
                    {
                        foreach (var line in File.ReadAllLines(candidate))
                        {
                            var trimmed = line.Trim();
                            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                                continue;

                            var idx = trimmed.IndexOf('=');
                            if (idx <= 0) continue;

                            var key = trimmed.Substring(0, idx).Trim();
                            var val = trimmed.Substring(idx + 1).Trim().Trim('"');

                            // Only set if not already set in environment (allow real env to override)
                            if (Environment.GetEnvironmentVariable(key) == null)
                            {
                                Environment.SetEnvironmentVariable(key, val);
                            }
                        }
                        // Stop after first found file
                        return;
                    }
                    dir = dir.Parent;
                }
            }
            catch
            {
                // ignore any issues reading the .env to avoid crashing on startup
            }
        }
    }
}
