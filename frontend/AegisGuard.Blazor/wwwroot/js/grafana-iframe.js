window.aegisGrafana = window.aegisGrafana || {};

// Fallback: if the load event was missed, hide the spinner after ms
window.aegisGrafana.failSafeHide = (ms, dotnetRef) => {
  setTimeout(() => {
    try { dotnetRef?.invokeMethodAsync('IframeLoaded'); } catch {}
  }, ms || 1500);
};
