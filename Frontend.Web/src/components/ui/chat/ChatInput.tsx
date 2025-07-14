import { Button } from "@/components/Button";
import { Textarea } from "@/components/Textarea";
import { cx } from "@/lib/utils";
import { motion } from 'framer-motion';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';
import { ArrowUpIcon } from "./ChatIcons";

interface ChatInputProps {
  question: string;
  setQuestion: (question: string) => void;
  onSubmit: (text?: string) => void;
  isLoading: boolean;
  hideSuggestions: boolean;
}

const suggestedActions = [
  {
    title: 'How is the weather',
    label: 'in Vancouver?',
    action: 'How is the weather in Vancouver today?',
  },
  {
    title: 'Tell me a fun fact',
    label: 'about programmers',
    action: 'Tell me a fun fact about programmers',
  },
  {
    title: 'What\'s a good book',
    label: 'or movie recommendation?',
    action: 'What\'s a good book or movie recommendation?'
  },
  {
    title: 'Can you help me',
    label: 'brainstorm ideas for a project?',
    action: 'Can you help me brainstorm ideas for a project?'
  }
];

export const ChatInput = ({ question, setQuestion, onSubmit, isLoading, hideSuggestions = false }: ChatInputProps) => {
  const [showSuggestions, setShowSuggestions] = useState(!hideSuggestions);

  useEffect(() => {
    setShowSuggestions(!hideSuggestions);
  }, [hideSuggestions]);

  return (
    <div className="relative w-full flex flex-col gap-4">
      {showSuggestions && (
        <div className="hidden md:grid sm:grid-cols-2 gap-2 w-full">
          {suggestedActions.map((suggestedAction, index) => (
            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: 20 }}
              transition={{ delay: 0.05 * index }}
              key={index}
              className={index > 1 ? 'hidden sm:block' : 'block'}
            >
              <Button
                variant="ghost"
                onClick={() => {
                  const text = suggestedAction.action;
                  onSubmit(text);
                  setShowSuggestions(false);
                }}
                className="text-left border rounded-xl px-4 py-3.5 text-sm flex-1 gap-1 sm:flex-col w-full h-auto justify-start items-start"
              >
                <span className="font-medium">{suggestedAction.title}</span>
                <span className="text-muted-foreground">
                  {suggestedAction.label}
                </span>
              </Button>
            </motion.div>
          ))}
        </div>
      )}
      <input
        type="file"
        className="fixed -top-4 -left-4 size-0.5 opacity-0 pointer-events-none"
        multiple
        tabIndex={-1}
      />

      <Textarea
        placeholder="Send a message..."
        className={cx(
          'min-h-[24px] max-h-[calc(75dvh)] overflow-hidden resize-none rounded-xl text-base bg-muted',
        )}
        value={question}
        onChange={(e) => setQuestion(e.target.value)}
        onKeyDown={(event) => {
          if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault();

            if (isLoading) {
              toast.error('Please wait for the model to finish its response!');
            } else {
              setShowSuggestions(false);
              onSubmit(question);
            }
          }
        }}
        rows={3}
        autoFocus
      />

      <Button
        variant="secondary"
        className="rounded-full p-1.5 h-fit absolute bottom-2 right-2 m-0.5 border dark:border-zinc-600"
        onClick={() => onSubmit(question)}
        disabled={question.length === 0}
      >
        <ArrowUpIcon size={14} />
      </Button>
    </div>
  );
}