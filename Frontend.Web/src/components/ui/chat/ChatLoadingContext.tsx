"use client";

import { createContext, useContext, useState } from "react";

const ChatLoadingContext = createContext<{ isLoading: boolean, setIsLoading: (v: boolean) => void } | undefined>(undefined);

export function ChatLoadingProvider({ children }: { children: React.ReactNode }) {
  const [isLoading, setIsLoading] = useState(false);
  return (
    <ChatLoadingContext.Provider value={{ isLoading, setIsLoading }}>
      {children}
    </ChatLoadingContext.Provider>
  );
}

export function useChatLoading() {
  const ctx = useContext(ChatLoadingContext);
  if (!ctx) throw new Error("useChatLoading must be used within ChatLoadingProvider");
  return ctx;
}