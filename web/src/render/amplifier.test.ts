import { expect, test } from "bun:test";
import { amplifierBits } from "./amplifier";

test("bity stavu podla mien z logu INISSu", () => {
  expect(amplifierBits(0x90)).toEqual(["Ready", "Hlaseni"]);
  expect(amplifierBits(0xa4)).toEqual(["Ready", "TransferErr", "HwErr"]);
  expect(amplifierBits(0)).toEqual([]);
});
