import { getToken } from "@/lib/msal";
import { ChatSession } from "./definitions";

export async function getSessions(): Promise<ChatSession[]> {
  const endpoint = process.env.NEXT_PUBLIC_API_ENDPOINT + "/api/chat/sessions";
  const token = await getToken();

  const res = await fetch(endpoint, {
    headers: {
      "Authorization": `Bearer ${token}`,
      "Content-Type": "application/json",
    },
  });

  if (!res.ok) {
    throw new Error(`Failed to fetch sessions: ${res.statusText}`);
  }

  return res.json() as Promise<ChatSession[]>;
}

export async function getSessionsForAdmin(): Promise<ChatSession[]> {
  const endpoint = process.env.NEXT_PUBLIC_API_ENDPOINT + "/api/admin/sessions";
  const token = await getToken();

  const res = await fetch(endpoint, {
    headers: {
      "Authorization": `Bearer ${token}`,
      "Content-Type": "application/json",
    },
  });

  if (!res.ok) {
    throw new Error(`Failed to fetch sessions for admin: ${res.statusText}`);
  }

  return res.json() as Promise<ChatSession[]>;
}

export async function deleteSessionForAdmin(userId: string, sessionId: string): Promise<void> {
  const endpoint = `${process.env.NEXT_PUBLIC_API_ENDPOINT}/api/admin/sessions/${userId}/${sessionId}`;
  const token = await getToken();

  const res = await fetch(endpoint, {
    method: "DELETE",
    headers: {
      "Authorization": `Bearer ${token}`,
      "Content-Type": "application/json",
    },
  });

  if (!res.ok) {
    throw new Error(`Failed to delete session: ${res.statusText}`);
  }
}