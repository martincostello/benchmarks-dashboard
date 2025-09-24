'use strict';

(() => {
  // Theme setter with optional persistence (default true)
  window._setBenchmarkTheme = function (theme, persist) {
    if (persist === undefined) {
      persist = true;
    }
    if (persist) {
      localStorage.setItem('theme', theme);
    }

    const themeLinks = {
      dark: document.getElementById('theme-dark'),
      light: document.getElementById('theme-light'),
    };

    const isDark = theme === 'dark';
    const activeTheme = isDark ? themeLinks.dark : themeLinks.light;
    const inactiveTheme = isDark ? themeLinks.light : themeLinks.dark;

    activeTheme.setAttribute('media', 'all');
    inactiveTheme.setAttribute('media', 'not all');

    document.documentElement.setAttribute('data-bs-theme', theme);

    // Helper to update the toggle icon
    let depth = 0;
    const maxDepth = 128;

    const updateIcon = () => {
      const toUpdate = document.querySelector('#toggle-theme span');
      const darkIcon = 'fa-moon';
      const lightIcon = 'fa-sun';
      if (toUpdate) {
        toUpdate.classList.remove(darkIcon);
        toUpdate.classList.remove(lightIcon);
        toUpdate.classList.add(isDark ? lightIcon : darkIcon);
      } else if (depth++ < maxDepth) {
        // During page load, the icon may not be present yet
        requestAnimationFrame(updateIcon);
      }
    };

    updateIcon();
  };

  // Use stored preference if present, otherwise follow the system preference
  const storedTheme = localStorage.getItem('theme');
  const systemPrefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
  const initialTheme = storedTheme || (systemPrefersDark ? 'dark' : 'light');
  // Don't persist when following system preference by default
  window._setBenchmarkTheme(initialTheme, !!storedTheme);
})();
