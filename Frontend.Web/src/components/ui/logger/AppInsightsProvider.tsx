"use client";

import { ClickAnalyticsPlugin } from "@microsoft/applicationinsights-clickanalytics-js";
import { AppInsightsContext, ReactPlugin } from "@microsoft/applicationinsights-react-js";
import { ApplicationInsights } from "@microsoft/applicationinsights-web";
import { ReactNode, useEffect, useMemo } from "react";

export function AppInsightsProvider({ children }: { children: ReactNode }) {
  const reactPlugin = useMemo(() => new ReactPlugin(), []);
  useEffect(() => {
    const clickPluginInstance = new ClickAnalyticsPlugin();
    const clickPluginConfig = {
      autoCapture: true,
    };
    const appInsights = new ApplicationInsights({
      config: {
        connectionString: process.env.NEXT_PUBLIC_APPINSIGHTS_CONNECTION_STRING,
        extensions: [reactPlugin, clickPluginInstance],
        extensionConfig: {
          [reactPlugin.identifier]: {},
          [clickPluginInstance.identifier]: clickPluginConfig,
        },
      },
    });
    appInsights.loadAppInsights();

    return () => {
      appInsights.unload();
    };
  }, [reactPlugin]);

  return <AppInsightsContext.Provider value={reactPlugin}>{children}</AppInsightsContext.Provider>;
}