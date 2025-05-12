"use client";
import { ChatHeader } from "@/components/ui/chat/ChatHeader";
import { ChatInput } from "@/components/ui/chat/ChatInput";
import { PreviewChatMessage, ThinkingChatMessage } from "@/components/ui/chat/ChatMessage";
import { ChatOverview } from "@/components/ui/chat/ChatOverview";
import { UseScrollToBottom } from "@/components/ui/chat/UseScrollToBottom";
import { ChatMessage } from "@/lib/definitions";
import { getToken } from "@/lib/msal";
import { useCallback, useEffect, useRef, useState } from "react";
import { v4 as uuidv4 } from "uuid";

export default function Chat() {
  const [messagesContainerRef, messagesEndRef] = UseScrollToBottom<HTMLDivElement>();
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [question, setQuestion] = useState<string>("");
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [webSocket, setWebSocket] = useState<WebSocket | null>(null);

  const messageHandlerRef = useRef<((event: MessageEvent) => void) | null>(null);
  const isInitializedRef = useRef(false);

  const cleanupMessageHandler = () => {
    if (messageHandlerRef.current && webSocket) {
      if (webSocket.readyState === WebSocket.OPEN) {
        webSocket.removeEventListener("message", messageHandlerRef.current);
      }
      messageHandlerRef.current = null;
    }
  };

  const initializeEventSource = useCallback(async () => {
    if (isInitializedRef.current) {
      console.warn("WebSocket already initialized");
      return;
    }
    isInitializedRef.current = true; // Mark as initialized
    setIsLoading(true);

    const token = await getToken();
    if (!token) {
      console.error("Failed to fetch token");
      setIsLoading(false);
      return;
    }
    const webSocketEndpoint = process.env.NEXT_PUBLIC_WEBSOCKET_ENDPOINT;

    if (!webSocketEndpoint) {
      throw new Error("WebSocket endpoint is not defined");
    }
    const url = new URL(webSocketEndpoint);
    url.searchParams.append("access_token", encodeURIComponent(token));

    const newWebSocket = new WebSocket(url.toString());

    newWebSocket.onopen = () => setIsLoading(false);
    newWebSocket.onerror = (error) => {
      console.error("WebSocket error:", error);
      setIsLoading(false);
      isInitializedRef.current = false; // Allow reinitialization
    };
    newWebSocket.onclose = () => {
      console.warn("WebSocket closed. Attempting to reconnect...");
      isInitializedRef.current = false; // Allow reinitialization
      setTimeout(() => initializeEventSource(), 5000); // Retry after 5 seconds
    };

    setWebSocket(newWebSocket);
  }, [setIsLoading, setWebSocket]);

  useEffect(() => {
    initializeEventSource();

    return () => {
      if (webSocket) {
        webSocket.close();
        isInitializedRef.current = false;
      }
    };
  }, [initializeEventSource, webSocket]);

  async function handleSubmit(text?: string) {
    if (!webSocket || webSocket.readyState !== WebSocket.OPEN || isLoading) return;

    const messageText = text || question;
    setIsLoading(true);
    cleanupMessageHandler();

    const traceId = uuidv4();
    setMessages((prev) => [...prev, { content: messageText, role: "user", id: traceId }]);
    webSocket.send(messageText);
    setQuestion("");

    const END_TOKEN = "[END]";
    try {
      const messageHandler = (event: MessageEvent) => {
        setIsLoading(false);
        if (event.data.includes(END_TOKEN)) {
          cleanupMessageHandler();
          return;
        }

        setMessages((prev) => {
          const lastMessage = prev[prev.length - 1];
          const newContent =
            lastMessage?.role === "assistant" ? lastMessage.content + event.data : event.data;

          const newMessage = { content: newContent, role: "assistant", id: traceId };
          return lastMessage?.role === "assistant"
            ? [...prev.slice(0, -1), newMessage]
            : [...prev, newMessage];
        });
      };

      messageHandlerRef.current = messageHandler;
      webSocket.addEventListener("message", messageHandler);
    } catch (error) {
      console.error("WebSocket error:", error);
      setIsLoading(false);
    }
  }

  return (
    <section aria-label="Chat" className="flex flex-col h-[calc(96dvh-2rem)] bg-background">
      <ChatHeader />
      <div
        className="flex flex-col flex-1 min-w-0 gap-6 overflow-y-auto pt-4"
        ref={messagesContainerRef}
      >
        {messages.length == 0 && <ChatOverview />}
        {messages.map((message, index) => (
          <PreviewChatMessage key={index} message={message} />
        ))}
        {isLoading && <ThinkingChatMessage />}
        <div ref={messagesEndRef} className="shrink-0 min-w-[24px] min-h-[24px]" />
      </div>
      <div className="flex px-4 bg-background gap-2 w-full md:max-w-3xl mx-auto">
        <ChatInput
          question={question}
          setQuestion={setQuestion}
          onSubmit={handleSubmit}
          isLoading={isLoading}
        />
      </div>
    </section>
  );
}