// Jedna sprava informacneho panelu na celu plochu rodica (panel aj nahlad) - velkost textu podla sirky plochy.
import { useQuery } from "@tanstack/react-query";
import { errorText, getVirtualViewOptions, useSimulatorState, type SetSignageMessageRequest, type SignageAlign, type SignageTextSize } from "@/api";
import { BoardView } from "@/features/boards/BoardView";
import { VirtualScreen } from "@/features/virtual/VirtualScreen";
import { useNow } from "@/hooks/use-now";
import { t } from "@/i18n";
import { cn } from "@/lib/utils";
import { mediaUrl } from "./signage";

// ako male/stredne/velke pismo INISSView 2 (50/70/100 px na sirke 1920 px)
const SIZES: Record<SignageTextSize, string> = { small: "text-[2.6cqw]", medium: "text-[3.65cqw]", large: "text-[5.2cqw]" };
const ALIGN: Record<SignageAlign, string> = { left: "text-left", center: "text-center", right: "text-right" };

function Problem({ text }: { text: string }) {
  return <div className="flex size-full items-center justify-center p-[2cqw] text-center text-[2cqw] text-neutral-400">{text}</div>;
}

function VirtualSlide({ id }: { id: number }) {
  // obsah sa obnovuje so zmenou XML (live.ts)
  const query = useQuery(getVirtualViewOptions({ path: { id } }));
  const now = useNow(250);
  if (!query.data) return query.error ? <Problem text={errorText(query.error)} /> : null;
  return <VirtualScreen view={query.data} layout={query.data.layout} now={now} />;
}

function BoardSlide({ id }: { id: string }) {
  const board = useSimulatorState().boards.find((b) => b.id === id);
  if (!board) return <Problem text={t("signage.slide.noBoard", { id })} />;
  return <BoardView board={board} fit="contain" className="h-full p-[2cqw]" />;
}

export function SignageSlide({ message }: { message: SetSignageMessageRequest }) {
  const content = () => {
    switch (message.kind) {
      case "virtual":
        return message.virtualBoard !== null ? <VirtualSlide id={message.virtualBoard} /> : null;
      case "board":
        return message.board ? <BoardSlide id={message.board} /> : null;
      case "image":
        return message.media && <img src={mediaUrl(message.media)} alt="" className="size-full object-contain" />;
      case "video":
        // bez zvuku - prehliadac inak video sam nespusti
        return message.media && <video src={mediaUrl(message.media)} autoPlay muted loop playsInline className="size-full object-contain" />;
      case "web":
        return message.url && <iframe src={message.url} title={message.name} referrerPolicy="no-referrer" className="size-full border-0 bg-white" />;
      case "text":
        return (
          <div className="mx-[2%] w-[96%] py-[1cqw] [font-family:Verdana,Geneva,sans-serif] leading-tight whitespace-pre-line">
            {message.paragraphs.map((p, i) => (
              <p key={i} className={cn("my-[0.6em]", SIZES[p.size], ALIGN[p.align])} style={{ color: p.color }}>
                {p.text}
              </p>
            ))}
          </div>
        );
    }
  };
  return (
    <div className="@container relative size-full overflow-hidden" style={{ background: message.background }}>
      {content()}
    </div>
  );
}
