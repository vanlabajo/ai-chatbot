export type ChatMessage = {
  id: string;
  content: string;
  role: string;
  timestamp: string;
  shouldTypewrite?: boolean; // optional
}

export const HubEventNames = {
  ResponseStreamStart: "ResponseStreamStart",
  ResponseStreamChunk: "ResponseStreamChunk",
  ResponseStreamEnd: "ResponseStreamEnd",
  SessionSubjectUpdated: "SessionSubjectUpdated",
  HistoryStreamStart: "HistoryStreamStart",
  HistoryStreamChunk: "HistoryStreamChunk",
  HistoryStreamEnd: "HistoryStreamEnd",
  SessionUpdate: "SessionUpdate",
  SessionDelete: "SessionDelete",
} as const;

export interface ChatSession {
  id: string;
  userId: string;
  title?: string | null;
  timestamp: string;
  messages: ChatMessage[];
}