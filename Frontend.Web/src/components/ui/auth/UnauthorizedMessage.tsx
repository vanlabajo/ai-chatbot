
import { Logo } from "@/public/Logo"
import { SignInButton } from "./SignInButton"

export function UnauthorizedMessage() {
  return (
    <div className="flex h-screen flex-col items-center justify-center">
      <Logo className="mt-6 h-auto w-32" />
      <h1 className="mt-4 text-2xl font-semibold text-gray-900 dark:text-gray-50">
        Welcome to AI Chatbot!
      </h1>
      <p className="mt-2 text-sm text-gray-600 dark:text-gray-400">
        Please sign in to start a new conversation or continue where you left off.
      </p>
      <SignInButton className="mt-8" text="Sign in" />
    </div>
  )
}