import { Configuration } from "@azure/msal-browser";

if (!process.env.NEXT_PUBLIC_CLIENT_ID || !process.env.NEXT_PUBLIC_TENANT_ID) {
  throw new Error("Missing required environment variables");
}

export const msalConfig: Configuration = {
  auth: {
    clientId: process.env.NEXT_PUBLIC_CLIENT_ID || "",
    authority: `https://login.microsoftonline.com/${process.env.NEXT_PUBLIC_TENANT_ID}`,
    redirectUri: process.env.NEXT_PUBLIC_REDIRECT_URI || "http://localhost:3000",
    postLogoutRedirectUri: "/"
  },
  cache: {
    cacheLocation: "localStorage",
    storeAuthStateInCookie: false,
  }
};

// Add here scopes for id token to be used at MS Identity Platform endpoints.
export const loginRequest = {
  scopes: [process.env.NEXT_PUBLIC_SCOPE || process.env.NEXT_PUBLIC_CLIENT_ID || "openid"]
};

// Add here scopes for access token to be used at MS Graph API endpoints.
export const graphRequest = {
  scopes: ["User.Read"]
};

// Add here the endpoints for MS Graph API services you would like to use.
export const graphConfig = {
  graphMeEndpoint: "https://graph.microsoft.com/v1.0/me",
  graphMePhotoEndpoint: "https://graph.microsoft.com/v1.0/me/photo/$value",
};