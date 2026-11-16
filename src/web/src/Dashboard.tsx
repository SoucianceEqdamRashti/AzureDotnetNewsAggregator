import { useState } from 'react';
import { filterArticles, type Article } from './articles';
import { loadReadingState, saveReadingState, type ReadingState } from './reading-state';

export interface Label { id: string; name: string }
export interface DashboardProps {
  articles: Article[];
  sources: Label[];
  categories: Label[];
  lastSuccessfulIngestion: string | null;
  previousVisit: string | null;
}

export function Dashboard({ articles, sources, categories, lastSuccessfulIngestion, previousVisit }: DashboardProps) {
  const [category, setCategory] = useState('');
  const [source, setSource] = useState('');
  const [query, setQuery] = useState('');
  const [reading, setReading] = useState(loadReadingState);
  const [mode, setMode] = useState('all');
  const [storageWarning, setStorageWarning] = useState(false);
  const labels = new Map([...categories, ...sources].map(item => [item.id, item.name]));
  const visible = filterArticles(articles, category, source, query).filter(article =>
    mode === 'all' || (mode === 'unread' ? !reading.read.includes(article.id) : reading.bookmarks.includes(article.id)));

  function toggle(key: keyof ReadingState, id: string) {
    const next = { ...reading, [key]: reading[key].includes(id)
      ? reading[key].filter(item => item !== id) : [...reading[key], id] };
    setReading(next);
    setStorageWarning(!saveReadingState(next));
  }

  return <main>
    <header>
      <p className="eyebrow">THE LAST 30 DAYS</p>
      <h1>Azure &amp; .NET News</h1>
      <p>Updates for developers, in one place.</p>
      <p className="muted">Last successful update: {lastSuccessfulIngestion
        ? <time dateTime={lastSuccessfulIngestion}>{new Date(lastSuccessfulIngestion).toLocaleString()}</time>
        : 'Not available'}</p>
    </header>
    <section className="filters" aria-label="Article filters">
      <label className="search">Search articles<input type="search" value={query}
        placeholder="Search titles and summaries" onChange={event => setQuery(event.target.value)} /></label>
      <label>Category<select value={category} onChange={event => setCategory(event.target.value)}>
        <option value="">All categories</option>
        {categories.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
      </select></label>
      <label>Source<select value={source} onChange={event => setSource(event.target.value)}>
        <option value="">All sources</option>
        {sources.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
      </select></label>
      <label>Show<select value={mode} onChange={event => setMode(event.target.value)}>
        <option value="all">All articles</option><option value="unread">Unread</option><option value="bookmarks">Bookmarks</option>
      </select></label>
    </section>
    {storageWarning && <p role="status">Browser storage is unavailable. Changes will last for this visit only.</p>}
    <p className="result-count" aria-live="polite">{visible.length} articles · Newest first</p>
    {visible.length === 0 && <p className="empty">{articles.length === 0
      ? 'No recent articles are available yet.' : 'No articles match your filters.'}</p>}
    <section aria-label="News articles" className="articles">
      {visible.map(article => {
        const read = reading.read.includes(article.id);
        return <article key={article.id} className={read ? 'read' : ''}>
          <div className="article-meta"><span>{labels.get(article.source) ?? article.source}</span>
            <time dateTime={article.publishedAt}>{new Date(article.publishedAt).toLocaleDateString()}</time>
            {previousVisit && Date.parse(article.publishedAt) > Date.parse(previousVisit)
              && <span className="new">New since last visit</span>}
          </div>
          <h2><a href={article.url} target="_blank" rel="noopener noreferrer">{article.title}</a></h2>
          <p className="summary">{article.summary}</p>
          <div className="tags">{article.categories.map(id => <span key={id}>{labels.get(id) ?? id}</span>)}</div>
          <div className="article-actions">
            <button type="button" aria-pressed={read} onClick={() => toggle('read', article.id)}>
              {read ? 'Mark as unread' : 'Mark as read'}</button>
            <button type="button" aria-pressed={reading.bookmarks.includes(article.id)}
              onClick={() => toggle('bookmarks', article.id)}>
              {reading.bookmarks.includes(article.id) ? 'Remove bookmark' : 'Bookmark'}</button>
          </div>
        </article>;
      })}
    </section>
    <footer>Links open the original publisher. Reading preferences stay in this browser.</footer>
  </main>;
}
