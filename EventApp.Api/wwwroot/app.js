const TOKEN_KEY = "eventapp_web_token";
const REMINDERS_KEY = "eventapp_web_reminders_enabled";
const APP_VERSION = "20260703-contact-links";
const APP_NAME = "Summer Fest";
const ROUTES = ["schedule", "info", "account"];
const DIRECTORY_TYPES = ["Speaker", "Sponsor", "Exhibitor", "Attendee"];
const REGISTRATION_OVERLAP_MESSAGE = "Activity overlaps with another registration.";
const FULL_NAME_VALIDATION_MESSAGE = "Full name must contain two words, each starting with a capital letter and at least 2 letters long.";
const EMAIL_VALIDATION_MESSAGE = "Enter a valid email address.";
const DEFAULT_LOGO_URL = "/images/default-logo.png";
const DEFAULT_WEB_HERO_IMAGE = "/images/event-fest-hero-2026.png";
const CONTACTS_SLACK_URL = "https://plslife.slack.com/archives/C0BAXHVJY9Y";
const NAV_STATE_KEY = "eventapp";
const NAV_RESTORE_MAX_ATTEMPTS = 60;

const state = {
  token: localStorage.getItem(TOKEN_KEY),
  user: null,
  info: null,
  route: "schedule",
  message: "",
  popupTitle: "",
  popupMessage: "",
  reminderPromptVisible: false,
  reminderPromptDismissed: false,
  loadRequestId: 0,
  busy: false,
  selectedDate: "",
  directoryType: "Speaker",
  activities: [],
  activityParticipants: {},
  myActivities: [],
  allUsers: [],
  remindersEnabled: localStorage.getItem(REMINDERS_KEY) === "true",
  notificationPermission: typeof Notification === "undefined" ? "unsupported" : Notification.permission,
  pushConfig: null,
  shellKey: "",
  renderedRoute: ""
};

const app = document.querySelector("#app");
let pendingNavigationRestore = null;

if ("scrollRestoration" in history) {
  history.scrollRestoration = "manual";
}

function icon(name) {
  const icons = {
    schedule: '<svg viewBox="0 0 24 24"><path d="M7 3v3M17 3v3M4 9h16M6 5h12a2 2 0 0 1 2 2v11a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2Z"/><path d="M8 13h3v3H8z"/></svg>',
    info: '<svg viewBox="0 0 24 24"><path d="M12 17v-6M12 7h.01"/><circle cx="12" cy="12" r="9"/></svg>',
    account: '<svg viewBox="0 0 24 24"><path d="M20 21a8 8 0 0 0-16 0"/><circle cx="12" cy="7" r="4"/></svg>',
    admin: '<svg viewBox="0 0 24 24"><path d="m12 3 2.7 5.5 6.1.9-4.4 4.3 1 6.1-5.4-2.9-5.4 2.9 1-6.1-4.4-4.3 6.1-.9L12 3Z"/></svg>',
    refresh: '<svg viewBox="0 0 24 24"><path d="M20 12a8 8 0 0 1-14.9 4M4 12A8 8 0 0 1 18.9 8"/><path d="M20 4v4h-4M4 20v-4h4"/></svg>',
    signout: '<svg viewBox="0 0 24 24"><path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4"/><path d="M10 17l5-5-5-5M15 12H3"/></svg>',
    save: '<svg viewBox="0 0 24 24"><path d="m5 12 4 4L19 6"/></svg>',
    add: '<svg viewBox="0 0 24 24"><path d="M12 5v14M5 12h14"/></svg>',
    delete: '<svg viewBox="0 0 24 24"><path d="M3 6h18M8 6V4h8v2M10 11v6M14 11v6M6 6l1 15h10l1-15"/></svg>',
    paint: '<svg viewBox="0 0 24 24"><path d="M19 11.5 12.5 5a3.5 3.5 0 0 0-5 0L4 8.5 15.5 20 19 16.5a3.5 3.5 0 0 0 0-5Z"/><path d="M2 22h20M7 10h10"/></svg>',
    clock: '<svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></svg>',
    location: '<svg viewBox="0 0 24 24"><path d="M12 21s7-5.4 7-12a7 7 0 1 0-14 0c0 6.6 7 12 7 12Z"/><circle cx="12" cy="9" r="2"/></svg>',
    users: '<svg viewBox="0 0 24 24"><path d="M16 21a6 6 0 0 0-12 0"/><circle cx="10" cy="8" r="4"/><path d="M22 21a5 5 0 0 0-4-4.9M17 4.2a4 4 0 0 1 0 7.6"/></svg>',
    chevron: '<svg viewBox="0 0 24 24"><path d="m9 18 6-6-6-6"/></svg>'
  };
  return `<span class="tab-icon" aria-hidden="true">${icons[name] ?? icons.info}</span>`;
}

function escapeHtml(value = "") {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function trimLinkedUrl(value) {
  const trailingPunctuation = value.match(/[),.;:!?]+$/)?.[0] || "";
  if (!trailingPunctuation) {
    return [value, ""];
  }

  return [value.slice(0, -trailingPunctuation.length), trailingPunctuation];
}

function linkTextForUrl(value) {
  const normalized = value.toLowerCase();
  if (normalized.startsWith("slack://") || normalized.includes(".slack.com/")) {
    return "Open Slack";
  }

  return value;
}

