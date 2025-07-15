"use client";

import { initializeMsal, msalInstance } from "@/lib/msal";
import { AuthenticatedTemplate, MsalProvider, UnauthenticatedTemplate } from "@azure/msal-react";
import { ReactNode, useEffect, useState } from "react";
import { LoadingSpinner } from "../LoadingSpinner";
import { UnauthorizedMessage } from "./UnauthorizedMessage";

interface MsalWrapperProps {
  children: ReactNode;
}

export function MsalWrapper({ children }: MsalWrapperProps) {
  const [isInitialized, setIsInitialized] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    setHydrated(true); // Ensure hydration is complete before updating state
    async function initialize() {
      try {
        await initializeMsal();
        setIsInitialized(true);
      } catch (error) {
        console.error("Error initializing MSAL:", error);
        setError("Failed to initialize authentication. Please try again later.");
      }
    }
    initialize();
  }, []);

  if (!hydrated) {
    return null; // Render nothing until hydration is complete
  }

  if (error) {
    return (
      <div className="fixed inset-0 flex items-center justify-center bg-gray-100 dark:bg-gray-900">
        <p className="text-red-500">
          {error || "An error occurred. Please try again later."}
        </p>
      </div>
    );
  }

  if (!isInitialized) {
    return <LoadingSpinner />;
  }

  return (
    <MsalProvider instance={msalInstance}>
      <AuthenticatedTemplate>{children}</AuthenticatedTemplate>
      <UnauthenticatedTemplate>
        <UnauthorizedMessage />
      </UnauthenticatedTemplate>
    </MsalProvider>
  );
}