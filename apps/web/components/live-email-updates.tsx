"use client";

import { useEffect } from "react";
import {
  HubConnectionBuilder,
  HttpTransportType,
  LogLevel,
} from "@microsoft/signalr";
import { useRouter } from "next/navigation";

export function LiveEmailUpdates() {
  const router = useRouter();

  useEffect(() => {
    const base = process.env.NEXT_PUBLIC_API_BASE!;
    const url = `${base.replace(/\/$/, "")}/hubs/events`;

    const conn = new HubConnectionBuilder()
      .withUrl(url, {
        transport: HttpTransportType.WebSockets,
        skipNegotiation: true,
        logMessageContent: false
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.None)
      .build();

    conn.on("email_received", () => router.refresh());

    conn.start().catch((err) => console.error("SignalR start error:", err));

    return () => {
      conn.stop().catch(() => {});
    };
  }, [router]);

  return null;
}
