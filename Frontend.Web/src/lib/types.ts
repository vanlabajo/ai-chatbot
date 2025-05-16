export interface ChatSession {
  sessionId: string;
  subject?: string | null;
  timestamp: string;
  messages: ChatMessage[];
}

export interface ChatMessage {
  messageId: string;
  sender: string;
  content: string;
  timestamp: string;
}