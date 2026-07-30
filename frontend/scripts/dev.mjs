import { spawn, spawnSync } from 'node:child_process'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const frontendDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')
const backendDir = path.resolve(frontendDir, '../backend')
const viteEntry = path.resolve(frontendDir, 'node_modules/vite/bin/vite.js')
const children = []
let stopping = false

const services = [
  { name: 'UserAPI', port: 5054, project: 'UserAPI/UserAPI.csproj', healthPath: '/swagger/index.html' },
  { name: 'BannerAPI', port: 5127, project: 'BannerAPI/BannerAPI.csproj', healthPath: '/swagger/index.html' },
  { name: 'ComicAPI', port: 5023, project: 'ComicAPI/ComicAPI.csproj', healthPath: '/swagger/index.html' },
  { name: 'ChapterAPI', port: 5115, project: 'ChapterAPI/ChapterAPI.csproj', healthPath: '/swagger/index.html' },
  { name: 'MissionAPI', port: 5288, project: 'MissionAPI/MissionAPI.csproj', healthPath: '/swagger/index.html' },
  { name: 'PaymentAPI', port: 5128, project: 'PaymentAPI/PaymentAPI.csproj', healthPath: '/swagger/index.html' },
  { name: 'WalletAPI', port: 5091, project: 'WalletAPI/WalletAPI.csproj', healthPath: '/swagger/index.html' },
  { name: 'SocialAPI', port: 5197, project: 'SocialAPI/SocialAPI.csproj', healthPath: '/swagger/index.html' },
  { name: 'ApiGateway', port: 5028, project: 'ApiGateway/ApiGateway.csproj', healthPath: '/banners' },
]

async function isServiceReady(service) {
  try {
    const response = await fetch(`http://127.0.0.1:${service.port}${service.healthPath}`, {
      signal: AbortSignal.timeout(1200),
    })
    return response.ok
  } catch {
    return false
  }
}

async function waitForService(service, timeoutMs = 60000) {
  const deadline = Date.now() + timeoutMs
  while (Date.now() < deadline) {
    if (await isServiceReady(service)) return true
    await new Promise((resolve) => setTimeout(resolve, 500))
  }
  return false
}

function start(command, args) {
  const child = spawn(command, args, { cwd: frontendDir, stdio: 'inherit' })
  children.push(child)
  return child
}

function stop(exitCode = 0) {
  if (stopping) return
  stopping = true
  for (const child of [...children].reverse()) {
    if (!child.pid || child.killed) continue

    if (process.platform === 'win32') {
      // dotnet run creates an API.exe child process on Windows. Killing only
      // dotnet.exe leaves that child listening on its port after Ctrl+C.
      spawnSync('taskkill', ['/PID', String(child.pid), '/T', '/F'], {
        stdio: 'ignore',
        windowsHide: true,
      })
    } else {
      child.kill('SIGTERM')
    }
  }
  setTimeout(() => process.exit(exitCode), 300)
}

async function ensureService(service) {
  if (await isServiceReady(service)) {
    console.log(`[DEV] ${service.name} is already running at http://127.0.0.1:${service.port}.`)
    return
  }

  console.log(`[DEV] Starting ${service.name} at http://127.0.0.1:${service.port}...`)
  const process = start('dotnet', [
    'run',
    '--project', path.resolve(backendDir, service.project),
    // HTTPS profiles also expose each service's HTTP port for the Gateway,
    // while the HTTPS port is required by the internal gRPC clients.
    '--launch-profile', 'https',
  ])

  process.on('exit', async (code) => {
    if (stopping) return
    if (code !== 0 && !(await isServiceReady(service))) {
      console.error(`[DEV] ${service.name} stopped with exit code ${code}.`)
      stop(code || 1)
    }
  })

  if (!(await waitForService(service))) {
    console.error(`[DEV] ${service.name} did not become ready within 60 seconds.`)
    stop(1)
    throw new Error(`${service.name} startup timed out.`)
  }
}

try {
  for (const service of services) await ensureService(service)
} catch {
  // The service-specific error has already been printed above.
}

if (!stopping) {
  console.log('[DEV] API Gateway is ready at http://127.0.0.1:5028. Starting React...')
  const vite = start(process.execPath, [viteEntry])
  vite.on('exit', (code) => stop(code || 0))
}

process.on('SIGINT', () => stop(0))
process.on('SIGTERM', () => stop(0))
