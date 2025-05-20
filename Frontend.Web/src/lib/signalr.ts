import { getToken } from "@/lib/msal";
import { HubConnection, HubConnectionBuilder, HubConnectionState } from "@microsoft/signalr";

let connection: HubConnection | null = null;

export async function getConnection() {
  if (connection && connection.state === HubConnectionState.Connected) {
    return connection;
  }

  const apiEndpoint = process.env.NEXT_PUBLIC_API_ENDPOINT;
  if (!apiEndpoint) throw new Error("API endpoint is not defined");

  const token = await getToken();
  if (!token) throw new Error("Failed to fetch token");

  connection = new HubConnectionBuilder()
    .withUrl(`${apiEndpoint}/hubs/chat`, {
      accessTokenFactory: () => token,
      withCredentials: true
    })
    .withAutomaticReconnect()
    .build();

  return connection;
}