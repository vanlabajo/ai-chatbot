import { motion } from "framer-motion";
import { useCallback, useEffect, useRef, useState } from "react";
import { Markdown } from "./Markdown";

type TypingMode = "char" | "word";

export const TypewriterMarkdown = ({
  text,
  isActive,
  typingMode = "word"
}: {
  text: string;
  isActive: boolean;
  typingMode?: TypingMode;
}) => {
  const [visibleText, setVisibleText] = useState("");
  const [isTyping, setIsTyping] = useState(false);
  const timeoutId = useRef<NodeJS.Timeout | null>(null);
  const prevTextRef = useRef<string>("");

  // Helper to get the new part of the text
  const getNewUnits = (oldText: string, newText: string, mode: TypingMode) => {
    if (oldText === newText) return [];
    if (mode === "word") {
      const oldUnits = oldText.split(/(\s+)/);
      const newUnits = newText.split(/(\s+)/);
      let diffIndex = 0;
      const minLen = Math.min(oldUnits.length, newUnits.length);
      while (diffIndex < minLen && oldUnits[diffIndex] === newUnits[diffIndex]) {
        diffIndex++;
      }
      return newUnits.slice(diffIndex);
    } else {
      // char mode
      let diffIndex = 0;
      const minLen = Math.min(oldText.length, newText.length);
      while (diffIndex < minLen && oldText[diffIndex] === newText[diffIndex]) {
        diffIndex++;
      }
      return newText.slice(diffIndex).split("");
    }
  };

  const resetTyping = useCallback(() => {
    if (timeoutId.current) clearTimeout(timeoutId.current);
    setVisibleText(text);
    setIsTyping(false);
    prevTextRef.current = text;
  }, [text]);

  useEffect(() => {
    if (!isActive) {
      resetTyping();
      return;
    }

    let currentText = prevTextRef.current;
    const newUnits = getNewUnits(currentText, text, typingMode);
    setIsTyping(true);

    const typeNext = () => {
      if (newUnits.length === 0) {
        setIsTyping(false);
        setVisibleText(text); // Make sure full text is visible
        prevTextRef.current = text;
        return;
      }
      const nextUnit = newUnits.shift()!;
      currentText += nextUnit;
      setVisibleText(currentText);
      prevTextRef.current = currentText;

      // Delay settings
      const trimmed = nextUnit.trim();
      const baseDelay = typingMode === "word" ? 15 : 20;
      let delay = Math.random() * 25 + baseDelay;
      if (/[.,!?]$/.test(trimmed)) {
        delay += 150;
      }
      timeoutId.current = setTimeout(typeNext, delay);
    };

    typeNext();

    return () => {
      if (timeoutId.current) clearTimeout(timeoutId.current);
    };
  }, [text, isActive, typingMode, resetTyping]);

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