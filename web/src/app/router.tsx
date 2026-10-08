// Stranky a ich adresy (TanStack Router s adresami za #): ram s menu, kiosky bez ovladania a prihlasenie pred vsetkym.
// Adresy su rovnake ako pred prestavbou - odkazy zariadeni (?key=…#/virtual/3) a ulozene zalozky ostavaju platne.
import { lazy, useEffect } from "react";
import { createHashHistory, createRootRoute, createRoute, createRouter, lazyRouteComponent, Outlet, redirect } from "@tanstack/react-router";
import { openSocket, useMe } from "@/api";
import { t, useLanguage } from "@/i18n";
import { NotFound } from "./NotFound";

// stranky sa nacitaju az pri prvom zobrazeni - kiosk (televizor, tablet) nestahuje ovladanie a naopak
const AuthPage = lazy(() => import("@/features/auth/AuthPage").then((m) => ({ default: m.AuthPage })));
const BoardKiosk = lazy(() => import("./BoardKiosk").then((m) => ({ default: m.BoardKiosk })));
const VirtualKiosk = lazy(() => import("@/features/virtual/VirtualKiosk").then((m) => ({ default: m.VirtualKiosk })));
const ArchivePage = lazy(() => import("@/features/archive/ArchivePage").then((m) => ({ default: m.ArchivePage })));

/** Kym sa nezisti pouzivatel, nic; neprihlaseny dostane prihlasenie (aj na adrese kiosku - po prihlaseni tam ostane). */
function Root() {
  const language = useLanguage();
  const me = useMe();
  const authenticated = me?.authenticated ?? false;
  useEffect(() => (authenticated ? openSocket() : undefined), [authenticated]);
  useEffect(() => {
    document.title = t("app.title");
  }, [language]);

  if (!me) return null;
  if (me.loginRequired && !me.authenticated) return <AuthPage setup={me.setupRequired} />;
  // zmena jazyka nakresli stranky znova (texty sa beru pri kresleni)
  return <Outlet key={language} />;
}

const rootRoute = createRootRoute({ component: Root });

const boardKiosk = createRoute({
  getParentRoute: () => rootRoute,
  path: "/view/$boardId",
  component: function BoardKioskRoute() {
    const { boardId } = boardKiosk.useParams();
    return <BoardKiosk boardId={boardId} />;
  },
});

const wall = createRoute({
  getParentRoute: () => rootRoute,
  path: "/wall",
  component: () => <BoardKiosk boardId={null} />,
});

const virtualKiosk = createRoute({
  getParentRoute: () => rootRoute,
  path: "/virtual/$id",
  component: function VirtualKioskRoute() {
    const { id } = virtualKiosk.useParams();
    return <VirtualKiosk id={Number(id)} />;
  },
});

const shell = createRoute({
  getParentRoute: () => rootRoute,
  id: "shell",
  component: lazyRouteComponent(() => import("./AppShell"), "AppShell"),
  notFoundComponent: () => t("nav.notFound"),
});

const pages = [
  createRoute({ getParentRoute: () => shell, path: "/", component: lazyRouteComponent(() => import("@/features/boards/BoardsPage"), "BoardsPage") }),
  // povodna adresa stranky tabul
  createRoute({
    getParentRoute: () => shell,
    path: "/boards",
    beforeLoad: () => {
      // eslint-disable-next-line @typescript-eslint/only-throw-error -- presmerovanie TanStack Router sa vyhadzuje ako objekt
      throw redirect({ to: "/" });
    },
  }),
  createRoute({ getParentRoute: () => shell, path: "/lines", component: lazyRouteComponent(() => import("@/features/lines/LinesPage"), "LinesPage") }),
  createRoute({ getParentRoute: () => shell, path: "/designs", component: lazyRouteComponent(() => import("@/features/designs/DesignsPage"), "DesignsPage") }),
  createRoute({ getParentRoute: () => shell, path: "/log", component: lazyRouteComponent(() => import("@/features/traffic/LogPage"), "LogPage") }),
  createRoute({
    getParentRoute: () => shell,
    path: "/recordings",
    component: lazyRouteComponent(() => import("@/features/recordings/RecordingsPage"), "RecordingsPage"),
  }),
  createRoute({
    getParentRoute: () => shell,
    path: "/virtual",
    component: lazyRouteComponent(() => import("@/features/virtual/VirtualBoardsPage"), "VirtualBoardsPage"),
  }),
  createRoute({ getParentRoute: () => shell, path: "/archive/tables", component: () => <ArchivePage key="tables" kind="tables" /> }),
  createRoute({ getParentRoute: () => shell, path: "/archive/reports", component: () => <ArchivePage key="reports" kind="reports" /> }),
  createRoute({ getParentRoute: () => shell, path: "/users", component: lazyRouteComponent(() => import("@/features/users/UsersPage"), "UsersPage") }),
  createRoute({ getParentRoute: () => shell, path: "/keys", component: lazyRouteComponent(() => import("@/features/keys/KeysPage"), "KeysPage") }),
  createRoute({ getParentRoute: () => shell, path: "/audit", component: lazyRouteComponent(() => import("@/features/audit/AuditPage"), "AuditPage") }),
  createRoute({
    getParentRoute: () => shell,
    path: "/settings",
    component: lazyRouteComponent(() => import("@/features/settings/SettingsPage"), "SettingsPage"),
  }),
] as const;

const routeTree = rootRoute.addChildren([boardKiosk, wall, virtualKiosk, shell.addChildren(pages)]);

export const router = createRouter({ routeTree, history: createHashHistory(), defaultNotFoundComponent: NotFound });

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
