"use client";
import { ChatHeader } from "@/components/ui/chat/ChatHeader";
import { ChatInput } from "@/components/ui/chat/ChatInput";
import { useChatLoading } from "@/components/ui/chat/ChatLoadingContext";
import { PreviewChatMessage, ThinkingChatMessage } from "@/components/ui/chat/ChatMessage";
import { ChatOverview } from "@/components/ui/chat/ChatOverview";
import { UseScrollToBottom } from "@/components/ui/chat/UseScrollToBottom";
import { ChatMessage, HubEventNames } from "@/lib/definitions";
import { getConnection } from "@/lib/signalr";
import { HubConnection, HubConnectionState } from "@microsoft/signalr";
import { useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { v4 as uuidv4 } from "uuid";

export default function Chat() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const sessionId = searchParams.get("sessionId");

  const [messagesContainerRef, messagesEndRef] = UseScrollToBottom<HTMLDivElement>();
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [question, setQuestion] = useState<string>("");
  const { isLoading, setIsLoading } = useChatLoading();
  const [hubConnection, setHubConnection] = useState<HubConnection | null>(null);
  const [hasMore, setHasMore] = useState(true);
  const [offset, setOffset] = useState(0);
  const limit = 20;

  const sessionIdRef = useRef<string | null>(sessionId);
  const messageIds = useRef<Set<string>>(new Set());
  const prevHeightRef = useRef<number | null>(null);
  const messageHandlerRef = useRef<((chunk: string) => void) | null>(null);
  const messageStreamEndHandlerRef = useRef<(() => void) | null>(null);
  const historyHandlerRef = useRef<((history: ChatMessage) => void) | null>(null);
  const historyStreamEndHandlerRef = useRef<(() => void) | null>(null);
  const isInitializedRef = useRef(false);
  const batchCountRef = useRef(0);

  // -- Cleanup Handlers --

  const cleanupMessageHandler = useCallback(() => {
    if (messageHandlerRef.current && hubConnection) {
      if (hubConnection.state === HubConnectionState.Connected) {
        hubConnection.off(HubEventNames.ResponseStreamChunk, messageHandlerRef.current);
      }
      messageHandlerRef.current = null;
    }
  }, [hubConnection]);

  const cleanupHistoryHandler = useCallback(() => {
    if (historyHandlerRef.current && hubConnection) {
      if (hubConnection.state === HubConnectionState.Connected) {
        hubConnection.off(HubEventNames.HistoryStreamChunk, historyHandlerRef.current);
      }
      historyHandlerRef.current = null;
    }
  }, [hubConnection]);

  const cleanupMessageStreamEndHandler = useCallback(() => {
    if (messageStreamEndHandlerRef.current && hubConnection) {
      if (hubConnection.state === HubConnectionState.Connected) {
        hubConnection.off(HubEventNames.ResponseStreamEnd, messageStreamEndHandlerRef.current);
      }
      messageStreamEndHandlerRef.current = null;
    }
  }, [hubConnection]);

  const cleanupHistoryStreamEndHandler = useCallback(() => {
    if (historyStreamEndHandlerRef.current && hubConnection) {
      if (hubConnection.state === HubConnectionState.Connected) {
        hubConnection.off(HubEventNames.HistoryStreamEnd, historyStreamEndHandlerRef.current);
      }
      historyStreamEndHandlerRef.current = null;
    }
  }, [hubConnection]);

  // -- Handlers --

  const messageHandler = useCallback((chunk: string) => {
    setIsLoading(false);
    setMessages((prev) => {
      const lastMessage = prev[prev.length - 1];
      if (lastMessage?.role === "assistant") {
        // Append chunk to the last assistant message
        const updatedMessage = {
          ...lastMessage,
          content: lastMessage.content + chunk,
          shouldTypewrite: true, // Indicate this message should use typewriter effect
        };
        return [...prev.slice(0, -1), updatedMessage];
      } else {
        // Start a new assistant message
        const newMessage = {
          content: chunk,
          role: "assistant",
          id: uuidv4(),
          timestamp: new Date().toISOString(),
          shouldTypewrite: true, // Indicate this message should use typewriter effect
        };
        return [...prev, newMessage];
      }
    });
  }, []);

  const historyHandler = useCallback((history: ChatMessage) => {
    batchCountRef.current += 1;
    if (!messageIds.current.has(history.id)) {
      messageIds.current.add(history.id);
      // Only set prevHeightRef for the first message in a batch
      if (batchCountRef.current === 1) {
        prevHeightRef.current = messagesContainerRef.current?.scrollHeight ?? 0;
      }
      setMessages(prev => [history, ...prev]);
      setOffset(prev => prev + 1);
    }
  }, [messagesContainerRef]);

  const messageStreamEndHandler = useCallback(() => {
    cleanupMessageHandler();
  }, [cleanupMessageHandler]);

  const historyStreamEndHandler = useCallback(() => {
    cleanupHistoryHandler();
    setHasMore(batchCountRef.current === limit); // true if batch was full, false if not
    batchCountRef.current = 0;
  }, [cleanupHistoryHandler]);

  // -- Connection Initialization --

  const initializeConnection = useCallback(async () => {
    if (isInitializedRef.current) {
      console.warn("HubConnection already initialized");
      return;
    }
    isInitializedRef.current = true;
    setIsLoading(true);

    try {
      const connection = await getConnection();
      if (connection.state !== HubConnectionState.Connected) {
        await connection.start();
      }

      cleanupMessageHandler();
      cleanupHistoryHandler();
      cleanupMessageStreamEndHandler();
      cleanupHistoryStreamEndHandler();

      // Assign handlers to refs for cleanup
      messageHandlerRef.current = messageHandler;
      historyHandlerRef.current = historyHandler;
      messageStreamEndHandlerRef.current = messageStreamEndHandler;
      historyStreamEndHandlerRef.current = historyStreamEndHandler;

      connection.on(HubEventNames.ResponseStreamChunk, messageHandlerRef.current);
      connection.on(HubEventNames.HistoryStreamChunk, historyHandlerRef.current);
      connection.on(HubEventNames.ResponseStreamEnd, messageStreamEndHandlerRef.current);
      connection.on(HubEventNames.HistoryStreamEnd, historyStreamEndHandlerRef.current);

      setHubConnection(connection);
    } catch (err) {
      console.error("SignalR connection error:", err);
      isInitializedRef.current = false;
    }

    setIsLoading(false);
  }, [cleanupHistoryHandler, cleanupHistoryStreamEndHandler, cleanupMessageHandler, cleanupMessageStreamEndHandler, historyHandler, historyStreamEndHandler, messageHandler, messageStreamEndHandler]);

  useEffect(() => {
    initializeConnection();

    return () => {
      if (hubConnection) {
        hubConnection.stop();
        isInitializedRef.current = false;
      }
    };
  }, [hubConnection, initializeConnection]);

  // --- Reset state on session change ---
  useEffect(() => {
    const currentSessionIdRef = sessionIdRef.current;
    sessionIdRef.current = sessionId;
    if (currentSessionIdRef !== sessionId) {
      setMessages([]);
      messageIds.current.clear();
      setOffset(0);
    }
  }, [sessionId]);

  // --- Load history if there is a sessionId, connection, and no messages loaded yet ---
  useEffect(() => {
    if (
      sessionId &&
      hubConnection &&
      hubConnection.state === HubConnectionState.Connected &&
      messages.length === 0
    ) {
      cleanupHistoryHandler();
      historyHandlerRef.current = historyHandler;
      hubConnection.on(HubEventNames.HistoryStreamChunk, historyHandlerRef.current);
      hubConnection.invoke("GetHistory", sessionId, offset, limit).catch(err => {
        console.error("Failed to get history: ", err);
      });
    }
  }, [sessionId, hubConnection, messages.length, cleanupHistoryHandler, historyHandler, offset]);

  // --- Infinite scroll: load more messages on scroll up ---
  useEffect(() => {
    const el = messagesContainerRef.current;
    if (!el) return;
    function handleScroll() {
      if (el!.scrollTop < 100 && hasMore && !isLoading) {
        if (
          sessionId &&
          hubConnection &&
          hubConnection.state === HubConnectionState.Connected
        ) {
          hubConnection.invoke("GetHistory", sessionId, offset, limit).catch(err => {
            console.error("Failed to get history: ", err);
          });
        }
      }
    }
    el.addEventListener("scroll", handleScroll);
    return () => el.removeEventListener("scroll", handleScroll);
  }, [messagesContainerRef, hasMore, isLoading, sessionId, hubConnection, offset, limit]);

  // --- Handle user submitting a message ---
  const handleSubmit = async (text?: string) => {
    if (!hubConnection || hubConnection.state !== HubConnectionState.Connected || isLoading) return;

    if (!sessionIdRef.current) {
      sessionIdRef.current = uuidv4();
      router.replace(`/chat?sessionId=${sessionIdRef.current}`);
    }

    const messageText = text || question;
    setIsLoading(true);
    cleanupMessageHandler();

    const optimisticMessage: ChatMessage = {
      id: uuidv4(),
      content: messageText,
      role: "user",
      timestamp: new Date().toISOString(),
    };
    setMessages((prev) => [...prev, optimisticMessage]);
    setQuestion("");

    messageHandlerRef.current = messageHandler;
    hubConnection.on(HubEventNames.ResponseStreamChunk, messageHandler);

    try {
      await hubConnection.invoke("SendMessage", messageText, sessionIdRef.current!);
    } catch (error) {
      setIsLoading(false);
      console.error("SignalR send error:", error);
    }
  };

  const filteredMessages = useMemo(
    () => messages.filter(
      (message) => message.role === "user" || message.role === "assistant"
    ),
    [messages]
  );

  return (
    <section aria-label="Chat" className="flex flex-col h-[calc(96dvh-2rem)] bg-background">
      <ChatHeader />
      <div
        className="flex flex-col flex-1 min-w-0 gap-6 overflow-y-auto pt-4"
        ref={messagesContainerRef}
      >
        {filteredMessages.length === 0 && <ChatOverview />}
        {filteredMessages.map((message) => {
          return (
            <PreviewChatMessage
              key={message.id}
              message={message}
              activateTypewritingEffect={!!message.shouldTypewrite}
            />
          );
        })}
        {isLoading && <ThinkingChatMessage />}
        <div ref={messagesEndRef} className="shrink-0 min-w-[24px] min-h-[24px]" />
      </div>
      <div className="flex px-4 mx-auto bg-background gap-2 w-full md:max-w-3xl md:pb-4 lg:pb-2">
        <ChatInput
          question={question}
          setQuestion={setQuestion}
          onSubmit={handleSubmit}
          isLoading={isLoading}
          hideSuggestions={filteredMessages.length > 0}
        />
      </div>
    </section>
  );
}