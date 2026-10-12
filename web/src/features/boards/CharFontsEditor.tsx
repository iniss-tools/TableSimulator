// Pisma znakovej tabule LCD1 a ERP podla cisla pisma: INISS posiela pri kazdom znaku cislo pisma, co znamena, urcuje
// tabula - obsluha cislu priradi bodove pismo a ztucnenie. Inverzia (+64) a blikanie (+128) su v cisle.
import { useState } from "react";
import { Plus, Trash2 } from "lucide-react";
import type { CharFontDto } from "@/api";
import { CheckField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { Button } from "@/components/ui/button";
import { FieldDescription } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { PIXEL_FONTS } from "@/render/pixel-fonts.data";
import { CHAR_WIDTH } from "@/render/chars";
import { t } from "@/i18n";
import { charFontNumbers, MAX_FONT_NUMBER } from "./char-fonts";

/** Predvolene pismo bunky (najvacsie, ktore sa zmesti). */
const DEFAULT = "";

interface Props {
  fonts: Record<string, CharFontDto> | null;
  rowHeight: number;
  /** Cisla pisma, ktore tabula od INISSu dostala. */
  used: number[];
  onChange: (fonts: Record<string, CharFontDto> | null) => void;
}

export function CharFontsEditor({ fonts, rowHeight, used, onChange }: Props) {
  const [number, setNumber] = useState(16);
  const entries = charFontNumbers(fonts);
  // bodove pisma, ktore sa zmestia do bunky (sirka s bodom medzery, vyska riadku)
  const available = PIXEL_FONTS.filter((f) => f.width < CHAR_WIDTH && f.height <= rowHeight);
  const set = (key: number, font: CharFontDto | null) => {
    const next = { ...fonts };
    if (font) next[String(key)] = font;
    else delete next[String(key)];
    onChange(Object.keys(next).length ? next : null);
  };
  const missing = used.filter((n) => !entries.includes(n));

  return (
    <div className="flex flex-col gap-4">
      <FieldDescription>{t("board.charFonts.hint")}</FieldDescription>
      {used.length > 0 && <FieldDescription>{t("board.charFonts.used", { fonts: used.join(", ") })}</FieldDescription>}
      {entries.length === 0 ? (
        <p className="text-sm text-muted-foreground">{t("board.charFonts.none")}</p>
      ) : (
        <div className="grid grid-cols-[auto_minmax(0,11rem)_auto_auto] items-center gap-x-4 gap-y-2">
          {entries.map((key) => {
            const font = fonts![String(key)];
            return (
              <div key={key} className="contents">
                <span className="font-mono text-sm">{t("board.charFonts.number", { number: key })}</span>
                <OptionSelect
                  value={font.font}
                  onValueChange={(name) => set(key, { ...font, font: name })}
                  ariaLabel={t("board.charFonts.font", { number: key })}
                  className="w-full"
                  options={[
                    { value: DEFAULT, label: t("board.charFonts.default") },
                    ...available.map((f) => ({ value: f.name, label: `${f.width} × ${f.height}` })),
                  ]}
                />
                <CheckField label={t("board.charFonts.bold")} className="w-auto" checked={font.bold} onCheckedChange={(bold) => set(key, { ...font, bold })} />
                <Button
                  type="button"
                  variant="ghost"
                  size="icon-sm"
                  className="justify-self-end"
                  title={t("common.delete")}
                  aria-label={t("common.delete")}
                  onClick={() => set(key, null)}
                >
                  <Trash2 />
                </Button>
              </div>
            );
          })}
        </div>
      )}
      <div className="flex flex-wrap items-center gap-2">
        <Input
          type="number"
          min={0}
          max={MAX_FONT_NUMBER}
          value={number}
          aria-label={t("board.charFonts.newNumber")}
          className="w-24"
          onChange={(e) => setNumber(Math.min(MAX_FONT_NUMBER, Math.max(0, Number.parseInt(e.target.value, 10) || 0)))}
        />
        <Button type="button" variant="outline" disabled={entries.includes(number)} onClick={() => set(number, { font: DEFAULT, bold: false })}>
          <Plus />
          {t("board.charFonts.add")}
        </Button>
        {missing.length > 0 && (
          <Button
            type="button"
            variant="ghost"
            onClick={() => onChange({ ...fonts, ...Object.fromEntries(missing.map((n) => [String(n), { font: DEFAULT, bold: false }])) })}
          >
            {t("board.charFonts.addUsed")}
          </Button>
        )}
      </div>
    </div>
  );
}
