// Kiosk spusteny z plochy (manifest kiosku): adresa /?view=/virtual/3 sa zmeni na #/virtual/3 skor, nez ju precita
// smerovac - start_url s # niektore prehliadace zahadzuju.
try {
  const url = new URL(location.href);
  const view = url.searchParams.get("view");
  if (view?.startsWith("/")) {
    url.searchParams.delete("view");
    url.hash = view;
    history.replaceState(null, "", url.toString());
  }
} catch {
  // adresu nechat tak
}