function renderLinkedText(value = "") {
  const text = String(value);
  const urlPattern = /(?:https?:\/\/|slack:\/\/)[^\s<>"']+/gi;
  let lastIndex = 0;
  let output = "";

  for (const match of text.matchAll(urlPattern)) {
    output += escapeHtml(text.slice(lastIndex, match.index));

    const [url, punctuation] = trimLinkedUrl(match[0]);
    output += `<a class="inline-link" href="${escapeHtml(url)}" target="_blank" rel="noopener noreferrer">${escapeHtml(linkTextForUrl(url))}</a>${escapeHtml(punctuation)}`;
    lastIndex = match.index + match[0].length;
  }

  return output + escapeHtml(text.slice(lastIndex));
}

function renderContacts(value = "") {
  const contacts = renderLinkedText(value);
  const hasSlackLink = String(value).toLowerCase().includes(CONTACTS_SLACK_URL.toLowerCase());
  const slackLink = `<a class="inline-link" href="${CONTACTS_SLACK_URL}" target="_blank" rel="noopener noreferrer">Open Slack</a>`;

  if (!contacts) {
    return slackLink;
  }

  return hasSlackLink ? contacts : `${contacts} <span class="contact-separator">|</span> ${slackLink}`;
}

function formatDate(value) {
  if (!value) return "";
  return new Intl.DateTimeFormat(undefined, { weekday: "short", month: "short", day: "numeric" })
    .format(new Date(`${value}T00:00:00`));
}

function formatTime(value) {
  if (!value) return "";
  return String(value).slice(0, 5);
}

function formatTimeRange(startTime, endTime) {
  const start = formatTime(startTime);
  const end = formatTime(endTime);
  if (!start || !end) return [start, end].filter(Boolean).join(" - ");
  const overnight = end < start;
  return `${start} - ${end}${overnight ? " (+1 day)" : ""}`;
}

function compareActivitiesByTime(a, b) {
  return a.startTime.localeCompare(b.startTime) || a.endTime.localeCompare(b.endTime);
}

function eventTitle(value) {
  const title = value?.trim();
  return !title || title.toLowerCase() === "eventapp" ? APP_NAME : title;
}

function roleName(role) {
  return Number(role) === 1 || role === "Admin" ? "Admin" : "User";
}

function directoryName(type) {
  return DIRECTORY_TYPES[Number(type)] ?? type ?? "Attendee";
}

function initials(name = "E") {
  return name.trim().split(/\s+/).slice(0, 2).map(part => part[0]).join("").toUpperCase() || "E";
}

function isEmailValid(email = "") {
  return /^[^@\s]+@[^@\s.]+(?:\.[^@\s.]+)+$/.test(email);
}

function avatar(user) {
  if (user?.avatarUrl) {
    return `<span class="avatar"><img src="${escapeHtml(user.avatarUrl)}" alt=""></span>`;
  }
  return `<span class="avatar">${escapeHtml(initials(user?.fullName))}</span>`;
}

function brandMark(className = "") {
  const logoUrl = state.info?.logoImageUrl || DEFAULT_LOGO_URL;
  const classes = ["brand-mark", className].filter(Boolean).join(" ");
  const content = logoUrl === DEFAULT_LOGO_URL
    ? '<div class="default-logo-rectangle" aria-hidden="true"></div>'
    : `<img src="${escapeHtml(logoUrl)}" alt="">`;
  return `<div class="${classes}">${content}</div>`;
}

async function loadBranding() {
  try {
    const branding = await api("/api/info/branding");
    state.info = { ...state.info, ...branding };
  } catch {
  }
}

async function api(path, options = {}) {
  const headers = new Headers(options.headers);
  if (!(options.body instanceof FormData)) {
    headers.set("Content-Type", "application/json");
  }
  if (state.token) {
    headers.set("Authorization", `Bearer ${state.token}`);
  }

  const response = await fetch(path, { ...options, headers, cache: "no-store" });
  if (response.status === 401) {
    localStorage.removeItem(TOKEN_KEY);
    state.token = null;
    state.user = null;
    renderAuth("signin", "Please sign in again.");
    throw new Error("Unauthorized. Please sign in again.");
  }
  if (!response.ok) {
    const text = await response.text();
    let message = response.statusText || "Request failed.";
    try {
      const problem = JSON.parse(text);
      message = problem.message || problem.title || problem.detail || message;
    } catch {
      message = text || message;
    }
    throw new Error(message);
  }
  if (response.status === 204) {
    return null;
  }
  const text = await response.text();
  return text ? JSON.parse(text) : null;
}

function setMessage(message) {
  if (message === REGISTRATION_OVERLAP_MESSAGE) {
    state.message = "";
    state.popupTitle = "Registration conflict";
    state.popupMessage = message;
    render();
    return;
  }

  state.message = message;
  render();
}

function isFullNameValid(fullName) {
  return /^[A-Z][A-Za-z]{1,}\s+[A-Z][A-Za-z]{1,}$/.test(fullName);
}

function showPopup(title, message) {
  state.message = "";
  state.popupTitle = title;
  state.popupMessage = message;
  render();
}

function isSameData(currentValue, nextValue) {
  if (currentValue === nextValue) {
    return true;
  }

  return JSON.stringify(currentValue) === JSON.stringify(nextValue);
}

function setStateData(key, nextValue) {
  if (isSameData(state[key], nextValue)) {
    return false;
  }

  state[key] = nextValue;
  return true;
}

function renderPopup() {
  if (state.reminderPromptVisible && !state.popupMessage) {
    return `
      <div class="popup-backdrop" role="presentation">
        <section class="popup" role="dialog" aria-modal="true" aria-labelledby="popup-title" aria-describedby="popup-message">
          <h2 id="popup-title">Enable reminders</h2>
          <p id="popup-message">Get notified 30 minutes before your registered activities start.</p>
          <div class="popup-actions">
            <button class="secondary-btn" data-reminder-dismiss>Not now</button>
            <button class="primary-btn" data-reminder-enable>Enable</button>
          </div>
        </section>
      </div>
    `;
  }

  if (!state.popupMessage) {
    return "";
  }

  return `
    <div class="popup-backdrop" role="presentation">
      <section class="popup" role="dialog" aria-modal="true" aria-labelledby="popup-title" aria-describedby="popup-message">
        <h2 id="popup-title">${escapeHtml(state.popupTitle || "Notice")}</h2>
        <p id="popup-message">${escapeHtml(state.popupMessage)}</p>
        <button class="primary-btn" data-popup-ok>ok</button>
      </section>
    </div>
  `;
}

function routeFromUrl(url = location.href) {
  try {
    const route = new URL(url, location.href).hash.replace("#", "").split(/[/?]/)[0];
    return ROUTES.includes(route) || route === "admin" ? route : "";
  } catch {
    return "";
  }
}

function routeUrl(route) {
  return `${location.pathname}${location.search}#${route}`;
}

function routeHistoryState(route = state.route, restore = null) {
  return {
    [NAV_STATE_KEY]: true,
    screen: "route",
    route,
    restore
  };
}

function ensureRouteHistoryState() {
  if (!history.state?.[NAV_STATE_KEY]) {
    history.replaceState(routeHistoryState(state.route), "", routeUrl(state.route));
  }
}

function replaceCurrentRouteRestore(restore) {
  const current = history.state?.[NAV_STATE_KEY] ? history.state : routeHistoryState(state.route);
  history.replaceState({ ...current, screen: "route", route: state.route, restore }, "", routeUrl(state.route));
}

function pushContextScreen(screen, restore, context = {}) {
  replaceCurrentRouteRestore(restore);
  history.pushState({
    [NAV_STATE_KEY]: true,
    screen,
    route: state.route,
    ...context
  }, "", routeUrl(state.route));
}

function currentScrollContext(kind, itemId = "") {
  return {
    kind,
    itemId,
    x: Math.max(0, Math.round(window.scrollX || window.pageXOffset || 0)),
    y: Math.max(0, Math.round(window.scrollY || window.pageYOffset || 0))
  };
}

function restoreSelector(restore) {
  if (!restore?.itemId) {
    return "";
  }
  const itemId = typeof CSS !== "undefined" && CSS.escape
    ? CSS.escape(restore.itemId)
    : String(restore.itemId).replaceAll('"', '\\"');
  if (restore.kind === "schedule-activity") {
    return `[data-activity-card="${itemId}"]`;
  }
  if (restore.kind === "admin-activity") {
    return `[data-admin-activity-card="${itemId}"]`;
  }
  if (restore.kind === "admin-user") {
    return `[data-admin-user-card="${itemId}"]`;
  }
  return "";
}

function scrollToTopAfterRender() {
  pendingNavigationRestore = null;
  requestAnimationFrame(() => window.scrollTo({ top: 0, left: 0, behavior: "auto" }));
}

function scheduleNavigationRestore(restore) {
  if (!restore) {
    return;
  }

  pendingNavigationRestore = { ...restore, attempts: 0 };
  restoreNavigationContext();
}

function restoreNavigationContext() {
  if (!pendingNavigationRestore) {
    return;
  }

  requestAnimationFrame(() => {
    if (!pendingNavigationRestore) {
      return;
    }

    const selector = restoreSelector(pendingNavigationRestore);
    const target = selector ? document.querySelector(selector) : null;
    const maxY = Math.max(0, document.documentElement.scrollHeight - window.innerHeight);
    const wantedY = Number(pendingNavigationRestore.y) || 0;
    const targetY = Math.min(wantedY, maxY);
    const canUseSavedPosition = maxY >= wantedY;
    const contentReady = canUseSavedPosition || target || pendingNavigationRestore.attempts >= NAV_RESTORE_MAX_ATTEMPTS;

    if (contentReady) {
      if (canUseSavedPosition) {
        window.scrollTo({ left: Number(pendingNavigationRestore.x) || 0, top: targetY, behavior: "auto" });
      } else if (target) {
        target.scrollIntoView({ block: "center" });
      }

      const restored = canUseSavedPosition
        ? Math.abs((window.scrollY || window.pageYOffset || 0) - targetY) <= 2
        : Boolean(target);
      if (restored || pendingNavigationRestore.attempts >= NAV_RESTORE_MAX_ATTEMPTS) {
        pendingNavigationRestore = null;
        return;
      }
    }

    pendingNavigationRestore.attempts += 1;
    window.setTimeout(restoreNavigationContext, 50);
  });
}

function returnToRoute(route) {
  if (history.state?.[NAV_STATE_KEY] && history.state.screen !== "route" && history.state.route === route) {
    history.back();
    return;
  }

  state.route = route;
  render();
}

function nav(route) {
  if (route === state.route) {
    state.message = "";
    render();
    scrollToTopAfterRender();
    loadRoute();
    return;
  }

  ensureRouteHistoryState();
  history.pushState(routeHistoryState(route), "", routeUrl(route));
  state.route = route;
  state.message = "";
  render();
  scrollToTopAfterRender();
  loadRoute();
}

async function bootstrap() {
  if ("serviceWorker" in navigator) {
    navigator.serviceWorker.register(`/service-worker.js?v=${APP_VERSION}`).catch(() => {});
  }

  const hashRoute = location.hash.replace("#", "").split(/[/?]/)[0];
  if (ROUTES.includes(hashRoute) || hashRoute === "admin") {
    state.route = hashRoute;
  }
  ensureRouteHistoryState();

  await loadBranding();

  if (!state.token) {
    renderAuth("signin");
    return;
  }

  try {
    state.user = await api("/api/auth/me");
    await loadRoute();
  } catch {
    renderAuth("signin");
  }
}

function supportsEventNotifications() {
  return "Notification" in window && "serviceWorker" in navigator && "PushManager" in window;
}

function canUseEventNotifications() {
  return supportsEventNotifications() && state.remindersEnabled && Notification.permission === "granted";
}

function getNotificationPermission() {
  if (!supportsEventNotifications()) {
    return "unsupported";
  }

  return Notification.permission;
}

async function refreshEventReminders() {
  state.notificationPermission = getNotificationPermission();
  if (!supportsEventNotifications() || !state.token) {
    return;
  }

  if (Notification.permission === "default" && !state.reminderPromptDismissed) {
    state.reminderPromptVisible = true;
    return;
  }

  state.reminderPromptVisible = false;
  if (Notification.permission !== "granted") {
    state.remindersEnabled = false;
    localStorage.removeItem(REMINDERS_KEY);
    return;
  }

  try {
    await syncPushSubscription();
    state.remindersEnabled = true;
    localStorage.setItem(REMINDERS_KEY, "true");
  } catch {
    state.remindersEnabled = false;
    localStorage.removeItem(REMINDERS_KEY);
  }
}

async function enableEventReminders() {
  if (!supportsEventNotifications()) {
    state.message = "This browser does not support event notifications.";
    render();
    return;
  }

  let permission = Notification.permission;
  if (permission === "default") {
    permission = await Notification.requestPermission();
  }

  state.notificationPermission = permission;
  state.reminderPromptVisible = false;
  if (permission === "granted") {
    try {
      await syncPushSubscription();
      state.remindersEnabled = true;
      localStorage.setItem(REMINDERS_KEY, "true");
      state.message = "Reminders enabled. You will be notified 30 minutes before registered activities start.";
    } catch (error) {
      state.remindersEnabled = false;
      localStorage.removeItem(REMINDERS_KEY);
      state.message = error.message;
    }
  } else {
    state.remindersEnabled = false;
    localStorage.removeItem(REMINDERS_KEY);
    state.message = "Notifications are blocked. Enable them in your browser or device settings to receive reminders.";
  }
  render();
}

async function disableEventReminders() {
  await removePushSubscription();
  state.remindersEnabled = false;
  localStorage.removeItem(REMINDERS_KEY);
  state.message = "Event reminders disabled.";
  render();
}

async function removePushSubscription() {
  if (supportsEventNotifications()) {
    try {
      const registration = await navigator.serviceWorker.ready;
      const subscription = await registration.pushManager.getSubscription();
      if (subscription) {
        await api("/api/notifications/subscriptions/delete", {
          method: "POST",
          body: JSON.stringify(subscription.toJSON())
        });
        await subscription.unsubscribe();
      }
    } catch {
    }
  }
}

async function getPushConfig() {
  if (!state.pushConfig) {
    state.pushConfig = await api("/api/notifications/config");
  }

  return state.pushConfig;
}

async function syncPushSubscription() {
  const config = await getPushConfig();
  if (!config?.isEnabled || !config.publicKey) {
    throw new Error("Push notifications are not configured on the server.");
  }

  const registration = await navigator.serviceWorker.ready;
  const existing = await registration.pushManager.getSubscription();
  const subscription = existing || await registration.pushManager.subscribe({
    userVisibleOnly: true,
    applicationServerKey: urlBase64ToUint8Array(config.publicKey)
  });

  await api("/api/notifications/subscriptions", {
    method: "POST",
    body: JSON.stringify(subscription.toJSON())
  });

  return subscription;
}

function urlBase64ToUint8Array(base64String) {
  const padding = "=".repeat((4 - base64String.length % 4) % 4);
  const base64 = (base64String + padding).replaceAll("-", "+").replaceAll("_", "/");
  const rawData = atob(base64);
  return Uint8Array.from([...rawData].map(character => character.charCodeAt(0)));
}

function renderAuth(mode = "signin", message = "") {
  state.shellKey = "";
  if (mode === "signup" && state.info?.isSystemRegistrationClosed) {
    mode = "signin";
    message ||= "Registration to the system is closed.";
  }

  const isSignup = mode === "signup";
  const signupDisabled = state.info?.isSystemRegistrationClosed ? "disabled" : "";
  app.innerHTML = `
    <main class="auth-layout">
      <section class="auth-card">
        <div class="brand-row">
          ${isSignup ? '<div class="signup-logo-rectangle" aria-hidden="true"></div>' : brandMark()}
          <div>
            <strong>${APP_NAME}</strong>
            <span>Schedule and registration</span>
          </div>
        </div>
        <h1>${isSignup ? "Create account" : "Welcome back"}</h1>
        <p>${isSignup ? "Join the event with your name, email, and the application password." : "Sign in with your email and the application password."}</p>
        <div class="mode-switch">
          <button class="${mode === "signin" ? "active" : ""}" data-auth-mode="signin">Sign in</button>
          <button class="${mode === "signup" ? "active" : ""}" data-auth-mode="signup" ${signupDisabled}>Sign up</button>
        </div>
        <form class="form-grid" id="auth-form">
          <label class="${isSignup ? "" : "hide"}">Full name<input name="fullName" autocomplete="name"></label>
          <label>Email<input name="email" type="email" autocomplete="email" required></label>
          <label>Application password<input name="password" type="password" autocomplete="current-password" required></label>
          <button class="primary-btn" type="submit">${icon("save")}${isSignup ? "Create account" : "Sign in"}</button>
        </form>
        ${message ? `<div class="message">${escapeHtml(message)}</div>` : ""}
      </section>
    </main>
  `;

  document.querySelectorAll("[data-auth-mode]").forEach(button => {
    button.addEventListener("click", () => renderAuth(button.dataset.authMode));
  });

  document.querySelector("#auth-form").addEventListener("submit", async event => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    try {
      const email = form.get("email")?.toString().trim();
      const password = form.get("password")?.toString();
      let payload;
      if (isSignup) {
        const fullName = form.get("fullName")?.toString().trim() ?? "";
        if (!isFullNameValid(fullName)) {
          renderAuth(mode, FULL_NAME_VALIDATION_MESSAGE);
          return;
        }

        if (!isEmailValid(email)) {
          renderAuth(mode, EMAIL_VALIDATION_MESSAGE);
          return;
        }

        payload = await api("/api/auth/signup", {
          method: "POST",
          body: JSON.stringify({ fullName, email, password })
        });
      } else {
        payload = await api("/api/auth/signin", {
          method: "POST",
          body: JSON.stringify({ provider: "local", idToken: email, password, fullName: null })
        });
      }
      state.token = payload.accessToken;
      state.user = payload.user;
      localStorage.setItem(TOKEN_KEY, state.token);
      state.route = "schedule";
      location.hash = "schedule";
      await loadRoute();
    } catch (error) {
      renderAuth(mode, error.message);
    }
  });
}

