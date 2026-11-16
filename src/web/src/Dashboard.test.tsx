import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, expect, it, vi } from 'vitest';
import { Dashboard, type DashboardProps } from './Dashboard';
import { beginVisit, loadReadingState, saveReadingState } from './reading-state';

const props: DashboardProps = {
  articles:[{id:'a',title:'Azure agents',url:'https://example.com/a',source:'one',
    publishedAt:'2026-11-16T08:00:00Z',summary:'Foundry news',categories:['azure']}],
  sources:[{id:'one',name:'Source one'}], categories:[{id:'azure',name:'Azure'}],
  lastSuccessfulIngestion:'2026-11-16T09:00:00Z', previousVisit:'2026-11-15T08:00:00Z',
};

beforeEach(() => { localStorage.clear(); vi.restoreAllMocks(); });

it('Given an article, when read and bookmarked, then persist both across mounts', async () => {
  const user = userEvent.setup();
  const view = render(<Dashboard {...props} />);
  await user.click(screen.getByRole('button',{name:'Mark as read'}));
  await user.click(screen.getByRole('button',{name:'Bookmark'}));
  view.unmount();
  render(<Dashboard {...props} />);
  expect(screen.getByRole('button',{name:'Mark as unread'})).toBeInTheDocument();
  expect(screen.getByRole('button',{name:'Remove bookmark'})).toBeInTheDocument();
  expect(screen.getByText('New since last visit')).toBeInTheDocument();
});

it('Given search excludes all articles, when typing, then show an empty result message', async () => {
  render(<Dashboard {...props} />);
  await userEvent.setup().type(screen.getByRole('searchbox',{name:'Search articles'}),'unmatched');
  expect(screen.getByText('No articles match your filters.')).toBeInTheDocument();
});

it('Given corrupt or unavailable storage, when loading and saving, then remain usable', () => {
  localStorage.setItem('news:reading', '{');
  expect(loadReadingState()).toEqual({read:[],bookmarks:[]});
  vi.spyOn(Storage.prototype,'setItem').mockImplementation(() => { throw new Error('Storage blocked'); });
  expect(saveReadingState({read:['a'],bookmarks:[]})).toBe(false);
});

it('Given a previous visit, when beginning a visit, then return the old baseline and store the new one', () => {
  localStorage.setItem('news:lastVisit','2026-11-15T08:00:00Z');
  expect(beginVisit('2026-11-16T08:00:00Z')).toBe('2026-11-15T08:00:00Z');
  expect(localStorage.getItem('news:lastVisit')).toBe('2026-11-16T08:00:00Z');
});
