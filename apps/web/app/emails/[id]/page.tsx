import Link from "next/link";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { ScrollArea } from "@/components/ui/scroll-area";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator as DmSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";

import {
  ArrowLeft,
  CalendarClock,
  FileText,
  Globe,
  Mail,
  MoreHorizontal,
  Shield,
  TerminalSquare,
  User,
} from "lucide-react";
import { CopyButtons } from "@/components/copy-buttons";

type Email = {
  id: string;
  receivedAtUtc: string;
  helo: string | null;
  mailFrom: string | null;
  rcptTo: { address: string }[];
  subject: string | null;
  headerFrom: string | null;
  headerTo: string | null;
  textBody: string | null;
  htmlBody: string | null;
};

function fmtUtc(utcIso: string | null | undefined) {
  if (!utcIso) return "-";
  const d = new Date(utcIso);
  // если хочешь локальную — поменяй на undefined/ru-RU
  return (
    new Intl.DateTimeFormat("ru-RU", {
      dateStyle: "medium",
      timeStyle: "medium",
      timeZone: "UTC",
    }).format(d) + " UTC"
  );
}

function shortEmail(s: string | null | undefined) {
  if (!s) return "-";
  if (s.length <= 44) return s;
  return s.slice(0, 20) + "…" + s.slice(-20);
}

