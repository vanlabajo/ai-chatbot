export default function Layout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    <>
      <div className="bg-white dark:bg-gray-925">
        {children}
      </div>
    </>
  )
}
