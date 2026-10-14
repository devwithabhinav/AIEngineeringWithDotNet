using Microsoft.Extensions.VectorData;

namespace Phase6A_FanOutFanIn_VectorStore.VectorStore
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
