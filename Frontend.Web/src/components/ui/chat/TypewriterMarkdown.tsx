import { motion } from "framer-motion";
import { useEffect, useRef, useState } from "react";
import { Markdown } from "./Markdown";

type TypingMode = "char" | "word";

export const TypewriterMarkdown = ({
  text,
  isActive,
  onTypingFinished,
  typingMode = "word"
}: {
  text: string;
  isActive: boolean;
  onTypingFinished?: () => void;
  typingMode?: TypingMode;
}) => {
  const [visibleText, setVisibleText] = useState("");
  const [isTyping, setIsTyping] = useState(false);
  const timeoutId = useRef<NodeJS.Timeout | null>(null);
  const prevTextRef = useRef<string>("");

  // Helper to get the new part of the text
  const getNewUnits = (oldText: string, newText: string, mode: TypingMode) => {
    if (mode === "word") {
      const oldUnits = oldText.split(/(\s+)/);
      const newUnits = newText.split(/(\s+)/);
      return newUnits.slice(oldUnits.length);
    } else {
      return newText.slice(oldText.length).split("");
    }
  }

  const resetTyping = () => {
    if (timeoutId.current) clearTimeout(timeoutId.current);
    setVisibleText(text);
    setIsTyping(false);
    prevTextRef.current = text;
    onTypingFinished?.();
  };

  useEffect(() => {
    if (!isActive) {
      resetTyping();
      return;
    }

    const oldText = prevTextRef.current;
    // If this is the first chunk or oldText is empty, animate the whole text
    const isFirstChunk = oldText.length === 0;
    const newUnits = isFirstChunk
      ? (typingMode === "word" ? text.split(/(\s+)/) : text.split(""))
      : getNewUnits(oldText, text, typingMode);

    let currentText = isFirstChunk ? "" : oldText;
    setIsTyping(true);

    const typeNext = () => {
      if (newUnits.length === 0) {
        setIsTyping(false);
        setVisibleText(text);
        prevTextRef.current = text;
        onTypingFinished?.();
        return;
      }
      const nextUnit = newUnits.shift()!;
      currentText += nextUnit;
      setVisibleText(currentText);

      // Delay settings
      const trimmed = nextUnit.trim();
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
  }, [text, isActive, typingMode, onTypingFinished]);

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