function shell(content) {
  const navItems = [
    ["schedule", "Schedule"],
    ["info", "Info"],
    ["account", "Account"]
  ];
  const canAdmin = roleName(state.user?.role) === "Admin";
  const allNav = canAdmin ? [...navItems, ["admin", "Admin"]] : navItems;
  const shellKey = [
    canAdmin ? "admin" : "user",
    state.user?.id || "",
    state.user?.fullName || "",
    state.info?.title || "",
    state.info?.logoImageUrl || DEFAULT_LOGO_URL
  ].join(":");
  const contentHtml = `${content}${state.message ? `<div class="message">${escapeHtml(state.message)}</div>` : ""}`;

  if (state.shellKey === shellKey && app.querySelector(".main-layout")) {
    app.querySelector(".content").innerHTML = contentHtml;
    app.querySelector("[data-popup-root]").innerHTML = renderPopup();
    updateShellActiveRoute();
    bindShellActions();
    return;
  }

  state.shellKey = shellKey;

  app.innerHTML = `
    <div class="main-layout">
      <aside class="sidebar">
        <div class="brand-row">
          ${brandMark()}
          <div>
            <strong>${escapeHtml(eventTitle(state.info?.title))}</strong>
            <span>${escapeHtml(state.user?.fullName || "")}</span>
          </div>
        </div>
        <nav class="nav-list">
          ${allNav.map(([id, label]) => `<button class="${state.route === id ? "active" : ""}" data-route="${id}">${icon(id)}${label}</button>`).join("")}
        </nav>
      </aside>
      <div>
        <header class="topbar">
          <div class="brand-row" style="padding:0">
            ${brandMark("brand-mark-small")}
            <strong>${escapeHtml(eventTitle(state.info?.title))}</strong>
          </div>
          <button class="icon-btn" data-refresh title="Refresh">${icon("refresh")}</button>
        </header>
        <main class="content">${contentHtml}</main>
      </div>
      <nav class="mobile-tabs">
        ${allNav.map(([id, label]) => `<button class="${state.route === id ? "active" : ""}" data-route="${id}">${icon(id)}<span>${label}</span></button>`).join("")}
      </nav>
    </div>
    <div data-popup-root>${renderPopup()}</div>
  `;

  requestAnimationFrame(() => {
    app.querySelector(".main-layout")?.classList.add("render-stable");
  });
  bindShellActions();
}

