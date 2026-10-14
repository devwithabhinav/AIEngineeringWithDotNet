namespace Phase6A_FanOutFanIn_VectorStore.VectorStore
{
    public static class SeedDocuments
    {
        public static readonly (string Id, string Text)[] All =
        [
            ("doc-1", "Microsoft Agent Framework workflows use a graph of executors " +
                  "connected by edges. Within a superstep, all triggered executors " +
                  "run in parallel, but the workflow does not advance to the next " +
                  "superstep until every executor in the current one completes."),
        ("doc-2", "Fan-out/fan-in in Agent Framework is done via AddFanOutEdge to " +
                  "send a message to multiple executors in parallel, and " +
                  "AddFanInBarrierEdge to wait for all of them before proceeding."),
        ("doc-3", "Agent Framework supports four built-in multi-agent orchestration " +
                  "patterns: sequential, concurrent, hand-off, and magentic."),
        ("doc-4", "Checkpointing lets a workflow save its state so long-running " +
                  "processes can be recovered and resumed, and workflows can run " +
                  "on a durable host without changing the executor code."),
    ];
    }
}
