// Pata bocneho panela: kto je prihlaseny a jeho ponuka - jazyk, vzhlad, systemove oznamenia, vlastne heslo, odhlasenie
// (alebo kluc karty).
import { useState } from "react";
import { Bell, ChevronsUpDown, KeyRound, Languages, LogOut, Monitor, Moon, Sun, SunMoon } from "lucide-react";
import { toast } from "sonner";
import { forgetAccessKey, logout, queryClient, type MeDto } from "@/api";
import { PasswordDialog } from "@/features/users/PasswordDialog";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { SidebarMenu, SidebarMenuButton, SidebarMenuItem, useSidebar } from "@/components/ui/sidebar";
import { setLanguage, t, useLanguage, type Language, type TextKey } from "@/i18n";
import { setTheme, THEMES, useTheme, type Theme } from "@/lib/theme";
import { notificationsSupported, setNotifications, useNotifications } from "./notifications";

const LANGUAGES: Language[] = ["sk", "cs"];
const THEME_ICONS = { light: Sun, dark: Moon, auto: Monitor } satisfies Record<Theme, unknown>;

/** Odhlasenie - cache patri predchadzajucemu pouzivatelovi, zahodi sa a zisti sa znova, kto je prihlaseny. */
function signOut() {
  void logout().finally(() => queryClient.resetQueries());
}

function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  return (parts.length > 1 ? parts[0][0] + parts[parts.length - 1][0] : name.slice(0, 2)).toUpperCase();
}

export function UserMenu({ me }: { me: MeDto }) {
  const { isMobile } = useSidebar();
  const language = useLanguage();
  const theme = useTheme();
  const notifications = useNotifications();
  const [password, setPassword] = useState(false);
  const [passwordDone, setPasswordDone] = useState(false);

  // bez prihlasovania je kazdy na tomto pocitaci spravca; karta s klucom je zariadenie
  const name = me.accessKey ? t("user.device") : me.loginRequired ? me.displayName || me.userName || "" : t("user.local");
  const role = t(`role.${me.role ?? "viewer"}` as TextKey);

  return (
    <>
      <SidebarMenu>
        <SidebarMenuItem>
          <DropdownMenu>
            <DropdownMenuTrigger
              render={<SidebarMenuButton size="lg" className="data-popup-open:bg-sidebar-accent data-popup-open:text-sidebar-accent-foreground" />}
            >
              <span className="flex size-8 shrink-0 items-center justify-center rounded-lg bg-sidebar-primary text-xs font-semibold text-sidebar-primary-foreground">
                {me.accessKey ? <KeyRound className="size-4" /> : initials(name)}
              </span>
              <span className="grid flex-1 text-left text-sm leading-tight">
                <span className="truncate font-medium" title={me.userName ?? undefined}>
                  {name}
                </span>
                <span className="truncate text-xs text-muted-foreground">{role}</span>
              </span>
              <ChevronsUpDown className="ml-auto size-4" />
            </DropdownMenuTrigger>
            <DropdownMenuContent side={isMobile ? "bottom" : "right"} align="end" sideOffset={4} className="min-w-56">
              {passwordDone && (
                <DropdownMenuGroup>
                  <DropdownMenuLabel>{t("users.passwordChanged")}</DropdownMenuLabel>
                </DropdownMenuGroup>
              )}
              <DropdownMenuSub>
                <DropdownMenuSubTrigger>
                  <Languages />
                  {t("lang.label")}
                </DropdownMenuSubTrigger>
                <DropdownMenuSubContent>
                  <DropdownMenuRadioGroup value={language} onValueChange={(value) => setLanguage(value as Language)}>
                    {LANGUAGES.map((l) => (
                      <DropdownMenuRadioItem key={l} value={l}>
                        {t(`lang.${l}`)}
                      </DropdownMenuRadioItem>
                    ))}
                  </DropdownMenuRadioGroup>
                </DropdownMenuSubContent>
              </DropdownMenuSub>
              <DropdownMenuSub>
                <DropdownMenuSubTrigger>
                  <SunMoon />
                  {t("theme.label")}
                </DropdownMenuSubTrigger>
                <DropdownMenuSubContent>
                  <DropdownMenuRadioGroup value={theme} onValueChange={(value) => setTheme(value as Theme)}>
                    {THEMES.map((value) => {
                      const Icon = THEME_ICONS[value];
                      return (
                        <DropdownMenuRadioItem key={value} value={value}>
                          <Icon />
                          {t(`theme.${value}`)}
                        </DropdownMenuRadioItem>
                      );
                    })}
                  </DropdownMenuRadioGroup>
                </DropdownMenuSubContent>
              </DropdownMenuSub>
              {notificationsSupported() && (
                <DropdownMenuCheckboxItem
                  checked={notifications}
                  title={t("notify.hint")}
                  onCheckedChange={(on) => void setNotifications(on).then((done) => done || toast.error(t("notify.denied")))}
                >
                  <Bell />
                  {t("notify.label")}
                </DropdownMenuCheckboxItem>
              )}
              {me.accessKey ? (
                <>
                  <DropdownMenuSeparator />
                  <DropdownMenuGroup>
                    <DropdownMenuLabel>{t("keys.thisTab")}</DropdownMenuLabel>
                    <DropdownMenuItem
                      onClick={() => {
                        forgetAccessKey();
                        location.reload();
                      }}
                    >
                      <KeyRound />
                      {t("keys.forget")}
                    </DropdownMenuItem>
                  </DropdownMenuGroup>
                </>
              ) : (
                me.loginRequired && (
                  <>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem onClick={() => setPassword(true)}>
                      <KeyRound />
                      {t("users.ownPassword")}
                    </DropdownMenuItem>
                    <DropdownMenuItem onClick={signOut}>
                      <LogOut />
                      {t("auth.logout")}
                    </DropdownMenuItem>
                  </>
                )
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        </SidebarMenuItem>
      </SidebarMenu>
      {password && (
        <PasswordDialog
          userName={null}
          onClose={(changed) => {
            setPassword(false);
            setPasswordDone(changed);
          }}
        />
      )}
    </>
  );
}