function updateShellActiveRoute() {
  document.querySelectorAll("[data-route]").forEach(button => {
    button.classList.toggle("active", button.dataset.route === state.route);
  });
}

function bindShellActions() {
  document.querySelectorAll("[data-route]").forEach(button => {
    if (button.dataset.bound === "true") {
      return;
    }
    button.dataset.bound = "true";
    button.addEventListener("click", () => nav(button.dataset.route));
  });
  const refreshButton = document.querySelector("[data-refresh]");
  if (refreshButton && refreshButton.dataset.bound !== "true") {
    refreshButton.dataset.bound = "true";
    refreshButton.addEventListener("click", loadRoute);
  }
  document.querySelector("[data-popup-ok]")?.addEventListener("click", () => {
    state.popupTitle = "";
    state.popupMessage = "";
    render();
  });
  document.querySelector("[data-reminder-dismiss]")?.addEventListener("click", () => {
    state.reminderPromptVisible = false;
    state.reminderPromptDismissed = true;
    render();
  });
  document.querySelector("[data-reminder-enable]")?.addEventListener("click", enableEventReminders);
}

async function loadRoute() {
  if (!state.token) {
    renderAuth("signin");
    return;
  }

  const requestId = ++state.loadRequestId;
  const route = state.route;
  const beforeReminderUi = JSON.stringify({
    notificationPermission: state.notificationPermission,
    reminderPromptVisible: state.reminderPromptVisible,
    remindersEnabled: state.remindersEnabled
  });
  let didChange = false;

  try {
    if (!state.info) {
      didChange = setStateData("info", await api("/api/info")) || didChange;
    }
    if (route === "schedule") {
      const activities = await api("/api/activities");
      if (requestId !== state.loadRequestId || route !== state.route) {
        return;
      }
      didChange = setStateData("activities", activities) || didChange;
      const dates = [...new Set(state.activities.map(a => a.date))].sort();
      const nextSelectedDate = dates.includes(state.selectedDate) ? state.selectedDate : dates[0] || "";
      if (state.selectedDate !== nextSelectedDate) {
        state.selectedDate = nextSelectedDate;
        didChange = true;
      }
    }
    if (route === "info") {
      const info = await api("/api/info");
      if (requestId !== state.loadRequestId || route !== state.route) {
        return;
      }
      didChange = setStateData("info", info) || didChange;
    }
    if (route === "account") {
      const user = await api("/api/auth/me");
      const myActivities = await api("/api/auth/me/activities");
      if (requestId !== state.loadRequestId || route !== state.route) {
        return;
      }
      didChange = setStateData("user", user) || didChange;
      didChange = setStateData("myActivities", myActivities) || didChange;
    }
    if (route === "admin") {
      const user = await api("/api/auth/me");
      const info = await api("/api/info");
      const activities = await api("/api/activities");
      const allUsers = await api("/api/users");
      if (requestId !== state.loadRequestId || route !== state.route) {
        return;
      }
      didChange = setStateData("user", user) || didChange;
      didChange = setStateData("info", info) || didChange;
      didChange = setStateData("activities", activities) || didChange;
      didChange = setStateData("allUsers", allUsers) || didChange;
    }
    if (requestId !== state.loadRequestId || route !== state.route) {
      return;
    }
    await refreshEventReminders();
    if (requestId !== state.loadRequestId || route !== state.route) {
      return;
    }
    const afterReminderUi = JSON.stringify({
      notificationPermission: state.notificationPermission,
      reminderPromptVisible: state.reminderPromptVisible,
      remindersEnabled: state.remindersEnabled
    });
    if (didChange || beforeReminderUi !== afterReminderUi || state.renderedRoute !== route || !app.querySelector(".main-layout")) {
      render();
    }
  } catch (error) {
    state.message = error.message;
    render();
  }
}

function render() {
  if (!state.token) {
    renderAuth("signin");
    return;
  }
  const views = {
    schedule: renderSchedule,
    info: renderInfo,
    account: renderAccount,
    admin: renderAdmin
  };
  shell((views[state.route] || renderSchedule)());
  bindViewActions();
  state.renderedRoute = state.route;
  restoreNavigationContext();
}

function renderSchedule() {
  const dates = [...new Set(state.activities.map(a => a.date))].sort();
  const activities = state.activities
    .filter(activity => !state.selectedDate || activity.date === state.selectedDate)
    .sort(compareActivitiesByTime);

  return `
    ${renderHero()}
    <section class="toolbar">
      <div class="date-tabs">
        ${dates.map(date => `<button class="${state.selectedDate === date ? "active" : ""}" data-date="${date}">${formatDate(date)}</button>`).join("")}
      </div>
    </section>
    <section class="grid">
      ${activities.length ? activities.map(renderActivityCard).join("") : `<div class="empty">No activities for this date.</div>`}
    </section>
  `;
}

function renderHero() {
  const style = `style="background-image:url('${escapeHtml(DEFAULT_WEB_HERO_IMAGE)}')"`;
  return `
    <section class="hero" ${style}></section>
  `;
}

