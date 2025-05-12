import { loginRequest, msalConfig } from "@/lib/msalConfig";
import { AuthenticationResult, EventType, PublicClientApplication } from "@azure/msal-browser";

export const msalInstance = new PublicClientApplication(msalConfig);

const SCOPES = [...loginRequest.scopes];

/**
 * Initialize MSAL instance and set the active account.
 */
export async function initializeMsal(): Promise<void> {
  await msalInstance.initialize();

  // Set the active account
  const accounts = msalInstance.getAllAccounts();
  if (accounts.length > 0) {
    const activeAccount = msalInstance.getActiveAccount() ?? accounts[0];
    msalInstance.setActiveAccount(activeAccount);
  }

  // Add event callbacks
  msalInstance.addEventCallback((event) => {
    if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
      const payload = event.payload as AuthenticationResult;
      msalInstance.setActiveAccount(payload.account);
    }
  });
}

/**
 * Get a valid token using MSAL.
 */
export async function getToken(): Promise<string | null> {
  const acquireAccessToken = async () => {
    const activeAccount = msalInstance.getActiveAccount();
    const accounts = msalInstance.getAllAccounts();

    if (!activeAccount && accounts.length === 0) {
      console.warn("No active account found. User might not be logged in.");
      return null;
    }

    const request = {
      scopes: SCOPES,
      account: activeAccount || accounts[0],
    };

    try {
      console.log("Attempting to acquire token silently...");
      const authResult = await msalInstance.acquireTokenSilent(request);
      console.log("Token acquired silently.");
      return authResult.accessToken;
    } catch (error) {
      console.warn("Silent token acquisition failed. Attempting popup login...");
      try {
        const authResult = await msalInstance.acquireTokenPopup(request);
        console.log("Token acquired via popup.");
        return authResult.accessToken;
      } catch (popupError) {
        console.error("Error acquiring token via popup:", popupError);
        throw new Error("Failed to acquire token. Please try logging in again.");
      }
    }
  };

  if (typeof window !== "undefined") {
    return await acquireAccessToken();
  }

  console.warn("getToken called in a non-browser environment.");
  return null;
}

/**
 * Handle user login.
 */
export const handleLogin = (loginType: "popup" | "redirect" = "redirect"): void => {
  if (loginType === "popup") {
    msalInstance.loginPopup(loginRequest).catch((e) => {
      console.error(`loginPopup failed: ${e}`);
    });
  } else if (loginType === "redirect") {
    msalInstance.loginRedirect(loginRequest).catch((e) => {
      console.error(`loginRedirect failed: ${e}`);
    });
  }
};

/**
 * Handle user logout.
 */
export const handleLogout = (logoutType: "popup" | "redirect" = "redirect"): void => {
  const activeAccount = msalInstance.getActiveAccount();

  if (!activeAccount) {
    console.warn("No active account found during logout.");
  }

  if (logoutType === "popup") {
    msalInstance.logoutPopup().catch((e: any) => {
      console.error(`logoutPopup failed: ${e}`);
    });
  } else if (logoutType === "redirect") {
    const logoutRequest = {
      account: activeAccount,
      postLogoutRedirectUri: "/",
    };
    msalInstance.logoutRedirect(logoutRequest).catch((e) => {
      console.error(`logoutRedirect failed: ${e}`);
    });
  }
};

/**
 * Check if the user is logged in.
 */
export function isLoggedIn(): boolean {
  const activeAccount = msalInstance.getActiveAccount();
  return !!activeAccount;
}