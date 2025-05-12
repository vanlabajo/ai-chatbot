import { msalInstance } from "@/lib/msal";
import { graphConfig, graphRequest } from "@/lib/msalConfig";

export interface UserInfo {
  displayName?: string;
  mail?: string;
  userPrincipalName?: string;
}

/**
 * Get a valid token using MSAL.
 */
async function getToken(): Promise<string> {
  const instance = msalInstance;
  const account = instance.getActiveAccount();

  if (!account) {
    throw new Error("No active account! Verify a user has been signed in and setActiveAccount has been called.");
  }

  // Acquire a token silently (MSAL handles caching and expiration internally)
  const tokenResponse = await instance.acquireTokenSilent({
    ...graphRequest,
    account: account,
  });

  return tokenResponse.accessToken;
}

/**
 * Fetch data from a given URL with the provided token.
 */
async function fetchWithToken(url: string, token: string): Promise<Response> {
  const headers = new Headers();
  headers.append("Authorization", `Bearer ${token}`);

  const response = await fetch(url, { method: "GET", headers });
  if (!response.ok) {
    throw new Error(`API request failed: ${response.statusText}`);
  }

  return response;
}

/**
 * Get the user's photo avatar as a blob URL.
 */
export async function getUserPhotoAvatar(): Promise<string> {
  const token = await getToken();
  const response = await fetchWithToken(graphConfig.graphMePhotoEndpoint, token);
  const blob = await response.blob();
  return URL.createObjectURL(blob);
}

/**
 * Get the user's information from Microsoft Graph.
 */
export async function getUserInfo(): Promise<UserInfo> {
  const token = await getToken();
  const response = await fetchWithToken(graphConfig.graphMeEndpoint, token);
  return await response.json();
}