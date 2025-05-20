export type ChatMessage = {
  id: string;
  content: string;
  role: string;
  timestamp: string;
}

export const HubEventNames = {
  ResponseStreamStart: "ResponseStreamStart",
  ResponseStreamChunk: "ResponseStreamChunk",
  ResponseStreamEnd: "ResponseStreamEnd",
  SessionSubjectUpdated: "SessionSubjectUpdated",
  HistoryStreamStart: "HistoryStreamStart",
  HistoryStreamChunk: "HistoryStreamChunk",
  HistoryStreamEnd: "HistoryStreamEnd",
} as const;