function renderActivityCard(activity) {
  const participantState = state.activityParticipants[activity.id];
  const participantsLabel = participantState
    ? `<span class="label-wide">Hide participants</span><span class="label-narrow">Hide</span>`
    : "Participants";
  const participantsAriaLabel = participantState ? "Hide participants" : "Show participants";
  const activityControls = activity.requiresRegistration
    ? `
        <span class="pill registration-pill">${activity.registeredParticipants}/${activity.maxParticipants} registered</span>
        <div class="actions activity-actions">
          ${renderRegistrationButton(activity)}
          <button class="secondary-btn" data-participants="${activity.id}" aria-label="${participantsAriaLabel}" title="${participantsAriaLabel}">${participantsLabel}</button>
          <button class="secondary-btn" data-activity-details="${activity.id}">Details</button>
        </div>
      `
    : `
        <div class="actions activity-actions single-action schedule-only-action">
          <button class="secondary-btn" data-activity-details="${activity.id}">Details</button>
        </div>
      `;
  return `
    <article class="card activity-card ${activity.isPainted ? "painted-activity-card" : ""}" data-activity-card="${escapeHtml(activity.id)}">
      <div class="activity-summary">
        <div>
          <h2>${escapeHtml(activity.title)}</h2>
          <p class="muted activity-description">${escapeHtml(activity.description || "")}</p>
          <div class="meta">
            <span>${icon("location")}${escapeHtml(activity.location)}</span>
          </div>
        </div>
      </div>
      <div class="toolbar" style="margin:0">
        ${activityControls}
      </div>
      ${activity.requiresRegistration && participantState ? renderParticipantsList(participantState) : ""}
    </article>
  `;
}

function renderRegistrationButton(activity) {
  const action = activity.isRegistered ? "Cancel" : "Register";
  const isClosed = state.info?.isActivityRegistrationClosed && !activity.isRegistered;
  const disabled = (activity.isFull && !activity.isRegistered) || isClosed ? "disabled" : "";
  const label = isClosed ? "Closed" : action;
  return `<button class="primary-btn" data-register="${activity.id}" ${disabled}>${label}</button>`;
}

function renderParticipantsList(participantState) {
  if (participantState.isLoading) {
    return `<div class="participant-list"><div class="empty">Loading participants...</div></div>`;
  }

  if (!participantState.users.length) {
    return `<div class="participant-list"><div class="empty">No participants yet.</div></div>`;
  }

  return `
    <div class="participant-list" aria-label="Activity participants">
      ${participantState.users.map(user => `
        <div class="participant-row">
          ${avatar(user)}
          <strong>${escapeHtml(user.fullName)}</strong>
        </div>
      `).join("")}
    </div>
  `;
}

function canDownloadActivityParticipants() {
  return roleName(state.user?.role) === "Admin";
}

function renderActivityDetails(activity) {
  const registrationSummary = activity.requiresRegistration
    ? `${activity.registeredParticipants}/${activity.maxParticipants} registered`
    : "Activity without registration";
  const registrationStatus = activity.requiresRegistration
    ? activity.isRegistered ? "You are registered" : state.info?.isActivityRegistrationClosed ? "Registration is closed" : activity.isFull ? "Registration is full" : "Registration is open"
    : "No registration is required for this activity.";
  const downloadButton = canDownloadActivityParticipants()
    ? `<button class="secondary-btn" type="button" data-download-participants="${activity.id}">Download Activity Participants</button>`
    : "";

  return `
    <section class="page-title">
      <h1>${escapeHtml(activity.title)}</h1>
      <p>${formatDate(activity.date)} &middot; ${escapeHtml(formatTimeRange(activity.startTime, activity.endTime))}</p>
    </section>
    <article class="card activity-card activity-details ${activity.isPainted ? "painted-activity-card" : ""}">
      <div class="meta">
        <span>${escapeHtml(activity.location)}</span>
        <span>${escapeHtml(registrationSummary)}</span>
        <span>${escapeHtml(registrationStatus)}</span>
      </div>
      <p class="muted multiline">${escapeHtml(activity.details || activity.description || "")}</p>
      ${downloadButton ? `<div class="actions activity-detail-actions">${downloadButton}</div>` : ""}
      <dl class="info-list detail-rows">
        <div class="info-row">${icon("schedule")}<span><dt>Date</dt><dd>${formatDate(activity.date)}</dd></span></div>
        <div class="info-row">${icon("clock")}<span><dt>Time</dt><dd>${escapeHtml(formatTimeRange(activity.startTime, activity.endTime))}</dd></span></div>
        <div class="info-row">${icon("location")}<span><dt>Location</dt><dd>${escapeHtml(activity.location)}</dd></span></div>
        <div class="info-row">${icon("info")}<span><dt>Activity type</dt><dd>${activity.requiresRegistration ? "Activity with registration" : "Activity without registration"}</dd></span></div>
        ${activity.requiresRegistration ? `<div class="info-row">${icon("users")}<span><dt>Participants</dt><dd>${activity.registeredParticipants}/${activity.maxParticipants}</dd></span></div>` : ""}
      </dl>
      <div class="actions">
        ${activity.requiresRegistration ? renderRegistrationButton(activity) : ""}
        <button class="secondary-btn" type="button" data-back-schedule>Back to schedule</button>
      </div>
    </article>
  `;
}

function renderInfo() {
  const info = state.info || {};
  const dateRange = [formatDate(info.startDate), formatDate(info.endDate)].filter(Boolean).join(" - ");
  const details = [
    ["Dates", dateRange],
    ["Location", info.location],
    ["Address", info.address],
    ["Contacts", info.contacts]
  ].filter(([, value]) => value);

  return `
    ${renderHero()}
    <section class="grid two info-panels">
      <article class="card info-panel">
        <h2>Event details</h2>
        ${info.description ? `<p class="muted multiline">${escapeHtml(info.description)}</p>` : ""}
        <dl class="info-list">
          ${details.map(([label, value]) => `<div><dt>${label}</dt><dd>${label === "Contacts" ? renderContacts(value) : escapeHtml(value)}</dd></div>`).join("")}
        </dl>
      </article>
      <article class="card info-panel">
        <h2>Additional info</h2>
        ${info.additionalInfo
          ? `<p class="muted multiline">${escapeHtml(info.additionalInfo)}</p>`
          : `<div class="empty">No additional information yet.</div>`}
      </article>
    </section>
  `;
}

function renderAccount() {
  const user = state.user;
  return `
    <section class="page-title">
      <h1>Account</h1>
      <p>Your profile and registered activities.</p>
    </section>
    <section class="grid two">
      <article class="card profile">
        ${avatar(user)}
        <div>
          <h2>${escapeHtml(user?.fullName || "")}</h2>
          <p class="muted">${escapeHtml(user?.email || "")}</p>
          <p><span class="pill">${roleName(user?.role)}</span> <span class="pill">${directoryName(user?.directoryType)}</span></p>
          <div class="actions">
            <button class="secondary-btn" data-edit-profile>Edit profile</button>
            <button class="ghost-btn" data-sign-out>${icon("signout")}Sign out</button>
          </div>
        </div>
      </article>
      <article class="card">
        <h2>My Activities</h2>
        <div class="registered-list">
          ${state.myActivities.length ? state.myActivities.map(renderRegisteredActivity).join("") : `<div class="empty">No registrations yet.</div>`}
        </div>
      </article>
    </section>
  `;
}

