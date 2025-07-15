namespace Backend.Api.Hubs
{
    public static class HubEventNames
    {
        public const string ResponseStreamStart = "ResponseStreamStart";
        public const string ResponseStreamChunk = "ResponseStreamChunk";
        public const string ResponseStreamEnd = "ResponseStreamEnd";
        public const string HistoryStreamStart = "HistoryStreamStart";
        public const string HistoryStreamChunk = "HistoryStreamChunk";
        public const string HistoryStreamEnd = "HistoryStreamEnd";
        public const string SessionUpdate = "SessionUpdate";
        public const string SessionDelete = "SessionDelete";
    }

}
