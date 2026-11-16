import { describe, expect, it } from 'vitest';
import { filterArticles, parseArticles, type Article } from './articles';

const first: Article = { id:'a', title:'Azure agents', url:'https://example.com/a', source:'one',
  publishedAt:'2026-11-16T08:00:00Z', summary:'Foundry news', categories:['azure','ai-agents'] };
const second: Article = { ...first, id:'b', title:'.NET update', source:'two',
  publishedAt:'2026-11-15T08:00:00Z', categories:['dotnet-csharp'] };

describe('article data', () => {
  it('Given combined filters, when searched, then match all criteria', () => {
    expect(filterArticles([second,first], 'azure', 'one', 'FOUNDRY')).toEqual([first]);
    expect(filterArticles([first], 'azure', 'two', '')).toEqual([]);
  });
  it('Given unsorted articles, when displayed, then sort newest first without mutation', () => {
    const input = [second,first];
    expect(filterArticles(input, '', '', '')).toEqual([first,second]);
    expect(input).toEqual([second,first]);
  });
  it('Given invalid entries, when parsed, then exclude unsafe and malformed data', () => {
    expect(parseArticles([first, {...first,url:'javascript:alert(1)'}, {...first,publishedAt:'bad'}, {}])).toEqual([first]);
    expect(parseArticles(null)).toEqual([]);
  });
});
