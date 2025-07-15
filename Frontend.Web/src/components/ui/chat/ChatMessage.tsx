import { ChatMessage } from "@/lib/definitions";
import { cx } from "@/lib/utils";
import { motion } from 'framer-motion';
import { SparklesIcon } from "./ChatIcons";
import { ChatMessageActions } from './ChatMessageActions';
import { Markdown } from './Markdown';
import { TypewriterMarkdown } from './TypewriterMarkdown';

export const PreviewChatMessage = ({
  message,
  activateTypewritingEffect = false
}: {
  message: ChatMessage;
  activateTypewritingEffect?: boolean;
  onMessageRendered?: () => void;
}) => {
  if (message.role !== 'assistant' && message.role !== 'user') return;

  return (
    <motion.div
      className="w-full mx-auto max-w-3xl px-4 group/message"
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3, ease: "easeOut" }}
      data-role={message.role}
    >
      <div
        className={cx(
          'group-data-[role=user]/message:bg-zinc-700 dark:group-data-[role=user]/message:bg-muted group-data-[role=user]/message:text-white flex gap-4 group-data-[role=user]/message:px-3 w-full group-data-[role=user]/message:w-fit group-data-[role=user]/message:ml-auto group-data-[role=user]/message:max-w-2xl group-data-[role=user]/message:py-2 rounded-xl'
        )}
      >
        {message.role === 'assistant' && (
          <div className="size-8 flex items-center rounded-full justify-center ring-1 shrink-0 ring-border ring-gray-200">
            <SparklesIcon size={14} />
          </div>
        )}

        <div className="flex flex-col w-full">
          {message.content && (
            <div className="flex flex-col gap-4 text-left">
              {message.role === "assistant" ? (
                <TypewriterMarkdown
                  text={message.content}
                  isActive={activateTypewritingEffect}
                />
              ) : (
                <Markdown>{message.content}</Markdown>
              )}
            </div>
          )}

          {message.role === 'assistant' && (
            <ChatMessageActions message={message} />
          )}
        </div>
      </div>
    </motion.div>
  );
};

export const ThinkingChatMessage = () => {
  const role = 'assistant';

  return (
    <motion.div
      className="w-full mx-auto max-w-3xl px-4 group/message "
      initial={{ y: 5, opacity: 0 }}
      animate={{ y: 0, opacity: 1, transition: { delay: 0.2 } }}
      data-role={role}
    >
      <div
        className={cx(
          'flex gap-4 group-data-[role=user]/message:px-3 w-full group-data-[role=user]/message:w-fit group-data-[role=user]/message:ml-auto group-data-[role=user]/message:max-w-2xl group-data-[role=user]/message:py-2 rounded-xl',
          'group-data-[role=user]/message:bg-muted'
        )}
      >
        <div className="size-8 flex items-center rounded-full justify-center ring-1 shrink-0 ring-border ring-gray-200">
          <SparklesIcon size={14} />
        </div>
        <div className="flex flex-col gap-4 text-left text-gray-500">
          Thinking...
        </div>
      </div>
    </motion.div>
  );
};