// Ulozenie suboru vytvoreneho v prehliadaci (napr. snimka tabule) cez docasny odkaz.
export function saveFile(blob: Blob, name: string) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = name;
  link.click();
  // prehliadac subor uz prevzal
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
