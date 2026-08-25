import { defineConfig, devices } from '@playwright/test';
import { fileURLToPath } from 'node:url';

const artifactRoot = fileURLToPath(new URL('../output/playwright/', import.meta.url));
const baseURL = process.env.E2E_BASE_URL || 'http://127.0.0.1:4173';
const recordDefense = process.env.E2E_RECORD === '1';
const reportFolder = recordDefense ? 'defense-recording-report' : 'html-report';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1,
  retries: process.env.CI ? 1 : 0,
  timeout: 60_000,
  expect: { timeout: 12_000 },
  outputDir: `${artifactRoot}test-results`,
  reporter: [
    ['list'],
    ['html', { outputFolder: `${artifactRoot}${reportFolder}`, open: 'never' }]
  ],
  use: {
    baseURL,
    locale: 'zh-CN',
    timezoneId: 'Asia/Shanghai',
    colorScheme: 'dark',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: recordDefense ? { mode: 'on', size: { width: 1920, height: 1080 } } : 'retain-on-failure'
  },
  projects: recordDefense
    ? [
        {
          name: 'defense-recording',
          testMatch: /defense-flow\.spec\.ts/,
          use: {
            ...devices['Desktop Chrome'],
            viewport: { width: 1920, height: 1080 }
          }
        }
      ]
    : [
        {
          name: 'desktop-chromium',
          testIgnore: /defense-flow\.spec\.ts/,
          use: { ...devices['Desktop Chrome'], viewport: { width: 1440, height: 900 } }
        },
        {
          name: 'mobile-chromium',
          testIgnore: /defense-flow\.spec\.ts/,
          use: { ...devices['Pixel 7'] }
        },
        {
          name: 'defense-chromium',
          testMatch: /defense-flow\.spec\.ts/,
          dependencies: ['desktop-chromium', 'mobile-chromium'],
          use: { ...devices['Desktop Chrome'], viewport: { width: 1440, height: 900 } }
        }
      ],
  webServer: process.env.E2E_NO_WEBSERVER === '1'
    ? undefined
    : {
        command: 'npm run dev -- --port 4173',
        url: baseURL,
        reuseExistingServer: true,
        timeout: 120_000
      }
});
