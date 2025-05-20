import { RefObject, useCallback, useRef } from "react";

export function UseChatScroll<T extends HTMLElement>(): [
  RefObject<T | null>,
  RefObject<T | null>,
  () => void,
  (prevHeight: number) => void
] {
  const containerRef = useRef<T | null>(null);
  const endRef = useRef<T | null>(null);

  const scrollToBottom = useCallback(() => {
    const end = endRef.current;
    if (end) {
      end.scrollIntoView({ behavior: "smooth", block: "end" });
    }
  }, []);

  const preserveScrollOnPrepend = useCallback((prevHeight: number) => {
    const container = containerRef.current;
    if (container) {
      const newHeight = container.scrollHeight;
      container.scrollTop += newHeight - prevHeight;
    }
  }, []);

  return [containerRef, endRef, scrollToBottom, preserveScrollOnPrepend];
}