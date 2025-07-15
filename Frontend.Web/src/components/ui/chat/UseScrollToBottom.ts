import { RefObject, useEffect, useRef } from "react";

export function UseScrollToBottom<T extends HTMLElement>(): [
  RefObject<T | null>,
  RefObject<T | null>
] {
  const containerRef = useRef<T | null>(null);
  const endRef = useRef<T | null>(null);
  const observerRef = useRef<MutationObserver | null>(null);

  // Helper to check if user is near the bottom
  const isNearBottom = () => {
    const container = containerRef.current;
    if (!container) return true;
    return container.scrollHeight - container.scrollTop - container.clientHeight < 50;
  };

  useEffect(() => {
    const container = containerRef.current;
    const end = endRef.current;
    if (!container || !end) return;

    const observer = new MutationObserver(() => {
      end.scrollIntoView({ behavior: "auto", block: "end" });
    });
    observer.observe(container, {
      childList: true,
      subtree: true,
      attributes: true,
      characterData: true,
    });
    observerRef.current = observer;

    // Listen for user scroll
    const handleScroll = () => {
      if (!isNearBottom()) {
        observer.disconnect();
      } else {
        // Reconnect if user scrolls back to bottom
        observer.observe(container, {
          childList: true,
          subtree: true,
          attributes: true,
          characterData: true,
        });
      }
    };
    container.addEventListener("scroll", handleScroll);

    return () => {
      observer.disconnect();
      container.removeEventListener("scroll", handleScroll);
    };
  }, []);

  return [containerRef, endRef];
}