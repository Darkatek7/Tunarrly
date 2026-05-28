import { defineConfig, devices } from '@playwright/test';

const dotnetConfiguration = process.env.CI ? 'Release' : 'Debug';

export default defineConfig({
  testDir: './tests/browser',
  timeout: 30_000,
  expect: {
    timeout: 10_000
  },
  use: {
    baseURL: 'http://127.0.0.1:5107',
    trace: 'on-first-retry'
  },
  webServer: {
    command: `bash -lc "rm -rf /tmp/tunarrly-browser && mkdir -p /tmp/tunarrly-browser/music && DATABASE__PATH=/tmp/tunarrly-browser/tunarrly.db LIBRARY__PATH=/tmp/tunarrly-browser/music LIDARR__BASEURL=http://127.0.0.1:9 LIDARR__APIKEY=browser-test AI__ENABLED=false ASPNETCORE_ENVIRONMENT=Development dotnet run --project Tunarrly.Web --no-build --configuration ${dotnetConfiguration} --urls http://127.0.0.1:5107"`,
    url: 'http://127.0.0.1:5107/health',
    reuseExistingServer: false,
    timeout: 120_000
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] }
    }
  ]
});
