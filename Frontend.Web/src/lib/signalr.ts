import { getToken } from "@/lib/msal";
import { HubConnection, HubConnectionBuilder, HubConnectionState } from "@microsoft/signalr";

let connection: HubConnection | null = null;
let connectionPromise: Promise<HubConnection> | null = null;

export async function getConnection(): Promise<HubConnection> {
  if (connection && connection.state === HubConnectionState.Connected) {
    return connection;
  }
  if (connectionPromise) {
    return connectionPromise;
  }

  connectionPromise = (async () => {
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

    if (connection.state !== HubConnectionState.Connected) {
      await connection.start();
    }

    return connection;
  })();

  return connectionPromise;
}