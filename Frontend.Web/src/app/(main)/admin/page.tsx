"use client";
import { DataTable } from "@/components/DataTable";
import { SessionColumns } from "@/components/ui/admin/SessionColumns";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { deleteSessionForAdmin, getSessionsForAdmin } from "@/lib/api";
import { ChatSession } from "@/lib/definitions";
import { useCallback, useEffect, useState } from "react";

export default function Admin() {
  const [sessions, setSessions] = useState<ChatSession[]>([]);
  const [loading, setLoading] = useState(false);
  const [bulkDeleteOpen, setBulkDeleteOpen] = useState(false);
  const [sessionsToDelete, setSessionsToDelete] = useState<ChatSession[]>([]);

  const fetchSessions = useCallback(async () => {
    setLoading(true);
    try {
      const response = await getSessionsForAdmin();
      setSessions(response);
    } catch (err) {
      console.error("Error fetching sessions:", err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchSessions();
  }, [fetchSessions]);

  const exportSessions = async () => {
    const rows: string[] = [];
    rows.push("Session ID,User ID,Title,Created At,Role,Message");
    sessions.forEach((session) => {
      session.messages.forEach((msg) => {
        const line = [
          session.id,
          session.userId,
          session.title,
          new Date(session.timestamp).toISOString(),
          msg.role,
          msg.content.replace(/"/g, '""'), // escape quotes
        ]
          .map((v) => `"${v}"`)
          .join(",");
        rows.push(line);
      });
    });

    const csvContent = rows.join("\n");
    const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);

    const link = document.createElement("a");
    link.href = url;
    link.download = `chat_sessions_${new Date().toISOString()}.csv`;
    link.click();

    URL.revokeObjectURL(url);
  };

  const handleDeleteSession = async (userId: string, sessionId: string) => {
    await deleteSessionForAdmin(userId, sessionId);
    setSessions(prev => prev.filter(session => session.id !== sessionId));
  };

  const handleBulkDelete = async (selectedSessions: ChatSession[]) => {
    for (const session of selectedSessions) {
      await deleteSessionForAdmin(session.userId, session.id);
    }
    setSessions(prev => prev.filter(session => !selectedSessions.some(s => s.id === session.id)));
  };

  return (
    <section
      aria-label="Admin Dashboard"
      className="px-2 sm:px-4 lg:px-6 pt-4 sm:pt-6 lg:pt-10"
    >
      <h1 className="text-lg font-semibold text-gray-900 sm:text-xl dark:text-gray-50">
        Sessions
      </h1>
      <div className="mt-4 sm:mt-6 lg:mt-10">
        {loading && <LoadingSpinner />}
        {!loading && (
          <DataTable
            data={sessions}
            columns={SessionColumns({ onDelete: handleDeleteSession })}
            columnToSearch="title"
            columnFilters={{ userId: "User ID" }}
            refreshData={fetchSessions}
            exportData={exportSessions}
            bulkDelete={handleBulkDelete} />
        )}
      </div>

    </section>
  );
}