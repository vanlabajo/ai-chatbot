import { motion } from "framer-motion";
import { useEffect, useRef, useState } from "react";
import { Markdown } from "./Markdown";

type TypingMode = "char" | "word";

export const TypewriterMarkdown = ({
  text,
  isActive,
  typingMode = "word",
}: {
  text: string;
  isActive: boolean;
  typingMode?: TypingMode;
}) => {
  const [visibleText, setVisibleText] = useState("");
  const [isTyping, setIsTyping] = useState(false);
  const timeoutId = useRef<NodeJS.Timeout | null>(null);
  const unitQueue = useRef<string[]>([]);

  const resetTyping = () => {
    if (timeoutId.current) clearTimeout(timeoutId.current);
    unitQueue.current = [];
    setVisibleText(text);
    setIsTyping(false);
  };

  useEffect(() => {
    if (!isActive) {
      // If not active, show full text and stop typing
      resetTyping();
      return;
    }

    const units = typingMode === "word" ? text.split(/(\s+)/) : text.split("");

    unitQueue.current = units;
    setVisibleText("");
    setIsTyping(true);

    const typeNext = () => {
      if (unitQueue.current.length === 0) {
        // Finished typing
        setIsTyping(false);
        setVisibleText(text); // Make sure full text is visible
        return;
      }

      const nextUnit = unitQueue.current.shift()!;
      setVisibleText((prev) => prev + nextUnit);

      // Delay settings
      const trimmed = nextUnit.trim();
      // Faster typing for word mode (feel less sluggish)
      const baseDelay = typingMode === "word" ? 30 : 40;
      let delay = Math.random() * 50 + baseDelay;

      if (/[.,!?]$/.test(trimmed)) {
        delay += 150;
      }

      timeoutId.current = setTimeout(typeNext, delay);
    };

    typeNext();

    return () => {
      if (timeoutId.current) clearTimeout(timeoutId.current);
    };
  }, [text, isActive, typingMode]);

  return (
    <motion.div
      className="relative pr-14 pt-2"
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      transition={{ duration: 0.2 }}
    >
      <Markdown>{visibleText}</Markdown>

      {isTyping && (
        <button
          onClick={resetTyping}
          className="absolute top-0 right-2 text-xs text-blue-500 hover:underline"
        >
          Skip
        </button>
      )}
    </motion.div>
  );
};
