export function LoadingSpinner() {
  return (
    <div className="fixed inset-0 flex items-center justify-center bg-gray-100 dark:bg-gray-900">
      <div className="flex flex-col">
        <div className="relative w-20 h-20 border-purple-200 dark:border-sky-200 border-2 rounded-full">
          <div className="absolute w-20 h-20 border-purple-700 dark:border-sky-700 border-t-2 animate-spin rounded-full"></div>
        </div>
        <div className="relative w-10 h-10 border-purple-200 dark:border-sky-200 border-2 rounded-full">
          <div className="absolute w-10 h-10 border-purple-700 dark:border-sky-700 border-t-2 animate-spin rounded-full"></div>
        </div>
        <div className="relative w-5 h-5 border-purple-200 dark:border-sky-200 border-2 rounded-full">
          <div className="absolute w-5 h-5 border-purple-700 dark:border-sky-700 border-t-2 animate-spin rounded-full"></div>
        </div>
      </div>
    </div>
  );
}