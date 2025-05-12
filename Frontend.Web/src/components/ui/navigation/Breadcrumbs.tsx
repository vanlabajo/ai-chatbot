"use client";
import { ChevronRight } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";

export function Breadcrumbs() {
  const pathname = usePathname();

  // Split the current path into segments
  const pathSegments = pathname.split("/").filter((segment) => segment);

  // Add a "Home" segment for the root path
  const breadcrumbs = [
    { name: "Home", href: "/" },
    ...pathSegments.map((segment, index) => {
      const href = "/" + pathSegments.slice(0, index + 1).join("/");
      return { name: segment.charAt(0).toUpperCase() + segment.slice(1), href };
    }),
  ];

  return (
    <nav aria-label="Breadcrumb" className="ml-2">
      <ol role="list" className="flex items-center space-x-3 text-sm">
        {breadcrumbs.map((breadcrumb, index) => (
          <li key={breadcrumb.href} className="flex">
            <div className="flex items-center">
              <Link
                href={breadcrumb.href}
                aria-current={index === breadcrumbs.length - 1 ? "page" : undefined}
                className={`${index === breadcrumbs.length - 1
                  ? "text-gray-900 dark:text-gray-50"
                  : "text-gray-500 transition hover:text-gray-700 dark:text-gray-400 hover:dark:text-gray-300"
                  }`}
              >
                {breadcrumb.name}
              </Link>
              {index < breadcrumbs.length - 1 && (
                <ChevronRight
                  className="size-4 shrink-0 text-gray-600 dark:text-gray-400"
                  aria-hidden="true"
                />
              )}
            </div>
          </li>
        ))}
      </ol>
    </nav>
  );
}