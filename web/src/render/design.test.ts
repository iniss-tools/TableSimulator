import { describe, expect, test } from "bun:test";
import { completeDesign, DEFAULT_DESIGN, DEFAULT_FLAP_DESIGN, DEFAULT_FLAPS, defaultDesignId, designForFamily, faceStyle } from "./design";
import { shade } from "./surface";

describe("vzhlad", () => {
  test("starsi subor bez novych casti dostane predvolene (vzhlad sa nezmeni)", () => {
    const design = completeDesign({
      name: "Stary",
      pitch: 4,
      frame: { width: 6, color: "#222222", radius: 2 } as never,
      header: { color: "#ffff00" } as never,
    });
    expect(design.renderer).toBe("led-matrix");
    expect(design.plate).toBeNull();
    expect(design.frame).toEqual({ width: 6, color: "#222222", radius: 2, style: "flat", badge: "" });
    expect(design.seam.color).toBeNull();
    expect(design.header.color).toBe("#ffff00");
    expect(design.header.font).toBe(DEFAULT_DESIGN.header.font);
    expect(design.flaps).toBeNull();
    // plech bez farby = pozadie
    expect(faceStyle(design).colors.plate).toBe(design.background);
    expect(faceStyle(design).colors.panel).toBe(design.header.background);
  });

  test("listovy vzhlad dopln farby listov", () => {
    const design = completeDesign({ renderer: "flaps", flaps: { ...DEFAULT_FLAPS, colors: { red: { top: "#ff0000", bottom: "#ee0000", text: "#ffffff" } } } });
    expect(design.flaps!.colors.red.top).toBe("#ff0000");
    expect(design.flaps!.colors.black).toEqual(DEFAULT_FLAPS.colors.black);
    expect(design.background).toBe(DEFAULT_FLAP_DESIGN.background);
  });

  test("vzhlad podla technologie tabule", () => {
    expect(defaultDesignId("flaps", "ERS")).toBe("listova");
    expect(defaultDesignId("chars", "LCD1")).toBe("lcd-dark");
    expect(designForFamily(DEFAULT_DESIGN, "flaps")).toBe(DEFAULT_FLAP_DESIGN);
    expect(designForFamily(DEFAULT_FLAP_DESIGN, "elen")).toBe(DEFAULT_DESIGN);
    expect(designForFamily(DEFAULT_DESIGN, "chars")).toBe(DEFAULT_DESIGN);
  });

  test("odtien farby ramu", () => {
    expect(shade("#808080", 0.5)).toBe("#c0c0c0");
    expect(shade("#808080", -0.5)).toBe("#404040");
    expect(shade("cierna", 0.5)).toBe("cierna");
  });
});
