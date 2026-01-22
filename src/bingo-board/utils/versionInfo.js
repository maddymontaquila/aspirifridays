/**
 * Version information utility
 * Gets commit SHA, .NET version, Aspire version, Vite version, and build time
 */

export function getVersionInfo() {
  return {
    commitHash: getCommitHash(),
    dotnetVersion: getDotNetVersion(),
    aspireVersion: getAspireVersion(),
    viteVersion: getViteVersion(),
    renderedAt: getRenderedTime()
  }
}

function getCommitHash() {
  // Try to get from environment variable (set during build)
  // In Vite, env vars must be prefixed with VITE_
  return import.meta.env.VITE_COMMIT_SHA?.substring(0, 7) || 'unknown'
}

function getDotNetVersion() {
  // This would typically be set during build
  return import.meta.env.VITE_DOTNET_VERSION || '10.0'
}

function getAspireVersion() {
  // This would typically be set during build
  return import.meta.env.VITE_ASPIRE_VERSION || '13.0.0-preview.1'
}

function getViteVersion() {
  // Vite version is set during build from package.json
  return import.meta.env.VITE_VERSION || '6.3.5'
}

function getRenderedTime() {
  // Format current time in GMT format similar to the inspiration image
  const now = new Date()
  return now.toUTCString()
}

export function getVersionString() {
  const info = getVersionInfo()
  return `Version Hash: #${info.commitHash} | Running on .NET ${info.dotnetVersion} | with Aspire ${info.aspireVersion} | Vite ${info.viteVersion} | Rendered at: ${info.renderedAt}`
}