function renderRegisteredActivity(activity) {
  const dateParts = formatDate(activity.date).split(" ");
  const participantState = state.activityParticipants[activity.id];
  const participantsLabel = participantState
    ? `<span class="label-wide">Hide participants</span><span class="label-narrow">Hide</span>`
    : "Participants";
  const participantsAriaLabel = participantState ? "Hide participants" : "Show participants";
  return `
    <div class="registered-card ${activity.isPainted ? "painted-activity-card" : ""}">
      <div class="calendar-badge compact">
        <span>${dateParts[1] || ""}</span>
        <strong>${dateParts[2] || ""}</strong>
      </div>
      <div class="registered-content">
        <strong>${escapeHtml(activity.title)}</strong>
        <div class="muted">${formatDate(activity.date)} at ${escapeHtml(formatTime(activity.startTime))}</div>
        <div class="actions activity-actions registered-actions">
          <button class="secondary-btn" data-activity-details="${activity.id}">Details</button>
          <button class="secondary-btn" data-participants="${activity.id}" aria-label="${participantsAriaLabel}" title="${participantsAriaLabel}">${participantsLabel}</button>
        </div>
        ${participantState ? renderParticipantsList(participantState) : ""}
      </div>
    </div>
  `;
}

function renderAdmin() {
  if (roleName(state.user?.role) !== "Admin") {
    return `<div class="empty">Admin access is required.</div>`;
  }
  const info = state.info || {};
  const systemRegistrationClosed = Boolean(info.isSystemRegistrationClosed);
  const activityRegistrationClosed = Boolean(info.isActivityRegistrationClosed);
  return `
    <section class="page-title">
      <h1>Admin</h1>
      <p>Manage event information, activities, and users.</p>
    </section>
    <section class="admin-section card">
      <h2>Event Info</h2>
      <form class="form-grid" id="info-form">
        <label>Title<input name="title" value="${escapeHtml(info.title || "")}"></label>
        <div class="form-grid two-columns">
          <label>Start date<input name="startDate" type="date" value="${escapeHtml(info.startDate || "")}"></label>
          <label>End date<input name="endDate" type="date" value="${escapeHtml(info.endDate || "")}"></label>
        </div>
        <label>Location<input name="location" value="${escapeHtml(info.location || "")}"></label>
        <label>Address<input name="address" value="${escapeHtml(info.address || "")}"></label>
        <label>Contacts<textarea name="contacts">${escapeHtml(info.contacts || "")}</textarea></label>
        <label>Description<textarea name="description">${escapeHtml(info.description || "")}</textarea></label>
        <label>Additional info<textarea name="additionalInfo">${escapeHtml(info.additionalInfo || "")}</textarea></label>
        <div class="logo-upload">
          ${brandMark("logo-preview")}
          <div class="form-grid">
            <label>Logo image from device<input name="logoFile" type="file" accept="image/png,image/jpeg,image/svg+xml,image/webp"></label>
            ${info.logoImageUrl ? `<button class="secondary-btn" type="button" data-remove-logo>Remove logo</button>` : ""}
          </div>
        </div>
        <button class="primary-btn" type="submit">${icon("save")}Save info</button>
      </form>
    </section>
    <section class="admin-section">
      <div class="toolbar">
        <h2>Activities</h2>
        <button class="primary-btn" data-new-activity>${icon("add")}New activity</button>
      </div>
      <div class="grid two">${state.activities.map(renderAdminActivity).join("") || `<div class="empty">No activities yet.</div>`}</div>
    </section>
    <section class="admin-section">
      <h2>Users</h2>
      <div class="grid">${state.allUsers.map(renderAdminUser).join("")}</div>
    </section>
    <section class="admin-section card admin-registration-controls">
      <h2>Registration Controls</h2>
      <div class="registration-control-row">
        <div>
          <strong>System registration</strong>
          <p class="muted">${systemRegistrationClosed ? "New account registration is closed." : "New account registration is open."}</p>
        </div>
        <button class="${systemRegistrationClosed ? "secondary-btn" : "danger-btn"}" data-toggle-system-registration>
          ${systemRegistrationClosed ? "Open system registration" : "Close system registration"}
        </button>
      </div>
      <div class="registration-control-row">
        <div>
          <strong>Activity registration</strong>
          <p class="muted">${activityRegistrationClosed ? "Users cannot register for activities." : "Users can register for activities."}</p>
        </div>
        <button class="${activityRegistrationClosed ? "secondary-btn" : "danger-btn"}" data-toggle-activity-registration>
          ${activityRegistrationClosed ? "Open activity registration" : "Close activity registration"}
        </button>
      </div>
    </section>
  `;
}

function renderAdminActivity(activity) {
  return `
    <article class="card activity-card ${activity.isPainted ? "painted-activity-card" : ""}" data-admin-activity-card="${escapeHtml(activity.id)}">
      <h3>${escapeHtml(activity.title)}</h3>
      <div class="meta"><span>${formatDate(activity.date)}</span><span>${escapeHtml(formatTimeRange(activity.startTime, activity.endTime))}</span></div>
      <div class="actions">
        <button class="secondary-btn" data-paint-activity="${activity.id}" ${activity.isPainted ? "disabled" : ""}>${icon("paint")}Paint</button>
        <button class="secondary-btn" data-edit-activity="${activity.id}">Edit</button>
        <button class="danger-btn" data-delete-activity="${activity.id}">${icon("delete")}Delete</button>
      </div>
      <div class="muted">${activity.requiresRegistration ? `${activity.registeredParticipants}/${activity.maxParticipants} registered` : "No registration required"}</div>
    </article>
  `;
}

function renderAdminUser(user) {
  return `
    <article class="card person-row" data-admin-user-card="${escapeHtml(user.id)}">
      ${avatar(user)}
      <div>
        <h3>${escapeHtml(user.fullName)}</h3>
        <div class="muted">${escapeHtml(user.email)} · ${directoryName(user.directoryType)} · ${user.isActive ? "Active" : "Inactive"}</div>
      </div>
      <button class="secondary-btn" data-admin-user="${user.id}">Edit</button>
    </article>
  `;
}

