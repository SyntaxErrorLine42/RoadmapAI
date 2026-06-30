import { Injectable, signal, effect } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  // Persists in localStorage so it survives page refresh
  private readonly storageKey = 'roadmapai-dark-mode';
  readonly isDark = signal(this.loadInitial());

  constructor() {
    // Sync the .dark class on <html> whenever the signal changes
    effect(() => {
      const dark = this.isDark();
      document.documentElement.classList.toggle('dark', dark);
      localStorage.setItem(this.storageKey, String(dark));
    });
  }

  toggle(): void {
    this.isDark.update(v => !v);
  }

  private loadInitial(): boolean {
    const stored = localStorage.getItem(this.storageKey);
    if (stored !== null) return stored === 'true';
    // Fall back to OS preference
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
  }
}
