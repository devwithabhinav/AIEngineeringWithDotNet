using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phase3_RAG
{
    public class ChunkItems
    {
        public static List<TextChunk> List()
        {

            // --- Dummy health report chunks, hardcoded — no file I/O, minimal token cost ---
            var dummyReports = new List<TextChunk>
            {
                new()
                {
                    SourceId = "report-2025-01",
                    SourceName = "January 2025 Report",
                    Text = "Checkup Report - January 2025. Cholesterol: 200 mg/dL. Blood Pressure: 124/82. " +
                           "Blood Sugar (Fasting): 95 mg/dL. Weight: 78 kg."
                },
                new()
                {
                    SourceId = "report-2025-05",
                    SourceName = "May 2025 Report",
                    Text = "Checkup Report - May 2025. Cholesterol: 190 mg/dL. Blood Pressure: 120/80. " +
                           "Blood Sugar (Fasting): 92 mg/dL. Weight: 76 kg."
                },
                new()
                {
                    SourceId = "report-2025-09",
                    SourceName = "September 2025 Report",
                    Text = "Checkup Report - September 2025. Cholesterol: 185 mg/dL. Blood Pressure: 118/78. " +
                           "Blood Sugar (Fasting): 90 mg/dL. Weight: 75 kg."
                },
                new()
                {
                    SourceId = "report-2026-01",
                    SourceName = "January 2026 Report",
                    Text = "Checkup Report - January 2026. Cholesterol: 178 mg/dL. Blood Pressure: 116/76. " +
                           "Blood Sugar (Fasting): 88 mg/dL. Weight: 74 kg."
                }
            };

            return dummyReports;
        }
    }
}
