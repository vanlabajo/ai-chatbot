import { RefObject, useEffect, useRef } from "react";

export function UseScrollToBottom<T extends HTMLElement>(): [RefObject<T | null>, RefObject<T | null>] {
  const containerRef = useRef<T | null>(null);
  const endRef = useRef<T | null>(null);

  useEffect(() => {
    const container = containerRef.current;
    const end = endRef.current;

    if (!container || !end) return;

    const scrollToBottom = () => {
      end.scrollIntoView({ behavior: "smooth", block: "end" });
    };

    const observer = new MutationObserver(scrollToBottom);

    observer.observe(container, {
      childList: true,
      subtree: true,
    });

    // Scroll to the bottom initially
    scrollToBottom();

    return () => observer.disconnect();
  }, []);

  return [containerRef, endRef];
}