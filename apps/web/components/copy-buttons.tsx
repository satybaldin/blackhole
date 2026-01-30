"use client";

import { Button } from "@/components/ui/button";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { Copy } from "lucide-react";
import { toast } from "sonner";

export function CopyButtons(props: {
  subject: string;
  mailFrom: string;
  rcptTo: string;
  raw: string;
}) {
  async function copy(label: string, text: string) {
    await navigator.clipboard.writeText(text);
    toast.success(`Copied ${label}`);
  }

  return (
    <div className="flex flex-wrap gap-2">
      <Tooltip>
        <TooltipTrigger asChild>
          <Button
            variant="outline"
            size="sm"
            className="cursor-pointer"
            onClick={() => copy("subject", props.subject)}
          >
            <Copy className="mr-2 h-4 w-4" />
            Subject
          </Button>
        </TooltipTrigger>
        <TooltipContent>Скопировать тему</TooltipContent>
      </Tooltip>

      <Tooltip>
        <TooltipTrigger asChild>
          <Button
            variant="outline"
            size="sm"
            onClick={() => copy("mail from", props.mailFrom)}
          >
            <Copy className="mr-2 h-4 w-4" />
            From
          </Button>
        </TooltipTrigger>
        <TooltipContent>Скопировать MAIL FROM</TooltipContent>
      </Tooltip>

      <Tooltip>
        <TooltipTrigger asChild>
          <Button
            variant="outline"
            size="sm"
            onClick={() => copy("rcpt to", props.rcptTo)}
          >
            <Copy className="mr-2 h-4 w-4" />
            Rcpt
          </Button>
        </TooltipTrigger>
        <TooltipContent>Скопировать RCPT TO</TooltipContent>
      </Tooltip>

      <Tooltip>
        <TooltipTrigger asChild>
          <Button
            variant="secondary"
            size="sm"
            className="border-border"
            onClick={() => copy("raw", props.raw)}
          >
            <Copy className="mr-2 h-4 w-4" />
            Raw
          </Button>
        </TooltipTrigger>
        <TooltipContent>Скопировать raw письмо</TooltipContent>
      </Tooltip>
    </div>
  );
}
