const CACHE_NAME = "eventapp-pwa-v20260716-event-icon";
const APP_SHELL = [
  "/",
  "/index.html",
  "/styles.css?v=20260703-contact-links",
  "/app.js?v=20260703-contact-links",
  "/manifest.webmanifest",
  "/images/default-logo.png",
  "/icons/icon-512.svg"
];

self.addEventListener("install", event => {
  event.waitUntil(caches.open(CACHE_NAME).then(cache => cache.addAll(APP_SHELL)));
  self.skipWaiting();
});

self.addEventListener("activate", event => {
  event.waitUntil(
    caches.keys().then(keys =>
      Promise.all(keys.filter(key => key !== CACHE_NAME).map(key => caches.delete(key)))
    )
  );
  self.clients.claim();
});

self.addEventListener("fetch", event => {
  const request = event.request;
  const url = new URL(request.url);

  if (url.pathname.startsWith("/api/") || request.method !== "GET") {
    return;
  }

  if (request.mode === "navigate") {
    event.respondWith(fetch(request).catch(() => caches.match("/index.html")));
    return;
  }

  if (url.pathname === "/index.html" ||
      url.pathname === "/app.js" ||
      url.pathname === "/styles.css" ||
      url.pathname === "/service-worker.js") {
    event.respondWith(
      fetch(request)
        .then(response => {
          const copy = response.clone();
          caches.open(CACHE_NAME).then(cache => cache.put(request, copy));
          return response;
        })
        .catch(() => caches.match(request))
    );
    return;
  }

  event.respondWith(
    caches.match(request).then(cached =>
      cached || fetch(request).then(response => {
        const copy = response.clone();
        caches.open(CACHE_NAME).then(cache => cache.put(request, copy));
        return response;
      })
    )
  );
});

self.addEventListener("push", event => {
  let payload = {};
  try {
    payload = event.data?.json() || {};
  } catch {
    payload = { body: event.data?.text() || "" };
  }

  const title = payload.title || "Event reminder";
  const options = {
    body: payload.body || "An event you registered for starts soon.",
    icon: "/icons/icon-512.svg",
    badge: "/icons/icon-192.svg",
    tag: payload.tag || "eventapp-reminder",
    data: { url: payload.url || "/#schedule" }
  };

  event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener("notificationclick", event => {
  event.notification.close();

  const targetUrl = new URL(event.notification.data?.url || "/#schedule", self.location.origin).href;
  event.waitUntil(
    clients.matchAll({ type: "window", includeUncontrolled: true }).then(clientList => {
      const matchingClient = clientList.find(client => client.url.startsWith(self.location.origin));
      if (matchingClient) {
        matchingClient.navigate(targetUrl);
        return matchingClient.focus();
      }

      return clients.openWindow(targetUrl);
    })
  );
});
