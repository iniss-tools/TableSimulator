// Servery testov: bez prihlasovania (localhost = spravca) a s prihlasovanim (prvy spravca, prihlasenie).
export const E2E_SERVERS = {
  open: { name: "open", port: 5491, url: "http://localhost:5491" },
  login: { name: "login", port: 5492, url: "http://localhost:5492" },
} as const;
