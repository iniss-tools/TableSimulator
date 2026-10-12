// Prehlad klaves a mysi v editore ciel (tlacidlo v nastrojoch alebo klaves ?).
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Kbd, KbdGroup } from "@/components/ui/kbd";
import { t, type TextKey } from "@/i18n";

/** Slova v skratkach, ktore sa prekladaju (ostatne su nazvy klaves). */
const WORDS: Record<string, TextKey> = {
  click: "faces.keys.click",
  dblclick: "faces.keys.dblclick",
  drag: "faces.keys.drag",
  empty: "faces.keys.empty",
  label: "faces.keys.label",
  wheel: "faces.keys.wheel",
  space: "faces.keys.space",
  hover: "faces.keys.hover",
  arrows: "faces.keys.arrows",
};

/** Skupiny: nadpis a riadky - kombinacie klaves (alternativy) a co robia. */
const GROUPS: { title: TextKey; rows: { keys: string[][]; text: TextKey }[] }[] = [
  {
    title: "faces.keys.select",
    rows: [
      { keys: [["click"]], text: "faces.keys.select.one" },
      {
        keys: [
          ["Ctrl", "click"],
          ["Shift", "click"],
        ],
        text: "faces.keys.select.add",
      },
      { keys: [["drag", "empty"]], text: "faces.keys.select.marquee" },
      { keys: [["Ctrl", "A"]], text: "faces.keys.select.all" },
      { keys: [["Esc"]], text: "faces.keys.select.none" },
    ],
  },
  {
    title: "faces.keys.edit",
    rows: [
      { keys: [["arrows"]], text: "faces.keys.edit.arrows" },
      {
        keys: [
          ["Shift", "arrows"],
          ["Alt", "arrows"],
        ],
        text: "faces.keys.edit.arrowsMod",
      },
      { keys: [["Alt", "drag"]], text: "faces.keys.edit.noSnap" },
      { keys: [["drag", "label"]], text: "faces.keys.edit.scrub" },
      { keys: [["dblclick"]], text: "faces.keys.edit.text" },
      {
        keys: [
          ["Ctrl", "C"],
          ["Ctrl", "X"],
          ["Ctrl", "V"],
        ],
        text: "faces.keys.edit.clipboard",
      },
      { keys: [["Ctrl", "D"]], text: "faces.keys.edit.duplicate" },
      { keys: [["Del"]], text: "faces.keys.edit.remove" },
    ],
  },
  {
    title: "faces.keys.arrange",
    rows: [
      {
        keys: [
          ["Ctrl", "]"],
          ["Ctrl", "["],
        ],
        text: "faces.keys.arrange.step",
      },
      {
        keys: [
          ["Ctrl", "Shift", "]"],
          ["Ctrl", "Shift", "["],
        ],
        text: "faces.keys.arrange.end",
      },
    ],
  },
  {
    title: "faces.keys.view",
    rows: [
      { keys: [["Alt", "hover"]], text: "faces.keys.view.measure" },
      { keys: [["Ctrl", "wheel"]], text: "faces.keys.view.zoom" },
      { keys: [["space", "drag"]], text: "faces.keys.view.pan" },
    ],
  },
  {
    title: "faces.keys.history",
    rows: [
      { keys: [["Ctrl", "Z"]], text: "faces.keys.history.undo" },
      {
        keys: [
          ["Ctrl", "Y"],
          ["Ctrl", "Shift", "Z"],
        ],
        text: "faces.keys.history.redo",
      },
      { keys: [["Ctrl", "S"]], text: "faces.keys.history.save" },
    ],
  },
];

export function ShortcutsDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  return (
    <Dialog open={open} onOpenChange={(value) => !value && onClose()}>
      <DialogContent className="max-h-[85dvh] overflow-y-auto sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>{t("faces.keys.title")}</DialogTitle>
          <DialogDescription>{t("faces.keys.hint")}</DialogDescription>
        </DialogHeader>
        <div className="gap-8 sm:columns-2">
          {GROUPS.map((group) => (
            <section key={group.title} className="mb-4 flex break-inside-avoid flex-col gap-1.5">
              <h3 className="text-xs font-medium text-muted-foreground">{t(group.title)}</h3>
              <dl className="flex flex-col gap-1.5">
                {group.rows.map((row) => (
                  <div key={row.text} className="flex items-start justify-between gap-3 text-sm">
                    <dt className="min-w-0">{t(row.text)}</dt>
                    <dd className="flex max-w-[55%] shrink-0 flex-wrap justify-end gap-x-2 gap-y-1">
                      {row.keys.map((combo) => (
                        <KbdGroup key={combo.join("+")}>
                          {combo.map((key) => (
                            <Kbd key={key}>{key in WORDS ? t(WORDS[key]) : key}</Kbd>
                          ))}
                        </KbdGroup>
                      ))}
                    </dd>
                  </div>
                ))}
              </dl>
            </section>
          ))}
        </div>
      </DialogContent>
    </Dialog>
  );
}
