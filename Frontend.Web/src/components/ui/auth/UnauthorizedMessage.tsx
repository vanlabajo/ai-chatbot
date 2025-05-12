import { SignInButton } from "./SignInButton";

export function UnauthorizedMessage() {
  return (
    <main className="grid min-h-full place-items-center bg-white px-6 py-24 sm:py-32 lg:px-8">
      <div className="text-center">
        <p className="text-base font-semibold text-indigo-600">401</p>
        <h1 className="mt-4 text-5xl font-semibold tracking-tight text-balance text-gray-900 sm:text-7xl">Unauthorized</h1>
        <p className="mt-6 text-lg font-medium text-pretty text-gray-500 sm:text-xl/8">You are not authorized to view this page.</p>
        <p className="mt-6 text-lg font-medium text-pretty text-gray-500 sm:text-xl/8">Please login to continue.</p>
      </div>
      <div className="mt-10 flex items-center justify-center gap-x-6">
        <SignInButton />
      </div>
    </main>
  )
}