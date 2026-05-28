import { expect, Page, test } from '@playwright/test';

async function signIn(page: Page) {
  await page.goto('/');
  await expect(page.getByLabel('Username')).toBeVisible();
  await page.getByLabel('Username').fill('admin');
  await page.getByLabel('Password').fill('Test1234.');
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page.getByRole('heading', { name: /AI-assisted music discovery for Lidarr/i })).toBeVisible();
}

test('built-in login signs in and returns to the requested page', async ({ page }) => {
  await page.goto('/settings');
  await expect(page).toHaveURL(/\/login\?returnUrl=%2Fsettings/);
  await expect(page.locator('.login-shell')).toHaveCSS('display', 'grid');

  await page.getByLabel('Username').fill('admin');
  await page.getByLabel('Password').fill('Test1234.');
  await page.getByRole('button', { name: 'Sign in' }).click();

  await expect(page).toHaveURL(/\/settings$/);
  await expect(page.getByRole('heading', { name: 'Settings' })).toBeVisible();
});

test('dashboard loads and Blazor actions respond', async ({ page }) => {
  await signIn(page);

  await expect(page.getByRole('heading', { name: /AI-assisted music discovery for Lidarr/i })).toBeVisible();
  await expect(page.getByText('First-run checklist')).toBeVisible();

  await page.getByRole('button', { name: 'Scan Library' }).click();
  await page.getByRole('link', { name: 'Scan Jobs' }).click();
  await expect(page.getByRole('heading', { name: 'Scan Jobs' })).toBeVisible();
  await page.getByRole('link', { name: 'Dashboard' }).click();

  await page.getByRole('button', { name: 'Generate Local Recommendations' }).click();
  await expect(page.getByText(/Generated \d+ local recommendations\./)).toBeVisible();
});

test('navigation and settings tabs are interactive', async ({ page }) => {
  await signIn(page);

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
