import { ThemeToggle } from "@/components/ui/chat/ThemeToggle";

export function ChatHeader() {
  return (
    <>
      <header className="flex items-center justify-between px-2 sm:px-4 py-2 text-black dark:text-white w-full">
        <div className="flex items-center space-x-1 sm:space-x-2">
          <ThemeToggle />
        </div>
      </header>
    </>
  );
}