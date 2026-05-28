import { expect, test } from '@playwright/test';

test('dashboard loads and Blazor actions respond', async ({ page }) => {
  await page.goto('/');

  await expect(page.getByRole('heading', { name: /AI-assisted music discovery for Lidarr/i })).toBeVisible();
  await expect(page.getByText('First-run checklist')).toBeVisible();

  await page.getByRole('button', { name: 'Scan Library' }).click();
  await expect(page.getByText('Library scan queued.')).toBeVisible();

  await page.getByRole('button', { name: 'Generate Local Recommendations' }).click();
  await expect(page.getByText(/Generated \d+ local recommendations\./)).toBeVisible();
});

test('navigation and settings tabs are interactive', async ({ page }) => {
  await page.goto('/');

  await page.getByRole('link', { name: 'Settings' }).click();
  await expect(page.getByRole('heading', { name: 'Settings' })).toBeVisible();

  await page.getByRole('tab', { name: 'AI Provider' }).click();
  await expect(page.getByText('Enable AI recommendations')).toBeVisible();

  await page.getByRole('button', { name: 'Save settings' }).click();
  await expect(page.getByText('Settings saved.')).toBeVisible();
});

test('critical Blazor and MudBlazor assets are served', async ({ request }) => {
  await expect((await request.get('/_framework/blazor.web.js')).ok()).toBeTruthy();
  await expect((await request.get('/_content/MudBlazor/MudBlazor.min.js')).ok()).toBeTruthy();
});
