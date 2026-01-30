import { LiveEmailUpdates } from "@/components/live-email-updates";

type EmailListItem = {
  id: string;
  receivedAtUtc: string;
  mailFrom: string | null;
  rcptTo: string[];
  subject: string | null;
  from: string | null;
  to: string | null;
};

export default async function Home() {
  const base = process.env.NEXT_PUBLIC_API_BASE!;
  const res = await fetch(`${base}/api/emails?take=50`, { cache: "no-store" });
  const emails: EmailListItem[] = await res.json();

  return (
    <main className="p-4 max-w-300 w-full mx-auto">
      <LiveEmailUpdates />

      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-2xl font-medium">SMTP Inbox</h1>
        <div className="text-sm text-zinc-500">{emails.length} message(s)</div>
      </div>

      <div className="rounded-xl border overflow-clip">
        <div className="grid grid-cols-12 gap-2 border-b bg-zinc-50 px-3 py-2 text-xs font-medium text-zinc-600">
          <div className="col-span-2">Received</div>
          <div className="col-span-3">From</div>
          <div className="col-span-3">To</div>
          <div className="col-span-4">Subject</div>
        </div>

        {emails.map((e) => (
          <a
            key={e.id}
            href={`/emails/${e.id}`}
            className="grid grid-cols-12 gap-2 px-3 py-2 text-sm hover:bg-zinc-50"
          >
            <div className="col-span-2 text-zinc-600">
              {new Date(e.receivedAtUtc).toLocaleString()}
            </div>
            <div className="col-span-3 font-mono">{e.mailFrom ?? "-"}</div>
            <div className="col-span-3 font-mono">{e.rcptTo.join(", ")}</div>
            <div className="col-span-4 truncate">
              {e.subject ?? "(no subject)"}
            </div>
          </a>
        ))}

        {emails.length === 0 && (
          <div className="p-6 text-sm text-zinc-500">No emails yet.</div>
        )}
      </div>
    </main>
  );
}
