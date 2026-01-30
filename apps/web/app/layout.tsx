import type { Metadata } from "next";
import { Geist, Geist_Mono, Inter } from "next/font/google";
import "./globals.css";

const inter = Inter({ subsets: ["latin"], variable: "--font-sans" });

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "Blackbird",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" className={inter.variable}>
      <body
        className={`${geistSans.variable} ${geistMono.variable} h-dvh w-dvw flex flex-col antialiased`}
      >
        <div className="h-12 shrink-0 border-b flex px-4 items-center">
          <p className="text-lg font-medium">Blackhole</p>
        </div>

        <div className="flex flex-1 min-h-0 w-full">
          <div className="h-full border-r w-12 flex flex-col shrink-0" />
          <div className="flex-1 min-h-0 overflow-auto">{children}</div>
        </div>
      </body>
    </html>
  );
}
