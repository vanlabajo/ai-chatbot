import { motion } from 'framer-motion';
import { BotIcon, MessageCircle } from 'lucide-react';

export const ChatOverview = () => {
  return (
    <>
      <motion.div
        key="overview"
        className="max-w-3xl mx-auto md:mt-20"
        initial={{ opacity: 0, scale: 0.98 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.98 }}
        transition={{ delay: 0.75 }}
      >
        <div className="rounded-xl p-6 flex flex-col gap-8 leading-relaxed text-center max-w-xl">
          <p className="flex flex-row justify-center gap-4 items-center">
            <BotIcon size={44} />
            <span>+</span>
            <MessageCircle size={44} />
          </p>
          <p>
            Welcome to <strong>AI Chatbot</strong><br />
            a project to ramp up on AI development by<br />
            <strong>Van Labajo</strong>.
          </p>
        </div>
      </motion.div>
    </>
  );
};