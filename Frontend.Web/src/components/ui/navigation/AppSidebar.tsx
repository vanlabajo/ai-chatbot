"use client";
import { Input } from "@/components/Input";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarHeader,
  SidebarLink,
  SidebarMenu,
  SidebarMenuItem,
} from "@/components/Sidebar";
import { getToken } from "@/lib/msal";
import { ChatSession } from "@/lib/types";
import { Logo } from "@/public/Logo";
import { ComponentProps, useEffect, useRef, useState } from "react";
import { LoadingStatus } from "./LoadingStatus";
import { UserProfile } from "./UserProfile";

export function AppSidebar({ ...props }: ComponentProps<typeof Sidebar>) {
  const [navigation, setNavigation] = useState<ChatSession[]>([]);
  const [activeSessionId, setActiveSessionId] = useState<string | null>(null);
  const sessionIds = useRef<Set<string>>(new Set());
  const eventSourceRef = useRef<EventSource | null>(null);
  const cancelledRef = useRef(false);

  useEffect(() => {
    cancelledRef.current = false;

    async function startEventSource() {
      const token = await getToken();
      if (!token) {
        console.error("Failed to fetch token");
        return;
      }
      const apiEndpoint = process.env.NEXT_PUBLIC_API_ENDPOINT;
      if (!apiEndpoint) {
        throw new Error("API endpoint is not defined");
      }
      const url = new URL(apiEndpoint + "/stream/chat/sessions");
      url.searchParams.append("access_token", encodeURIComponent(token));

      if (eventSourceRef.current) {
        eventSourceRef.current.close();
      }

      const es = new EventSource(url.toString());
      eventSourceRef.current = es;

      es.onmessage = (event) => {
        if (cancelledRef.current) return;
        try {
          const session: ChatSession = JSON.parse(event.data);
          if (!sessionIds.current.has(session.sessionId)) {
            sessionIds.current.add(session.sessionId);
            setNavigation((prev) => {
              const updated = [...prev, session];
              // If only one session, set it as active
              if (updated.length === 1) {
                setActiveSessionId(session.sessionId);
              }
              return updated;
            });
          }
        } catch (error) {
          console.error("Failed to parse event data:", error);
        }
      };

      es.onerror = (error) => {
        if (cancelledRef.current) return;
        console.error("EventSource error:", error);
      };
    }

    function simulateSessionStream(onSession: (session: ChatSession) => void) {
      let count = 0;
      const interval = setInterval(() => {
        count++;
        const session: ChatSession = {
          sessionId: `session-${count}`,
          subject: `Simulated Subject ${count}`,
          timestamp: new Date().toISOString(),
          messages: [],
        };
        onSession(session);

        // Optionally, simulate an update to an existing session
        if (count === 3) {
          setTimeout(() => {
            onSession({
              sessionId: "session-2",
              subject: "Simulated Subject 2 (updated)",
              timestamp: new Date().toISOString(),
              messages: [],
            });
          }, 1500);
        }

        // Stop after 5 sessions for demo
        if (count >= 5) clearInterval(interval);
      }, 2000);

      return () => clearInterval(interval);
    }

    startEventSource();
    // const cleanup = simulateSessionStream((session) => {
    //   setNavigation((prev) => {
    //     const existingIndex = prev.findIndex(s => s.sessionId === session.sessionId);
    //     if (existingIndex === -1) {
    //       // New session
    //       const updated = [...prev, session];
    //       if (updated.length === 1) setActiveSessionId(session.sessionId);
    //       return updated;
    //     } else {
    //       // Update existing session
    //       const updated = [...prev];
    //       updated[existingIndex] = { ...prev[existingIndex], ...session };
    //       return updated;
    //     }
    //   });
    // });

    return () => {
      cancelledRef.current = true;
      if (eventSourceRef.current) {
        eventSourceRef.current.close();
        eventSourceRef.current = null;
      }
      // cleanup && cleanup();
    };
  }, []);

  useEffect(() => {
    if (navigation.length === 1) {
      setActiveSessionId(navigation[0].sessionId);
    }
  }, [navigation]);

  return (
    <Sidebar {...props} className="bg-gray-50 dark:bg-gray-925">
      <SidebarHeader className="px-3 py-4">
        <div className="flex items-center gap-3">
          <span className="flex size-9 items-center justify-center rounded-md bg-white shadow-sm ring-1 ring-gray-200 dark:bg-gray-900 dark:ring-gray-800">
            <Logo className="w-full" />
          </span>
          <div>
            <span className="block text-sm font-semibold text-gray-900 dark:text-gray-50">
              AI Chatbot
            </span>
            <span className="block text-xs text-gray-900 dark:text-gray-50">
              AI Development Kickstart
            </span>
          </div>
        </div>
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupContent>
            <Input
              type="search"
              placeholder="Search sessions..."
              className="[&>input]:sm:py-1.5"
            />
          </SidebarGroupContent>
        </SidebarGroup>
        <SidebarGroup className="pt-0">
          <SidebarGroupContent>
            <SidebarMenu className="space-y-1">
              {navigation.map((session: ChatSession) => (
                <SidebarMenuItem key={session.sessionId}>
                  <SidebarLink
                    href={`#${session.sessionId}`}
                    isActive={session.sessionId === activeSessionId}
                    className="data-[active=true]:bg-gray-300/50 data-[active=true]:text-gray-900"
                    onClick={() => setActiveSessionId(session.sessionId)}
                  >
                    {session.subject || <LoadingStatus />}
                  </SidebarLink>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
      <SidebarFooter>
        <div className="border-t border-gray-200 dark:border-gray-800" />
        <UserProfile />
      </SidebarFooter>
    </Sidebar>
  );
}