function bindViewActions() {
  document.querySelectorAll("[data-date]").forEach(button => {
    button.addEventListener("click", () => {
      state.selectedDate = button.dataset.date;
      render();
    });
  });
  document.querySelectorAll("[data-disable-notifications]").forEach(button => {
    button.addEventListener("click", disableEventReminders);
  });
  document.querySelectorAll("[data-register]").forEach(button => {
    button.addEventListener("click", async () => {
      const activity = state.activities.find(item => item.id === button.dataset.register);
      try {
        if (activity.isRegistered) {
          await api(`/api/activities/${activity.id}/registrations`, { method: "DELETE" });
          state.message = "Registration cancelled.";
        } else {
          await api(`/api/activities/${activity.id}/registrations`, { method: "POST" });
          showPopup("Registration", "Registered successfully.");
        }
        delete state.activityParticipants[activity.id];
        await loadRoute();
      } catch (error) {
        setMessage(error.message);
      }
    });
  });
  document.querySelectorAll("[data-participants]").forEach(button => {
    button.addEventListener("click", async () => {
      const activityId = button.dataset.participants;
      if (state.activityParticipants[activityId]) {
        delete state.activityParticipants[activityId];
        render();
        return;
      }

      state.activityParticipants[activityId] = { isLoading: true, users: [] };
      render();
      try {
        const users = await api(`/api/activities/${activityId}/participants`);
        state.activityParticipants[activityId] = { isLoading: false, users };
        render();
      } catch (error) {
        delete state.activityParticipants[activityId];
        setMessage(error.message);
      }
    });
  });
  document.querySelectorAll("[data-activity-details]").forEach(button => {
    button.addEventListener("click", () => {
      const activityId = button.dataset.activityDetails;
      pushContextScreen("activity-details", currentScrollContext("schedule-activity", activityId), { activityId });
      showActivityDetails(activityId);
    });
  });
  document.querySelectorAll("[data-download-participants]").forEach(button => {
    button.addEventListener("click", async () => {
      const activity = state.activities.find(item => item.id === button.dataset.downloadParticipants);
      if (!activity) {
        setMessage("Activity was not found.");
        return;
      }

      try {
        button.disabled = true;
        await downloadActivityParticipantsCsv(activity);
      } catch (error) {
        setMessage(error.message);
      } finally {
        button.disabled = false;
      }
    });
  });
  document.querySelector("[data-sign-out]")?.addEventListener("click", async () => {
    await removePushSubscription();
    try { await api("/api/auth/signout", { method: "POST" }); } catch {}
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REMINDERS_KEY);
    state.token = null;
    state.user = null;
    state.remindersEnabled = false;
    renderAuth("signin", "Signed out.");
  });
  document.querySelector("[data-edit-profile]")?.addEventListener("click", showProfileForm);
  document.querySelector("#info-form")?.addEventListener("submit", saveInfo);
  document.querySelector("[name=avatarFile]")?.addEventListener("change", previewSelectedAvatar);
  document.querySelector("[name=logoFile]")?.addEventListener("change", previewSelectedLogo);
  document.querySelector("[data-remove-logo]")?.addEventListener("click", removeLogo);
  document.querySelector("[data-toggle-system-registration]")?.addEventListener("click", () => toggleRegistrationSetting("isSystemRegistrationClosed"));
  document.querySelector("[data-toggle-activity-registration]")?.addEventListener("click", () => toggleRegistrationSetting("isActivityRegistrationClosed"));
  document.querySelector("[data-new-activity]")?.addEventListener("click", () => showActivityForm());
  document.querySelectorAll("[data-edit-activity]").forEach(button => {
    button.addEventListener("click", () => {
      const activityId = button.dataset.editActivity;
      pushContextScreen("admin-activity-edit", currentScrollContext("admin-activity", activityId), { activityId });
      showActivityForm(state.activities.find(item => item.id === activityId));
    });
  });
  document.querySelectorAll("[data-paint-activity]").forEach(button => {
    button.addEventListener("click", async () => {
      await api(`/api/activities/${button.dataset.paintActivity}/paint`, { method: "POST" });
      state.message = "Activity painted.";
      await loadRoute();
    });
  });
  document.querySelectorAll("[data-delete-activity]").forEach(button => {
    button.addEventListener("click", async () => {
      if (!confirm("Delete this activity?")) return;
      await api(`/api/activities/${button.dataset.deleteActivity}`, { method: "DELETE" });
      state.message = "Activity deleted.";
      await loadRoute();
    });
  });
  document.querySelectorAll("[data-admin-user]").forEach(button => {
    button.addEventListener("click", () => {
      const userId = button.dataset.adminUser;
      pushContextScreen("admin-user-edit", currentScrollContext("admin-user", userId), { userId });
      showUserAdminForm(state.allUsers.find(user => user.id === userId));
    });
  });
}

