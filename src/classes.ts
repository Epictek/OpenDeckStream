import { findModule } from "decky-frontend-lib";

export const mediaPageClasses = findModule((mod) => {
  if (typeof mod !== 'object') return false;

  if (mod.ScreenshotGrid && mod.ScreenshotHeaderBanner) {
    return true;
  }

  return false;
});

export const gamepadTabbedPageClasses = findModule((mod) => {
  if (typeof mod !== 'object') return false;

  if (mod.TabCount && mod.TabTitle) {
    return true;
  }

  return false;
});