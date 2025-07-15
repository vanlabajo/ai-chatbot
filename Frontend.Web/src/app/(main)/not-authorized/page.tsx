import { siteConfig } from "@/app/siteConfig"
import { Button } from "@/components/Button"
import { ArrowAnimated } from "@/components/ui/icons/ArrowAnimated"
import { Logo } from "@/public/Logo"
import Link from "next/link"

export default function NotAuthorized() {
  return (
    <div className="flex h-screen flex-col items-center justify-center">
      <Link href={siteConfig.baseLinks.home}>
        <Logo className="mt-6 h-auto w-96" />
      </Link>
      <p className="mt-6 text-4xl font-semibold text-indigo-600 sm:text-5xl dark:text-indigo-500">
        403
      </p>
      <h1 className="mt-4 text-2xl font-semibold text-gray-900 dark:text-gray-50">
        Not authorized
      </h1>
      <p className="mt-2 text-sm text-gray-600 dark:text-gray-400">
        You do not have permission to view this page.
      </p>
      <Button asChild className="group mt-8" variant="light">
        <Link href={siteConfig.baseLinks.home}>
          Go to the home page
          <ArrowAnimated
            className="stroke-gray-900 dark:stroke-gray-50"
            aria-hidden="true"
          />
        </Link>
      </Button>
    </div>
  )
}
