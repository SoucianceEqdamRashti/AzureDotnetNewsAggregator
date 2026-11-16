import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';

const base = process.env.BASE_PATH || '/';
const html = await fs.readFile('dist/index.html','utf8');
const assets = [...html.matchAll(/(?:src|href)="([^"]+)"/g)]
  .map(match => match[1]).filter(value => value.includes('/assets/'));
assert(assets.length > 0, 'Build has no asset references');
for (const asset of assets) {
  assert(asset.startsWith(base), `Asset does not use base ${base}: ${asset}`);
  await fs.access(path.join('dist',asset.slice(base.length)));
}
for (const name of ['articles','metadata','sources','categories']) {
  const source = await fs.readFile(`../data/${name}.json`,'utf8');
  const built = await fs.readFile(`dist/${name}.json`,'utf8');
  assert.equal(built,source,`Published ${name} differs from collector data`);
  JSON.parse(built);
}
console.log(`Verified asset base ${base} and all four published JSON files.`);
