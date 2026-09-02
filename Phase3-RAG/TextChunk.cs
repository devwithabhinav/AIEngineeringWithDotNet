namespace Phase3_RAG
{
    public class TextChunk
    {
        public string SourceId { get; set; } = "";
        public string SourceName { get; set; } = "";
        public string Text { get; set; } = "";
        public string Embedding { get; set; } = ""; // auto-embedded by the vector store
        public string ReportDate { get; set; } = "";  // e.g. "2026-03-15"
    }
}
