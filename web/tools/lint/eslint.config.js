// ESLint pre web simulatora. Vlastny balik s TypeScript 6 - typescript-eslint potrebuje JS API kompilatora, ktore
// TypeScript 7 (web/package.json) nema. Spusta sa z web/: bun run lint.
import js from "@eslint/js";
import prettier from "eslint-config-prettier";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";
import globals from "globals";
import tseslint from "typescript-eslint";

const web = new URL("../..", import.meta.url).pathname.replace(/^\/(\w:)/, "$1");

export default tseslint.config(
  { basePath: web, ignores: ["src/api/generated/**", "tools/**", "node_modules/**"] },
  {
    basePath: web,
    files: ["src/**/*.{ts,tsx}"],
    extends: [js.configs.recommended, tseslint.configs.recommendedTypeChecked, reactHooks.configs.flat.recommended, reactRefresh.configs.vite, prettier],
    languageOptions: {
      ecmaVersion: 2022,
      globals: globals.browser,
      parserOptions: { projectService: true, tsconfigRootDir: web },
    },
    rules: {
      // obsluha udalosti v JSX moze byt async (chyby riesi sama)
      "@typescript-eslint/no-misused-promises": ["error", { checksVoidReturn: { attributes: false } }],
      "@typescript-eslint/no-unused-vars": ["error", { argsIgnorePattern: "^_", varsIgnorePattern: "^_" }],
      // hlasi len to, co nevie React Compiler - ten sa nepouziva
      "react-hooks/incompatible-library": "off",
    },
  },
  {
    // komponenty shadcn/ui exportuju aj varianty (cva); smerovac nie je modul komponentu
    basePath: web,
    files: ["src/components/ui/**", "src/app/router.tsx"],
    rules: { "react-refresh/only-export-components": "off" },
  },
);
