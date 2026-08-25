import { spawnSync } from 'node:child_process';
import { copyFileSync, existsSync, mkdirSync, readdirSync, statSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const frontendRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const repositoryRoot = resolve(frontendRoot, '..');
const artifactRoot = join(repositoryRoot, 'output', 'playwright');
const target = process.env.E2E_SSH_TARGET;
const key = process.env.E2E_SSH_KEY;
const baseURL = process.env.E2E_BASE_URL || 'http://124.222.213.245';
const record = process.argv.includes('--record');
const defenseOnly = process.argv.includes('--defense-only');

if (!target || !key) {
  console.error('E2E_SSH_TARGET 与 E2E_SSH_KEY 必须指向有权执行云端演示数据恢复的 SSH 目标和私钥。');
  process.exit(2);
}

function run(command, args, options = {}) {
  const result = spawnSync(command, args, {
    cwd: frontendRoot,
    stdio: 'inherit',
    shell: false,
    ...options
  });
  if (result.error) throw result.error;
  return result.status ?? 1;
}

function reset(actor) {
  const code = run('ssh', [
    '-i', key,
    '-o', 'BatchMode=yes',
    '-o', 'StrictHostKeyChecking=accept-new',
    target,
    'sudo', '/opt/steam-platform/bin/reset-demo-data', actor
  ]);
  if (code !== 0) throw new Error(`云端演示数据恢复失败，退出码 ${code}`);
}

function newestVideo(directory) {
  if (!existsSync(directory)) return null;
  const candidates = [];
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const fullPath = join(directory, entry.name);
    if (entry.isDirectory()) {
      const nested = newestVideo(fullPath);
      if (nested) candidates.push(nested);
    } else if (entry.name.endsWith('.webm')) {
      candidates.push(fullPath);
    }
  }
  return candidates.sort((a, b) => statSync(b).mtimeMs - statSync(a).mtimeMs)[0] || null;
}

let status = 1;
reset(record ? 'playwright-recording-before' : 'playwright-e2e-before');
try {
  const cli = join(frontendRoot, 'node_modules', '@playwright', 'test', 'cli.js');
  const playwrightArgs = [cli, 'test'];
  if (defenseOnly) playwrightArgs.push('e2e/defense-flow.spec.ts', '--project=defense-chromium', '--no-deps');
  status = run(process.execPath, playwrightArgs, {
    env: {
      ...process.env,
      E2E_BASE_URL: baseURL,
      E2E_NO_WEBSERVER: '1',
      E2E_MUTATING: '1',
      E2E_RECORD: record ? '1' : '0'
    }
  });

  if (status === 0 && record) {
    const video = newestVideo(join(artifactRoot, 'raw-video'));
    if (!video) throw new Error('录制模式通过，但没有找到 Playwright WebM 视频。');
    const destination = join(artifactRoot, 'defense-recording', 'steam-platform-defense-demo.webm');
    mkdirSync(dirname(destination), { recursive: true });
    copyFileSync(video, destination);
    console.log(`答辩备用录屏：${destination}`);
  }
} finally {
  try {
    reset(record ? 'playwright-recording-after' : 'playwright-e2e-after');
  } catch (error) {
    console.error(error);
    status = 1;
  }
}

process.exit(status);
