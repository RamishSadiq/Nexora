import { Sparkles } from "lucide-react";

type NexoraMarkProps = { inverse?: boolean; compact?: boolean };

export function NexoraMark({ inverse = false, compact = false }: NexoraMarkProps) {
  return (
    <div className="inline-flex items-center gap-3" aria-label="Nexora">
      <span className={`grid size-10 place-items-center rounded-[13px] shadow-lg ${inverse ? "bg-white text-[#263a7a] shadow-black/15" : "bg-[#233b81] text-white shadow-[#233b81]/20"}`}>
        <Sparkles className="size-[21px]" strokeWidth={2.2} aria-hidden="true" />
      </span>
      {!compact && <span className={`text-[1.35rem] font-semibold tracking-[-0.045em] ${inverse ? "text-white" : "text-[#17213a]"}`}>nexora</span>}
    </div>
  );
}
