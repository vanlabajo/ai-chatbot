"use client";
import { Button } from "@/components/Button";
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
import { getSessions } from "@/lib/api";
import { ChatSession, HubEventNames } from "@/lib/definitions";
import { getConnection } from "@/lib/signalr";
import { Logo } from "@/public/Logo";
import { HubConnection, HubConnectionState } from "@microsoft/signalr";
import { differenceInCalendarDays, isToday, isYesterday } from "date-fns";
import { PencilLine } from "lucide-react";
import { useRouter, useSearchParams } from "next/navigation";
import React, { ComponentProps, useCallback, useEffect, useRef, useState } from "react";
import { LoadingStatus } from "./LoadingStatus";
import { UserProfile } from "./UserProfile";

export function AppSidebar({ ...props }: ComponentProps<typeof Sidebar>) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const sessionId = searchParams.get("sessionId");

  const [navigation, setNavigation] = useState<ChatSession[]>([]);
  const [activeSessionId, setActiveSessionId] = useState<string | null>(null);
  const [hubConnection, setHubConnection] = useState<HubConnection | null>(null);
  const [searchTerm, setSearchTerm] = useState("");

  const isInitializedRef = useRef(false);
  const sessionUpdateHandlerRef = useRef<((session: ChatSession) => void) | null>(null);

  const cleanupSessionUpdateHandler = () => {
    if (sessionUpdateHandlerRef.current && hubConnection) {
      if (hubConnection.state === HubConnectionState.Connected) {
        hubConnection.off(HubEventNames.SessionUpdate, sessionUpdateHandlerRef.current);
      }
      sessionUpdateHandlerRef.current = null;
    }
  };

  const sessionUpdateHandler = useCallback((updatedSession: ChatSession) => {
    setNavigation(prev => {
      const exists = prev.some(session => session.id === updatedSession.id);
      if (exists) {
        return prev.map(session =>
          session.id === updatedSession.id
            ? { ...session, subject: updatedSession.title }
            : session
        );
      } else {
        return [updatedSession, ...prev];
      }
    });
  }, []);

  const initialize = useCallback(async () => {
    try {
      const sessions = await getSessions();
      setNavigation(sessions);
    } catch (error) {
      console.error("Error fetching sessions:", error);
    }
  }, []);

  useEffect(() => {
    initialize();
  }, [initialize]);

  useEffect(() => {
    const found = navigation.find(session => session.id === sessionId);
    setActiveSessionId(found ? found.id : null);
  }, [sessionId, navigation]);

  const initializeConnection = useCallback(async () => {
    if (isInitializedRef.current) {
      console.warn("HubConnection already initialized");
      return;
    }
    isInitializedRef.current = true;

    try {
      const connection = await getConnection();
      if (connection.state !== HubConnectionState.Connected) {
        await connection.start();
      }

      sessionUpdateHandlerRef.current = sessionUpdateHandler;
      connection.on(HubEventNames.SessionUpdate, sessionUpdateHandlerRef.current);

      setHubConnection(connection);
    } catch (err) {
      console.error("SignalR connection error:", err);
      isInitializedRef.current = false;
    }
  }, [sessionUpdateHandler]);

  useEffect(() => {
    initializeConnection();

    return () => {
      if (hubConnection) {
        cleanupSessionUpdateHandler();
        hubConnection.stop();
        isInitializedRef.current = false;
      }
    };
  }, [initializeConnection, hubConnection]);

  const handleClick = (sessionId: string) => {
    router.replace(`/chat?sessionId=${sessionId}`);
  };

  const handleNewChat = () => {
    router.replace("/chat");
  };

  const handleSearch = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchTerm(e.target.value);
  };

  const groupSessionsByDay = (sessions: ChatSession[]) => {
    const groups: Record<string, ChatSession[]> = {};
    const now = new Date();

    sessions.forEach(session => {
      const date = session.timestamp ? new Date(session.timestamp) : null;
      let label = "Unknown";
      if (date) {
        if (isToday(date)) {
          label = "Today";
        } else if (isYesterday(date)) {
          label = "Yesterday";
        } else {
          const daysAgo = differenceInCalendarDays(now, date);
          label = `${daysAgo} days ago`;
        }
      }
      if (!groups[label]) groups[label] = [];
      groups[label].push(session);
    });

    return groups;
  }

  const filteredNavigation = navigation
    .filter(session =>
      (session.title ?? "")
        .toLowerCase()
        .includes(searchTerm.toLowerCase())
    )
    .sort((a, b) => {
      // Sort descending (most recent first)
      const aTime = new Date(a.timestamp ?? 0).getTime();
      const bTime = new Date(b.timestamp ?? 0).getTime();
      return bTime - aTime;
    });

  const groupedSessions = groupSessionsByDay(filteredNavigation);

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
          <span className="flex size-6 ml-auto mb-auto items-center justify-center rounded-md bg-white dark:bg-gray-900 dark:ring-gray-800">
            <Button
              variant="secondary"
              className="h-6 w-6 p-0"
              title="New chat"
              onClick={handleNewChat}
            >
              <PencilLine className="w-4" />
              <span className="sr-only">New chat</span>
            </Button>
          </span>
        </div>
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupContent>
            <Input
              type="search"
              placeholder="Search sessions..."
              className="[&>input]:sm:py-1.5"
              value={searchTerm ?? ""}
              onChange={handleSearch}
            />
          </SidebarGroupContent>
        </SidebarGroup>
        <SidebarGroup className="pt-0">
          <SidebarGroupContent>
            <SidebarMenu className="space-y-1">
              {Object.entries(groupedSessions).map(([label, sessions]) => (
                <React.Fragment key={label}>
                  <SidebarMenuItem className="flex items-center">
                    <span className="text-xs font-semibold text-gray-500 dark:text-gray-400">
                      {label}
                    </span>
                  </SidebarMenuItem>
                  {sessions.map((session: ChatSession) => (
                    <SidebarMenuItem
                      key={session.id}
                      className="flex items-center"
                    >
                      <SidebarLink
                        href={`#${session.id}`}
                        isActive={session.id === activeSessionId}
                        className="flex items-center w-full data-[active=true]:bg-gray-300/50 data-[active=true]:text-gray-900"
                        title={session.title ?? ""}
                        onClick={() => handleClick(session.id)}
                      >
                        <span className="flex w-full items-center justify-between">
                          <span className="truncate max-w-[12rem] block">{session.title}</span>
                          {!session.title && <LoadingStatus className="size-5" />}
                        </span>
                      </SidebarLink>
                    </SidebarMenuItem>
                  ))}
                </React.Fragment>
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