// Stranky a ich adresy (TanStack Router s adresami za #): ram s menu, kiosky bez ovladania a prihlasenie pred vsetkym.
// Adresy su rovnake ako pred prestavbou - odkazy zariadeni (?key=…#/virtual/3) a ulozene zalozky ostavaju platne.
import { lazy, useEffect, type ReactNode } from "react";
import { createHashHistory, createRootRoute, createRoute, createRouter, lazyRouteComponent, Outlet, redirect } from "@tanstack/react-router";
import { openSocket, useMe } from "@/api";
import { t, useLanguage } from "@/i18n";

// stranky sa nacitaju az pri prvom zobrazeni - kiosk (televizor, tablet) nestahuje ovladanie a naopak
const AuthPage = lazy(() => import("@/components/AuthPage").then((m) => ({ default: m.AuthPage })));
const BoardKiosk = lazy(() => import("./BoardKiosk").then((m) => ({ default: m.BoardKiosk })));
const VirtualKiosk = lazy(() => import("@/components/VirtualKiosk").then((m) => ({ default: m.VirtualKiosk })));
const VirtualPage = lazy(() => import("@/components/VirtualPage").then((m) => ({ default: m.VirtualPage })));
const UsersPage = lazy(() => import("@/components/UsersPage").then((m) => ({ default: m.UsersPage })));

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
  if (me.loginRequired && !me.authenticated)
    return (
      <Legacy>
        <AuthPage setup={me.setupRequired} />
      </Legacy>
    );
  // zmena jazyka nakresli stranky znova (texty sa beru pri kresleni)
  return <Outlet key={language} />;
}

/** Stranka este v starom vzhlade (legacy.css). */
function Legacy({ children }: { children: ReactNode }) {
  return <div className="legacy">{children}</div>;
}

const rootRoute = createRootRoute({ component: Root });

const boardKiosk = createRoute({
  getParentRoute: () => rootRoute,
  path: "/view/$boardId",
  component: function BoardKioskRoute() {
    const { boardId } = boardKiosk.useParams();
    return (
      <Legacy>
        <BoardKiosk boardId={boardId} />
      </Legacy>
    );
  },
});

const wall = createRoute({
  getParentRoute: () => rootRoute,
  path: "/wall",
  component: () => (
    <Legacy>
      <BoardKiosk boardId={null} />
    </Legacy>
  ),
});

const virtualKiosk = createRoute({
  getParentRoute: () => rootRoute,
  path: "/virtual/$id",
  component: function VirtualKioskRoute() {
    const { id } = virtualKiosk.useParams();
    return (
      <Legacy>
        <VirtualKiosk id={Number(id)} />
      </Legacy>
    );
  },
});

const shell = createRoute({ getParentRoute: () => rootRoute, id: "shell", component: lazyRouteComponent(() => import("./AppShell"), "AppShell"), notFoundComponent: () => t("nav.notFound") });

const pages = [
  createRoute({ getParentRoute: () => shell, path: "/", component: lazyRouteComponent(() => import("@/components/BoardsPage"), "BoardsPage") }),
  // povodna adresa stranky tabul
  createRoute({
    getParentRoute: () => shell,
    path: "/boards",
    beforeLoad: () => {
      throw redirect({ to: "/" });
    },
  }),
  createRoute({ getParentRoute: () => shell, path: "/lines", component: lazyRouteComponent(() => import("@/components/LinesPage"), "LinesPage") }),
  createRoute({ getParentRoute: () => shell, path: "/log", component: lazyRouteComponent(() => import("@/components/LogPage"), "LogPage") }),
  createRoute({ getParentRoute: () => shell, path: "/recordings", component: lazyRouteComponent(() => import("@/components/RecordingsPage"), "RecordingsPage") }),
  createRoute({ getParentRoute: () => shell, path: "/virtual", component: () => <VirtualPage tab="boards" /> }),
  createRoute({ getParentRoute: () => shell, path: "/archive/tables", component: () => <VirtualPage tab="tables" /> }),
  createRoute({ getParentRoute: () => shell, path: "/archive/reports", component: () => <VirtualPage tab="reports" /> }),
  createRoute({ getParentRoute: () => shell, path: "/users", component: () => <UsersPage tab="users" /> }),
  createRoute({ getParentRoute: () => shell, path: "/keys", component: () => <UsersPage tab="keys" /> }),
  createRoute({ getParentRoute: () => shell, path: "/audit", component: () => <UsersPage tab="audit" /> }),
  createRoute({ getParentRoute: () => shell, path: "/settings", component: lazyRouteComponent(() => import("@/components/SettingsPage"), "SettingsPage") }),
] as const;

const routeTree = rootRoute.addChildren([boardKiosk, wall, virtualKiosk, shell.addChildren(pages)]);

export const router = createRouter({ routeTree, history: createHashHistory() });

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
