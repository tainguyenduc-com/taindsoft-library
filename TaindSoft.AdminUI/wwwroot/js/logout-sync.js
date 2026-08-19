// logout-sync.js - cross-tab logout sync via the window `storage` event.
// Serves as _content/TaindSoft.AdminUI/js/logout-sync.js
//
// The `storage` event fires in OTHER tabs of the same origin when this tab
// writes to localStorage. It does NOT fire in the tab that made the change,
// so the initiating tab performs its own local logout while every other tab
// receives the event and logs out through the callback.

let initialized = false;

export function initLogoutSync(dotNetRef, key) {
  if (initialized || !dotNetRef || !key) return;
  initialized = true;

  window.addEventListener('storage', (event) => {
    if (event.key === key && event.newValue) {
      dotNetRef.invokeMethodAsync('OnCrossTabLogout');
    }
  });
}
