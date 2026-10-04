// Body tabule v jednom okamihu - spolocny vystup rasterizacie ELEN aj znakovych tabul.

/** Farba bodu: 0 nesvieti, 1 cervena, 2 zelena, 3 zlta, 4 pismo bez farby. */
export const COLOR_NONE = 4;

export interface DotFrame {
  width: number;
  height: number;
  /** Farba kazdeho bodu po riadkoch. */
  color: Uint8Array;
  /** 1 = bod blika. */
  blink: Uint8Array;
}
