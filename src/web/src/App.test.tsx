import { render, screen } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { App } from './App';

afterEach(() => vi.unstubAllGlobals());

it('Given missing article data, when loading fails, then show a useful error', async () => {
  vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('Missing')));
  render(<App />);
  expect(await screen.findByRole('alert')).toHaveTextContent('Unable to load articles');
});

it('Given valid empty data, when loading, then show an empty dashboard', async () => {
  vi.stubGlobal('fetch', vi.fn().mockImplementation((url: string) => Promise.resolve({ok:true,
    json:() => Promise.resolve(url.includes('metadata') ? {} : [])})));
  render(<App />);
  expect(screen.getByRole('status')).toHaveTextContent('Loading');
  expect(await screen.findByText('No recent articles are available yet.')).toBeInTheDocument();
});
