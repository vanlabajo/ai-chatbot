"use client";
import { Button } from "@/components/Button";
import { Divider } from "@/components/Divider";
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
import { deleteSession, getSessions, updateSessionTitle } from "@/lib/api";
import { ChatSession, HubEventNames } from "@/lib/definitions";
import { isAdmin } from "@/lib/msal";
import { getConnection } from "@/lib/signalr";
import { Logo } from "@/public/Logo";
import { HubConnection, HubConnectionState } from "@microsoft/signalr";
import { differenceInCalendarDays, isToday, isYesterday } from "date-fns";
import { PencilLine, Shield } from "lucide-react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import React, { ComponentProps, useCallback, useEffect, useRef, useState } from "react";
import { useChatLoading } from "../chat/ChatLoadingContext";
import { LoadingStatus } from "./LoadingStatus";
import { SidebarLinkActions } from "./SidebarLinkActions";
import { UserProfile } from "./UserProfile";

export function AppSidebar({ ...props }: ComponentProps<typeof Sidebar>) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const sessionId = searchParams.get("sessionId");

  const { isLoading } = useChatLoading();
  const [navigation, setNavigation] = useState<ChatSession[]>([]);
  const [activeSessionId, setActiveSessionId] = useState<string | null>(null);
  const [hubConnection, setHubConnection] = useState<HubConnection | null>(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [editingSessionId, setEditingSessionId] = useState<string | null>(null);
  const [editValue, setEditValue] = useState("");

  const isInitializedRef = useRef(false);
  const sessionUpdateHandlerRef = useRef<((session: ChatSession) => void) | null>(null);
  const sessionDeleteHandlerRef = useRef<((sessionId: string) => void) | null>(null);
  const editInputRef = useRef<HTMLInputElement>(null);

  const cleanupSessionUpdateHandler = useCallback(() => {
    if (sessionUpdateHandlerRef.current && hubConnection) {
      if (hubConnection.state === HubConnectionState.Connected) {
        hubConnection.off(HubEventNames.SessionUpdate, sessionUpdateHandlerRef.current);
      }
      sessionUpdateHandlerRef.current = null;
    }
  }, [hubConnection]);

  const sessionUpdateHandler = useCallback((updatedSession: ChatSession) => {
    setNavigation(prev => {
      const exists = prev.some(session => session.id === updatedSession.id);
      if (exists) {
        return prev.map(session =>
          session.id === updatedSession.id
            ? { ...session, title: updatedSession.title }
            : session
        );
      } else {
        return [updatedSession, ...prev];
      }
    });
  }, []);

  const cleanupSessionDeleteHandler = useCallback(() => {
    if (sessionDeleteHandlerRef.current && hubConnection) {
      if (hubConnection.state === HubConnectionState.Connected) {
        hubConnection.off(HubEventNames.SessionDelete, sessionDeleteHandlerRef.current);
      }
      sessionDeleteHandlerRef.current = null;
    }
  }, [hubConnection]);

  const sessionDeleteHandler = useCallback((deletedSessionId: string) => {
    setNavigation(prev => prev.filter(session => session.id !== deletedSessionId));
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
      sessionDeleteHandlerRef.current = sessionDeleteHandler;
      connection.on(HubEventNames.SessionDelete, sessionDeleteHandlerRef.current);

      setHubConnection(connection);
    } catch (err) {
      console.error("SignalR connection error:", err);
      isInitializedRef.current = false;
    }
  }, [sessionUpdateHandler, sessionDeleteHandler]);

  useEffect(() => {
    initializeConnection();

    return () => {
      if (hubConnection) {
        cleanupSessionUpdateHandler();
        cleanupSessionDeleteHandler();
        hubConnection.stop();
        isInitializedRef.current = false;
      }
    };
  }, [initializeConnection, hubConnection, cleanupSessionUpdateHandler, cleanupSessionDeleteHandler]);

  const handleClick = (sessionId: string) => {
    router.replace(`/chat?sessionId=${sessionId}`);
  };

  const handleNewChat = () => {
    router.replace("/chat");
  };

  const handleSearch = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchTerm(e.target.value);
  };

  const handleEdit = (session: ChatSession) => {
    setTimeout(() => {
      setEditingSessionId(session.id);
      setEditValue(session.title ?? "");
    }, 200);
  };

  const handleEditChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setEditValue(e.target.value);
  };

  const handleEditSave = async (session: ChatSession) => {
    await updateSessionTitle(session.id, editValue);
    setEditingSessionId(null);
  };

  useEffect(() => {
    if (editingSessionId && editInputRef.current) {
      editInputRef.current.focus();
      editInputRef.current.select();
    }
  }, [editingSessionId]);

  const handleDelete = async (sessionId: string) => {
    await deleteSession(sessionId);
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
  const pathname = usePathname();

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
            <div className="relative">
              <Button
                variant="secondary"
                className="h-6 w-6 p-0"
                title={isLoading ? "Please wait for the current response to finish." : "New chat"}
                onClick={handleNewChat}
                disabled={isLoading}
                aria-disabled={isLoading}
              >
                <PencilLine className="w-4" />
                <span className="sr-only">New chat</span>
              </Button>
            </div>
          </span>
        </div>
      </SidebarHeader>
      <SidebarContent>
        {
          isAdmin() && (
            <>
              <SidebarGroup>
                <SidebarGroupContent>
                  <SidebarMenu className="space-y-1">
                    <SidebarMenuItem className="flex items-center">
                      <SidebarLink
                        href="/admin"
                        isActive={pathname === "/admin"}
                        className="flex items-center w-full data-[active=true]:bg-gray-300/50 data-[active=true]:text-gray-900 dark:data-[active=true]:bg-gray-800 dark:data-[active=true]:text-gray-50"
                        icon={Shield}
                      >
                        Admin Dashboard
                      </SidebarLink>
                    </SidebarMenuItem>
                  </SidebarMenu>
                </SidebarGroupContent>
              </SidebarGroup>
              <div className="px-3">
                <Divider className="my-0 py-0" />
              </div>
            </>
          )
        }
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
                        className="flex items-center w-full data-[active=true]:bg-gray-300/50 data-[active=true]:text-gray-900 dark:data-[active=true]:bg-gray-800 dark:data-[active=true]:text-gray-50"
                        title={session.title ?? ""}
                        onClick={() => handleClick(session.id)}
                        actions={
                          <SidebarLinkActions
                            subject={session.title ?? session.id}
                            edit={() => handleEdit(session)}
                            delete={() => handleDelete(session.id)} />
                        }
                      >
                        <span className="flex w-full items-center justify-between">
                          {editingSessionId === session.id ? (
                            <input
                              ref={editInputRef}
                              className="truncate max-w-[12rem] block bg-transparent outline-0"
                              value={editValue}
                              autoFocus
                              onChange={handleEditChange}
                              onBlur={() => handleEditSave(session)}
                              onKeyDown={e => {
                                if (e.key === "Enter") handleEditSave(session);
                                if (e.key === "Escape") setEditingSessionId(null);
                              }}
                            />
                          ) : (
                            <span className="truncate max-w-[12rem] block">{session.title}</span>
                          )}
                          {!session.title && <LoadingStatus className="size-4" />}
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