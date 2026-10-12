import { expect, test } from "bun:test";
import { shortcutAction } from "./actions";

const key = (key: string, code: string, mods: { ctrl?: boolean; shift?: boolean; alt?: boolean } = { ctrl: true }) => ({
  key,
  code,
  ctrlKey: mods.ctrl ?? false,
  metaKey: false,
  shiftKey: mods.shift ?? false,
  altKey: mods.alt ?? false,
});

test("skratky s Ctrl", () => {
  expect(shortcutAction(key("c", "KeyC"))).toBe("copy");
  expect(shortcutAction(key("V", "KeyV", { ctrl: true, shift: true }))).toBe("paste");
  expect(shortcutAction(key("z", "KeyY"))).toBe("undo");
  expect(shortcutAction(key("z", "KeyY", { ctrl: true, shift: true }))).toBe("redo");
  expect(shortcutAction(key("d", "KeyD"))).toBe("duplicate");
  expect(shortcutAction(key("c", "KeyC", {}))).toBeNull();
  expect(shortcutAction(key("c", "KeyC", { ctrl: true, alt: true }))).toBeNull();
});

test("poradie podla polohy klavesy, nie znaku (slovenska klavesnica)", () => {
  expect(shortcutAction(key("ä", "BracketRight"))).toBe("forward");
  expect(shortcutAction(key("ú", "BracketLeft"))).toBe("backward");
  expect(shortcutAction(key("Ä", "BracketRight", { ctrl: true, shift: true }))).toBe("front");
  expect(shortcutAction(key("Ú", "BracketLeft", { ctrl: true, shift: true }))).toBe("back");
});
