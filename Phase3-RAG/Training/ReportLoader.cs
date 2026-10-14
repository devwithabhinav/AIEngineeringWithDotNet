using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace Phase3_RagWithEvaluation.Training
{
    public class ReportLoader
    {
        public IEnumerable<TextChunk> LoadFromPdf(string fileName, string reportDate)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));

            string pdfPath = Path.Combine(projectRoot, "Training", "MyHealthreport", fileName);

            using var document = PdfDocument.Open(pdfPath);
            //var filename = Path.GetFileNameWithoutExtension(filePath);

            int pageNumber = 0;
            foreach (var page in document.GetPages())
            {
                pageNumber++;
                var text = page.Text;

                if (string.IsNullOrWhiteSpace(text))
                    continue; // skip blank/scanned-image pages that extracted no text

                yield return new TextChunk
                {
                    SourceId = $"{fileName}-p{pageNumber}",
                    SourceName = $"{reportDate} Report - Page {pageNumber}",
                    ReportDate = reportDate,
                    Text = text
                };
            }
        }

        public List<TextChunk> LoadPdf()
        {
            var allChunks = new List<TextChunk>();
            allChunks.AddRange(LoadFromPdf("April2026.pdf", "April 2026"));
            allChunks.AddRange(LoadFromPdf("jan2025.pdf", "jan 2025"));
            allChunks.AddRange(LoadFromPdf("June2025.pdf", "June 2025"));
            allChunks.AddRange(LoadFromPdf("may2025.pdf", "may 2025"));

            return allChunks;
        }

        // New: load markdown file and split into sections by top-level headings (#, ##, etc.).
        public IEnumerable<TextChunk> LoadFromMarkdown(string fileName, string reportDate)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));
            string mdPath = Path.Combine(projectRoot, "Training", "MyHealthreport", fileName);

            if (!File.Exists(mdPath))
                yield break;

            string MakeSlug(string s)
            {
                if (string.IsNullOrWhiteSpace(s))
                    return "section";
                var lowered = s.ToLowerInvariant();
                var cleaned = Regex.Replace(lowered, "[^\\w\\- ]+", string.Empty);
                cleaned = cleaned.Replace(' ', '-');
                return cleaned;
            }

            var sb = new StringBuilder();
            string currentHeading = null;
            int sectionNumber = 0;
            bool foundAnyHeading = false;

            foreach (var raw in File.ReadLines(mdPath))
            {
                var line = raw ?? string.Empty;
                var trimmedStart = line.TrimStart();

                if (trimmedStart.StartsWith("#"))
                {
                    // yield previous section if any
                    if (sb.Length > 0 || currentHeading != null)
                    {
                        sectionNumber++;
                        var text = sb.ToString().Trim();
                        var idPart = currentHeading != null ? MakeSlug(currentHeading) : $"s{sectionNumber}";
                        yield return new TextChunk
                        {
                            SourceId = $"{fileName}-s{sectionNumber}-{idPart}",
                            SourceName = currentHeading != null ? $"{reportDate} Report - {currentHeading}" : $"{reportDate} Report - Section {sectionNumber}",
                            ReportDate = reportDate,
                            Text = text
                        };
                    }

                    currentHeading = trimmedStart.TrimStart('#').Trim();
                    sb.Clear();
                    foundAnyHeading = true;
                }
                else
                {
                    sb.AppendLine(line);
                }
            }

            // final section or whole file
            if (sb.Length > 0 || !foundAnyHeading)
            {
                sectionNumber++;
                var text = sb.ToString().Trim();
                var idPart = foundAnyHeading ? (currentHeading != null ? MakeSlug(currentHeading) : $"s{sectionNumber}") : "full";
                var sourceName = foundAnyHeading ? (currentHeading != null ? $"{reportDate} Report - {currentHeading}" : $"{reportDate} Report - Section {sectionNumber}") : $"{reportDate} Report - Full";

                yield return new TextChunk
                {
                    SourceId = $"{fileName}-s{sectionNumber}-{idPart}",
                    SourceName = sourceName,
                    ReportDate = reportDate,
                    Text = text
                };
            }
        }

        // New: Load multiple markdown files (parity with LoadPdf)
        public List<TextChunk> LoadMarkdown()
        {
            var allChunks = new List<TextChunk>();
            allChunks.AddRange(LoadFromMarkdown("April2026.md", "April 2026"));
            allChunks.AddRange(LoadFromMarkdown("jan2025.md", "jan 2025"));
            allChunks.AddRange(LoadFromMarkdown("June2025.md", "June 2025"));
            allChunks.AddRange(LoadFromMarkdown("may2025.md", "may 2025"));

            return allChunks;
        }
    }
}
