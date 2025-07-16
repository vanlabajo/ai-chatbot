"use client";
import { handleLogin } from "@/lib/msal";
import { cx } from "@/lib/utils";

export function SignInButton({ className = '', text = 'Login' }) {
  return (
    <button className={cx("rounded-md bg-indigo-600 px-3.5 py-2.5 text-sm font-semibold text-white shadow-xs hover:bg-indigo-500 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600", className)} onClick={() => handleLogin("redirect")}>
      {text}
    </button>
  )
};