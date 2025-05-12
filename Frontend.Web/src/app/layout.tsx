import { SidebarProvider, SidebarTrigger } from "@/components/Sidebar";
import { MsalWrapper } from "@/components/ui/auth/MsalWrapper";
import { AppSidebar } from "@/components/ui/navigation/AppSidebar";
import { Breadcrumbs } from "@/components/ui/navigation/Breadcrumbs";
import type { Metadata } from "next";
import { ThemeProvider } from "next-themes";
import localFont from "next/font/local";
import { cookies } from "next/headers";
import "./globals.css";
import { siteConfig } from "./siteConfig";

const geistSans = localFont({
  src: "./fonts/GeistVF.woff",
  variable: "--font-geist-sans",
  weight: "100 900",
})
const geistMono = localFont({
  src: "./fonts/GeistMonoVF.woff",
  variable: "--font-geist-mono",
  weight: "100 900",
})

export const metadata: Metadata = {
  metadataBase: new URL("https://github.com/vanlabajo/ai-chatbot"),
  title: siteConfig.name,
  description: siteConfig.description,
  keywords: [
    "AI Chatbot",
    "Artificial Intelligence",
    "Natural Language Processing",
    "OpenAI",
    "Azure OpenAI",
    "Chat Assistant",
    "AI Assistant",
    "Conversational AI",
    "React",
    "Next.js",
    "TypeScript",
    "MSAL Authentication",
    "Data Visualization",
    "Dashboard",
    "Real-Time Chat",
    "Interactive Dashboard",
    "Software Application",
    "Business Intelligence",
  ],
  authors: [
    {
      name: "Van Labajo",
      url: "https://github.com/vanlabajo",
    },
  ],
  creator: "vanlabajo",
  openGraph: {
    type: "website",
    locale: "en_US",
    url: siteConfig.url,
    title: siteConfig.name,
    description: siteConfig.description,
    siteName: siteConfig.name,
  },
  icons: {
    icon: [
      {
        type: "image/svg+xml",
        url: "/bot.svg"
      }
    ],
  },
};

export default async function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const cookieStore = await cookies();
  const defaultOpen = cookieStore.get("sidebar:state")?.value === "true";

  return (
    <html lang="en" className="h-full" suppressHydrationWarning>
      <body
        className={`${geistSans.variable} ${geistMono.variable} bg-white-50 h-full antialiased dark:bg-gray-950`}
      >
        <ThemeProvider defaultTheme="system" attribute="class" disableTransitionOnChange>
          <MsalWrapper>
            <SidebarProvider defaultOpen={defaultOpen}>
              <AppSidebar />
              <div className="w-full">
                <header className="sticky top-0 z-10 flex h-16 shrink-0 items-center gap-2 border-b border-gray-200 bg-white px-4 dark:border-gray-800 dark:bg-gray-950">
                  <SidebarTrigger className="-ml-1" />
                  <div className="mr-2 h-4 w-px bg-gray-200 dark:bg-gray-800" />
                  <Breadcrumbs />
                </header>
                <MsalWrapper>
                  <main>{children}</main>
                </MsalWrapper>
              </div>
            </SidebarProvider>
          </MsalWrapper>
        </ThemeProvider>
      </body>
    </html>
  );
}