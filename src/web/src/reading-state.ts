export interface ReadingState { read: string[]; bookmarks: string[] }

export function loadReadingState(): ReadingState {
  try {
    const value: unknown = JSON.parse(localStorage.getItem('news:reading') ?? '{}');
    if (value && typeof value === 'object') {
      const state = value as Record<string, unknown>;
      const strings = (items: unknown): string[] => Array.isArray(items)
        ? items.filter((item: unknown): item is string => typeof item === 'string') : [];
      return { read: strings(state.read), bookmarks: strings(state.bookmarks) };
    }
  } catch { /* Browser storage is optional. */ }
  return { read: [], bookmarks: [] };
}

export function saveReadingState(state: ReadingState): boolean {
  try {
    localStorage.setItem('news:reading', JSON.stringify(state));
    return true;
  } catch {
    return false;
  }
}

export function beginVisit(now: string): string | null {
  try {
    const previous = localStorage.getItem('news:lastVisit');
    localStorage.setItem('news:lastVisit', now);
    return previous && Number.isFinite(Date.parse(previous)) ? previous : null;
  } catch {
    return null;
  }
}