async function downloadActivityParticipantsCsv(activity) {
  const users = await api(`/api/activities/${activity.id}/participants`);
  const rows = [
    [activity.title],
    ...users.map((user, index) => [index + 1, user.fullName, user.email]),
    ["Total count of participants", users.length]
  ];
  const csv = rows.map(row => row.map(formatCsvCell).join(",")).join("\r\n");
  const blob = new Blob([`\uFEFF${csv}`], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `${toSafeFileName(activity.title)}-participants.csv`;
  document.body.append(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

function formatCsvCell(value = "") {
  const text = String(value);
  return /[",\r\n]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text;
}

function toSafeFileName(value = "activity") {
  const safeName = String(value)
    .trim()
    .replace(/[\\/:*?"<>|]+/g, "-")
    .replace(/\s+/g, "-")
    .replace(/-+/g, "-")
    .replace(/^-|-$/g, "")
    .toLowerCase();
  return safeName || "activity";
}

async function showActivityDetails(activityId) {
  let activity = state.activities.find(item => item.id === activityId);
  try {
    activity = await api(`/api/activities/${activityId}`);
  } catch (error) {
    if (!activity) {
      setMessage(error.message);
      return;
    }
  }

  const index = state.activities.findIndex(item => item.id === activity.id);
  if (index >= 0) {
    state.activities[index] = activity;
  }

  shell(renderActivityDetails(activity));
  bindViewActions();
  scrollToTopAfterRender();
  document.querySelector("[data-back-schedule]")?.addEventListener("click", () => returnToRoute("schedule"));
}

function showProfileForm() {
  const user = state.user;
  shell(`
    <section class="page-title"><h1>Edit Profile</h1></section>
    <form class="card form-grid" id="profile-form">
      <label>Full name<input name="fullName" value="${escapeHtml(user.fullName)}"></label>
      <div class="avatar-upload">
        ${avatar(user)}
        <div class="form-grid">
          <label>Avatar image from device<input name="avatarFile" type="file" accept="image/png,image/jpeg,image/svg+xml,image/webp"></label>
          <label>Avatar URL<input name="avatarUrl" value="${escapeHtml(user.avatarUrl || "")}"></label>
        </div>
      </div>
      <label>Company<input name="company" value="${escapeHtml(user.company || "")}"></label>
      <label>Position<input name="position" value="${escapeHtml(user.position || "")}"></label>
      <label>Bio<textarea name="bio">${escapeHtml(user.bio || "")}</textarea></label>
      <div class="actions">
        <button class="primary-btn" type="submit">${icon("save")}Save</button>
        <button class="secondary-btn" type="button" data-cancel-form>Cancel</button>
      </div>
    </form>
  `);
  document.querySelector("[data-cancel-form]").addEventListener("click", () => returnToRoute("account"));
  document.querySelector("#profile-form").addEventListener("submit", async event => {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    const avatarFile = formData.get("avatarFile");
    let avatarUrl = formData.get("avatarUrl")?.toString() || "";
    if (avatarFile instanceof File && avatarFile.size > 0) {
      const uploadData = new FormData();
      uploadData.append("file", avatarFile);
      const upload = await api("/api/users/me/avatar", {
        method: "POST",
        body: uploadData
      });
      avatarUrl = upload.imageUrl || upload.user?.avatarUrl || avatarUrl;
    }

    const form = {
      fullName: formData.get("fullName")?.toString() || "",
      avatarUrl,
      company: formData.get("company")?.toString() || "",
      position: formData.get("position")?.toString() || "",
      bio: formData.get("bio")?.toString() || ""
    };
    await api("/api/users/me", { method: "PUT", body: JSON.stringify(form) });
    state.message = "Profile saved.";
    state.route = "account";
    await loadRoute();
  });
}

async function saveInfo(event) {
  event.preventDefault();
  const formData = new FormData(event.currentTarget);
  const logoFile = formData.get("logoFile");
  if (logoFile instanceof File && logoFile.size > 0) {
    const uploadData = new FormData();
    uploadData.append("file", logoFile);
    await api("/api/info/logo/upload", {
      method: "POST",
      body: uploadData
    });
  }

  const form = {
    title: formData.get("title")?.toString() || "",
    startDate: formData.get("startDate")?.toString() || state.info?.startDate,
    endDate: formData.get("endDate")?.toString() || state.info?.endDate,
    location: formData.get("location")?.toString() || "",
    address: formData.get("address")?.toString() || "",
    contacts: formData.get("contacts")?.toString() || "",
    description: formData.get("description")?.toString() || "",
    additionalInfo: formData.get("additionalInfo")?.toString() || "",
    isSystemRegistrationClosed: Boolean(state.info?.isSystemRegistrationClosed),
    isActivityRegistrationClosed: Boolean(state.info?.isActivityRegistrationClosed)
  };
  await api("/api/info", {
    method: "PUT",
    body: JSON.stringify({
      ...state.info,
      ...form
    })
  });
  state.message = "Event information saved.";
  await loadRoute();
}

async function toggleRegistrationSetting(key) {
  const current = state.info || await api("/api/info");
  const nextValue = !Boolean(current[key]);
  await api("/api/info", {
    method: "PUT",
    body: JSON.stringify({
      ...current,
      [key]: nextValue
    })
  });
  state.info = { ...current, [key]: nextValue };
  state.message = key === "isSystemRegistrationClosed"
    ? `System registration ${nextValue ? "closed" : "opened"}.`
    : `Activity registration ${nextValue ? "closed" : "opened"}.`;
  await loadRoute();
}

function previewSelectedAvatar(event) {
  const file = event.currentTarget.files?.[0];
  const preview = document.querySelector(".avatar-upload .avatar");
  if (!file || !preview) {
    return;
  }

  const reader = new FileReader();
  reader.addEventListener("load", () => {
    preview.innerHTML = `<img src="${reader.result}" alt="">`;
  });
  reader.readAsDataURL(file);
}

function previewSelectedLogo(event) {
  const file = event.currentTarget.files?.[0];
  const preview = document.querySelector(".logo-preview");
  if (!file || !preview) {
    return;
  }

  const reader = new FileReader();
  reader.addEventListener("load", () => {
    preview.innerHTML = `<img src="${reader.result}" alt="">`;
  });
  reader.readAsDataURL(file);
}

async function removeLogo() {
  await api("/api/info/logo", { method: "DELETE" });
  state.message = "Logo removed.";
  await loadRoute();
}

function showActivityForm(activity = null) {
  shell(`
    <section class="page-title"><h1>${activity ? "Edit Activity" : "New Activity"}</h1></section>
    <form class="card form-grid" id="activity-form">
      <label>Title<input name="title" value="${escapeHtml(activity?.title || "")}" required></label>
      <label>Description<textarea name="description">${escapeHtml(activity?.description || "")}</textarea></label>
      <label>Details<textarea name="details">${escapeHtml(activity?.details || "")}</textarea></label>
      <div class="grid three">
        <label>Date<input name="date" type="date" value="${escapeHtml(activity?.date || "")}" required></label>
        <label>Start<input name="startTime" type="time" value="${escapeHtml((activity?.startTime || "09:00").slice(0, 5))}" required></label>
        <label>End<input name="endTime" type="time" value="${escapeHtml((activity?.endTime || "10:00").slice(0, 5))}" required></label>
      </div>
      <label>Location<input name="location" value="${escapeHtml(activity?.location || "")}" required></label>
      <label>Activity type<select name="requiresRegistration">
        <option value="true" ${activity?.requiresRegistration !== false ? "selected" : ""}>Activity with registration</option>
        <option value="false" ${activity?.requiresRegistration === false ? "selected" : ""}>Activity without registration</option>
      </select></label>
      <label data-participant-limit>Max participants<input name="maxParticipants" type="number" min="1" value="${activity?.maxParticipants || 30}" required></label>
      <div class="actions">
        <button class="primary-btn" type="submit">${icon("save")}Save</button>
        <button class="secondary-btn" type="button" data-cancel-form>Cancel</button>
      </div>
    </form>
  `);
  scrollToTopAfterRender();
  const typeSelect = document.querySelector("[name=requiresRegistration]");
  const participantLimit = document.querySelector("[data-participant-limit]");
  const syncParticipantLimit = () => {
    participantLimit.hidden = typeSelect.value !== "true";
  };
  typeSelect.addEventListener("change", syncParticipantLimit);
  syncParticipantLimit();
  document.querySelector("[data-cancel-form]").addEventListener("click", () => returnToRoute("admin"));
  document.querySelector("#activity-form").addEventListener("submit", async event => {
    event.preventDefault();
    const form = Object.fromEntries(new FormData(event.currentTarget).entries());
    const body = {
      ...form,
      startTime: form.startTime.length === 5 ? `${form.startTime}:00` : form.startTime,
      endTime: form.endTime.length === 5 ? `${form.endTime}:00` : form.endTime,
      maxParticipants: Number(form.maxParticipants),
      requiresRegistration: form.requiresRegistration === "true"
    };
    await api(activity ? `/api/activities/${activity.id}` : "/api/activities", {
      method: activity ? "PUT" : "POST",
      body: JSON.stringify(body)
    });
    state.message = "Activity saved.";
    state.route = "admin";
    await loadRoute();
  });
}

function showUserAdminForm(user) {
  shell(`
    <section class="page-title"><h1>Edit User</h1><p>${escapeHtml(user.fullName)}</p></section>
    <form class="card form-grid" id="user-admin-form">
      <label>Directory type
        <select name="directoryType">
          ${DIRECTORY_TYPES.map((type, index) => `<option value="${index}" ${Number(user.directoryType) === index || user.directoryType === type ? "selected" : ""}>${type}</option>`).join("")}
        </select>
      </label>
      <label>Role
        <select name="role">
          <option value="0" ${roleName(user.role) === "User" ? "selected" : ""}>User</option>
          <option value="1" ${roleName(user.role) === "Admin" ? "selected" : ""}>Admin</option>
        </select>
      </label>
      <label>Account status
        <select name="isActive">
          <option value="true" ${user.isActive ? "selected" : ""}>Active</option>
          <option value="false" ${!user.isActive ? "selected" : ""}>Inactive</option>
        </select>
      </label>
      <div class="actions">
        <button class="primary-btn" type="submit">${icon("save")}Save</button>
        <button class="danger-btn" type="button" data-delete-user="${user.id}">${icon("delete")}Delete</button>
        <button class="secondary-btn" type="button" data-cancel-form>Cancel</button>
      </div>
    </form>
  `);
  scrollToTopAfterRender();
  document.querySelector("[data-cancel-form]").addEventListener("click", () => returnToRoute("admin"));
  document.querySelector("[data-delete-user]").addEventListener("click", async () => {
    if (!confirm("Delete this user and their registrations?")) return;
    await api(`/api/users/${user.id}`, { method: "DELETE" });
    state.message = "User deleted.";
    state.route = "admin";
    await loadRoute();
  });
  document.querySelector("#user-admin-form").addEventListener("submit", async event => {
    event.preventDefault();
    const form = Object.fromEntries(new FormData(event.currentTarget).entries());
    await api(`/api/users/${user.id}/admin`, {
      method: "PUT",
      body: JSON.stringify({ role: Number(form.role), directoryType: Number(form.directoryType), isActive: form.isActive === "true" })
    });
    state.message = "User updated.";
    state.route = "admin";
    await loadRoute();
  });
}

async function showRouteFromHistory(route, restore = null) {
  state.route = route;
  state.message = "";
  render();
  scheduleNavigationRestore(restore);
  await loadRoute();
  scheduleNavigationRestore(restore);
}

async function showHistoryScreen(navState) {
  const route = navState?.route || routeFromUrl() || "schedule";
  if (!navState?.[NAV_STATE_KEY] || navState.screen === "route") {
    await showRouteFromHistory(route, navState?.restore);
    return;
  }

  state.route = route;
  await loadRoute();

  if (navState.screen === "activity-details" && navState.activityId) {
    await showActivityDetails(navState.activityId);
    return;
  }

  if (navState.screen === "admin-activity-edit" && navState.activityId) {
    const activity = state.activities.find(item => item.id === navState.activityId);
    if (activity) {
      showActivityForm(activity);
      return;
    }
    await showRouteFromHistory("admin", navState.restore);
    return;
  }

  if (navState.screen === "admin-user-edit" && navState.userId) {
    const user = state.allUsers.find(item => item.id === navState.userId);
    if (user) {
      showUserAdminForm(user);
      return;
    }
    await showRouteFromHistory("admin", navState.restore);
    return;
  }

  await showRouteFromHistory(route, navState.restore);
}

window.addEventListener("popstate", event => {
  showHistoryScreen(event.state);
});

window.addEventListener("hashchange", () => {
  const route = routeFromUrl();
  if (!route || route === state.route) {
    return;
  }

  history.replaceState(routeHistoryState(route), "", routeUrl(route));
  showRouteFromHistory(route);
});

bootstrap();