// Server component (Next.js app router)
export default async function EmailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;

  const base = process.env.NEXT_PUBLIC_API_BASE!;
  const res = await fetch(`${base}/api/emails/${id}`, { cache: "no-store" });

  if (!res.ok) {
    return (
      <main className="p-6">
        <Alert>
          <AlertTitle>Not found</AlertTitle>
          <AlertDescription>
            Письмо с id: <span className="font-mono">{id}</span> не найдено.
          </AlertDescription>
        </Alert>
        <div className="mt-4">
          <Button asChild variant="secondary">
            <Link href="/emails">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to inbox
            </Link>
          </Button>
        </div>
      </main>
    );
  }

  const e: Email = await res.json();

  const rawRes = await fetch(`${base}/api/emails/${id}/raw`, {
    cache: "no-store",
  });
  const raw = rawRes.ok ? await rawRes.text() : "";

  const rcpt = e.rcptTo?.map((x) => x.address).filter(Boolean) ?? [];
  const subject = e.subject ?? "(no subject)";

  // простая эвристика статусов/флагов
  const hasHtml = Boolean(e.htmlBody?.trim());
  const hasText = Boolean(e.textBody?.trim());
  const hasRaw = Boolean(raw?.trim());

  return (
    <TooltipProvider>
      <main className="mx-auto w-full max-w-6xl p-4 md:p-6">
        {/* Top bar */}
        <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
          <div className="flex items-start gap-3">
            <Button asChild variant="ghost" size="icon" className="mt-0.5">
              <Link href="/" aria-label="Back">
                <ArrowLeft className="h-5 w-5" />
              </Link>
            </Button>

            <div className="space-y-1">
              <Breadcrumb>
                <BreadcrumbList>
                  <BreadcrumbItem>
                    <BreadcrumbLink asChild>
                      <Link href="/">Emails</Link>
                    </BreadcrumbLink>
                  </BreadcrumbItem>
                  <BreadcrumbSeparator />
                  <BreadcrumbItem>
                    <span className="font-mono text-muted-foreground">
                      {id}
                    </span>
                  </BreadcrumbItem>
                </BreadcrumbList>
              </Breadcrumb>

              <div className="flex flex-wrap items-center gap-2">
                <h1 className="text-xl font-semibold leading-tight md:text-2xl">
                  {subject}
                </h1>

                <div className="flex items-center gap-2">
                  {hasText && <Badge variant="secondary">text</Badge>}
                  {hasHtml && <Badge variant="secondary">html</Badge>}
                  {hasRaw && <Badge variant="outline">raw</Badge>}
                </div>
              </div>

              <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
                <span className="inline-flex items-center gap-1">
                  <CalendarClock className="h-4 w-4" />
                  {fmtUtc(e.receivedAtUtc)}
                </span>
                <Separator orientation="vertical" className="h-4" />
                <span className="inline-flex items-center gap-1">
                  <TerminalSquare className="h-4 w-4" />
                  HELO: <span className="font-mono">{e.helo ?? "-"}</span>
                </span>
              </div>
            </div>
          </div>

          <div className="flex flex-wrap items-center justify-start gap-2 md:justify-end">
            {/* Copy actions */}
            <Tooltip>
              <TooltipTrigger asChild>
                <Button variant="secondary" size="icon-sm" className="hidden">
                  Copy
                </Button>
              </TooltipTrigger>
              <TooltipContent>
                Кнопки копирования реализуй клиентским компонентом (ниже
                пример).
              </TooltipContent>
            </Tooltip>

            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="outline" size="sm">
                  <MoreHorizontal className="mr-2 h-4 w-4" />
                  Actions
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-56">
                <DropdownMenuLabel>Quick actions</DropdownMenuLabel>
                <DropdownMenuItem asChild>
                  <a
                    href={`${base}/api/emails/${id}/raw`}
                    target="_blank"
                    rel="noreferrer"
                  >
                    <FileText className="mr-2 h-4 w-4" />
                    Open raw in new tab
                  </a>
                </DropdownMenuItem>
                <DmSeparator />
                <DropdownMenuItem asChild>
                  <a
                    href={`${base}/api/emails/${id}`}
                    target="_blank"
                    rel="noreferrer"
                  >
                    <Globe className="mr-2 h-4 w-4" />
                    Open JSON
                  </a>
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>

        <Separator className="my-4" />

        {/* Metadata card */}
        <Card className="gap-0 ring-0 p-0 rounded-none">
          <CardHeader className="mb-4">
            <div className="flex flex-col  md:flex-row md:items-center md:justify-between">
              <div className="flex items-center gap-3">
                <Avatar className="h-9 w-9">
                  <AvatarFallback>
                    <Mail className="h-4 w-4" />
                  </AvatarFallback>
                </Avatar>

                <div>
                  <div className="text-sm font-medium">Envelope</div>
                  <div className="text-xs text-muted-foreground">
                    SMTP-level fields and parsed headers
                  </div>
                </div>
              </div>

              <div className="flex flex-wrap gap-2">
                <Badge variant="outline" className="font-mono">
                  id: {shortEmail(e.id)}
                </Badge>
                <Badge variant="outline" className="font-mono">
                  rcpt: {rcpt.length}
                </Badge>
              </div>
            </div>
          </CardHeader>

          <CardContent className="p-0">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="rounded-xl border p-4">
                <div className="mb-2 flex items-center gap-2 text-sm font-medium">
                  <User className="h-4 w-4 text-muted-foreground" />
                  SMTP
                </div>

                <div className="grid gap-2 text-sm">
                  <div className="flex items-start justify-between gap-3">
                    <div className="text-muted-foreground">MAIL FROM</div>
                    <div className="font-mono text-right">
                      {e.mailFrom ?? "-"}
                    </div>
                  </div>

                  <div className="flex items-start justify-between gap-3">
                    <div className="text-muted-foreground">RCPT TO</div>
                    <div className="font-mono text-right">
                      {rcpt.length ? rcpt.join(", ") : "-"}
                    </div>
                  </div>

                  <Separator className="my-1" />

                  <div className="flex items-start justify-between gap-3">
                    <div className="text-muted-foreground">HELO</div>
                    <div className="font-mono text-right">{e.helo ?? "-"}</div>
                  </div>
                </div>
              </div>

              <div className="rounded-xl border p-4">
                <div className="mb-2 flex items-center gap-2 text-sm font-medium">
                  <Shield className="h-4 w-4 text-muted-foreground" />
                  Headers
                </div>

                <div className="grid gap-2 text-sm">
                  <div className="flex items-start justify-between gap-3">
                    <div className="text-muted-foreground">Header From</div>
                    <div className="text-right">{e.headerFrom ?? "-"}</div>
                  </div>

                  <div className="flex items-start justify-between gap-3">
                    <div className="text-muted-foreground">Header To</div>
                    <div className="text-right">{e.headerTo ?? "-"}</div>
                  </div>

                  <Separator className="my-1" />

                  <div className="flex items-start justify-between gap-3">
                    <div className="text-muted-foreground">Subject</div>
                    <div className="text-right">{subject}</div>
                  </div>
                </div>
              </div>
            </div>

            <div className="mt-4 flex flex-wrap gap-2">
              {/* Кнопки копирования лучше вынести в client component.
                  Ниже оставляю "слоты", чтобы ты быстро вставил. */}
              <CopyButtons
                subject={subject}
                mailFrom={e.mailFrom ?? ""}
                rcptTo={rcpt.join(", ")}
                raw={raw}
              />
            </div>
          </CardContent>
        </Card>

        {/* Content */}
        <div className="mt-6">
          <Tabs
            defaultValue={hasText ? "text" : hasHtml ? "html" : "raw"}
            className="w-full"
          >
            <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
              <TabsList className="w-70">
                <TabsTrigger value="text" disabled={!hasText}>
                  Text
                </TabsTrigger>
                <TabsTrigger value="html" disabled={!hasHtml}>
                  HTML
                </TabsTrigger>
                <TabsTrigger value="raw" disabled={!hasRaw}>
                  Raw
                </TabsTrigger>
              </TabsList>

              <div className="flex items-center gap-2">
                <Badge variant="secondary" className="hidden md:inline-flex">
                  Preview
                </Badge>
                <span className="text-xs text-muted-foreground">
                  Tabs + sandboxed iframe + scrollable raw
                </span>
              </div>
            </div>

            <TabsContent value="text">
              <Card className="gap-0 p-0">
                <CardHeader className="p-2 px-4">
                  <div className="text-md font-medium">Body (text/plain)</div>
                </CardHeader>
                <CardContent className="p-0 border-t">
                  <pre className="whitespace-pre-wrap p-4 text-sm leading-relaxed">
                    {e.textBody ?? ""}
                  </pre>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="html">
              <Card className="gap-0 p-0">
                <CardHeader className="p-2 px-4">
                  <div className="text-md font-medium">Body (text/html)</div>
                </CardHeader>
                <CardContent className="p-0 border-t ">
                  <div className="overflow-hidden">
                    <iframe
                      className="h-full w-full"
                      sandbox=""
                      srcDoc={e.htmlBody ?? ""}
                      title="HTML email preview"
                    />
                  </div>
                  <div className="p-2 border-t bg-accent">
                    <Collapsible>
                      <div className="flex items-center justify-between">
                        <div className="text-sm text-muted-foreground">
                          Show HTML source
                        </div>
                        <CollapsibleTrigger asChild>
                          <Button variant="outline" size="sm">
                            Toggle
                          </Button>
                        </CollapsibleTrigger>
                      </div>
                      <CollapsibleContent className="mt-4">
                        <pre className="whitespace-pre-wrap  text-xs text-muted-foreground">
                          {e.htmlBody ?? ""}
                        </pre>
                      </CollapsibleContent>
                    </Collapsible>
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="raw">
              <Card className="gap-0 p-0 ">
                <CardHeader className="p-2 px-4">
                  <div className="text-md font-medium">Raw</div>
                </CardHeader>
                <CardContent className="p-0 border-t">
                  <pre className="whitespace-pre-wrap p-2.5 pt-2 font-mono text-xs text-muted-foreground">
                    {raw}
                  </pre>
                </CardContent>
              </Card>
            </TabsContent>
          </Tabs>
        </div>
      </main>
    </TooltipProvider>
  );
}
