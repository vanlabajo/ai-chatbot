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