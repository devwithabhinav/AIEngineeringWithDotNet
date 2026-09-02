using Microsoft.Extensions.VectorData;

namespace Phase5_3_RagWorkFlowWithVector.VectorStore
{
    public sealed class RetrievedDocument
    {
        [VectorStoreKey]
        public required string Id { get; set; }

        [VectorStoreData]
        public required string SourceId { get; set; }

        [VectorStoreVector(Dimensions: 3072, DistanceFunction = DistanceFunction.CosineSimilarity)]
        public required string Text { get; set; }
    }
}
