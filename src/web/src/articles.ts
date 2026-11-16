export interface Article {
  id: string;
  title: string;
  url: string;
  source: string;
  publishedAt: string;
  summary: string;
  categories: string[];
}

export function parseArticles(value: unknown): Article[] {
  if (!Array.isArray(value)) {
    return [];
  }
  return value.filter((entry: unknown): entry is Article => {
    if (!entry || typeof entry !== 'object') {
      return false;
    }
    const item = entry as Record<string, unknown>;
    if (!['id','title','url','source','publishedAt','summary'].every(key => typeof item[key] === 'string')
      || !Array.isArray(item.categories) || !item.categories.every(category => typeof category === 'string')
      || !item.id || !item.title || !Number.isFinite(Date.parse(String(item.publishedAt)))) {
      return false;
    }
    try {
      const url = new URL(String(item.url));
      return ['http:', 'https:'].includes(url.protocol) && !url.username && !url.password;
    } catch {
      return false;
    }
  });
}

export function filterArticles(articles: Article[], category: string, source: string, query: string): Article[] {
  const keyword = query.trim().toLocaleLowerCase();
  return articles.filter(article => (!category || article.categories.includes(category))
    && (!source || article.source === source)
    && `${article.title} ${article.summary} ${article.source}`.toLocaleLowerCase().includes(keyword))
    .sort((a,b) => Date.parse(b.publishedAt) - Date.parse(a.publishedAt) || a.id.localeCompare(b.id));
}
