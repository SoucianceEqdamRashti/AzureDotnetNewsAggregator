import { useEffect, useState } from 'react';
import { Dashboard, type DashboardProps, type Label } from './Dashboard';
import { parseArticles } from './articles';
import { beginVisit } from './reading-state';

function labels(value: unknown): Label[] {
  if (!Array.isArray(value)) {
    return [];
  }
  return value.filter((entry: unknown): entry is Label => Boolean(entry) && typeof entry === 'object'
    && 'id' in entry! && typeof entry.id === 'string' && 'name' in entry && typeof entry.name === 'string');
}

async function load(name: string, signal: AbortSignal): Promise<unknown> {
  const response = await fetch(`${import.meta.env.BASE_URL}${name}.json`, {signal});
  if (!response.ok) {
    throw new Error(`Unable to load ${name}`);
  }
  return response.json();
}

export function App() {
  const [data, setData] = useState<DashboardProps | null>(null);
  const [error, setError] = useState(false);
  useEffect(() => {
    const controller = new AbortController();
    const signal = controller.signal;
    Promise.all([
      load('articles', signal), load('sources', signal).catch(() => []),
      load('categories', signal).catch(() => []), load('metadata', signal).catch(() => null),
    ]).then(([rawArticles, rawSources, rawCategories, metadata]) => {
      if (signal.aborted) {
        return;
      }
      if (!Array.isArray(rawArticles)) {
        throw new Error('Invalid article dataset');
      }
      const timestamp = metadata && typeof metadata === 'object' && 'lastSuccessfulIngestion' in metadata
        && typeof metadata.lastSuccessfulIngestion === 'string' ? metadata.lastSuccessfulIngestion : null;
      setData({ articles: parseArticles(rawArticles), sources: labels(rawSources), categories: labels(rawCategories),
        lastSuccessfulIngestion: timestamp && Number.isFinite(Date.parse(timestamp)) ? timestamp : null,
        previousVisit: beginVisit(new Date().toISOString()) });
    }).catch(() => {
      if (!signal.aborted) {
        setError(true);
      }
    });
    return () => controller.abort();
  }, []);

  if (error) {
    return <main><h1>Azure &amp; .NET News</h1><p role="alert">Unable to load articles. Please try again later.</p></main>;
  }
  if (!data) {
    return <main><p role="status">Loading articles…</p></main>;
  }
  return <Dashboard {...data} />;
}
