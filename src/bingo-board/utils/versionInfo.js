/**
 * Version information utility
 * Gets commit SHA, .NET version, Aspire version, and build time
 */

export function getVersionInfo() {
  return {
    commitHash: getCommitHash(),
    dotnetVersion: getDotNetVersion(),
    aspireVersion: getAspireVersion(),
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
  return import.meta.env.VITE_DOTNET_VERSION || '9.0.7'
}

function getAspireVersion() {
  // This would typically be set during build
  return import.meta.env.VITE_ASPIRE_VERSION || '9.3.2'
}

function getRenderedTime() {
  // Format current time in GMT format similar to the inspiration image
  const now = new Date()
  return now.toUTCString()
}

export function getVersionString() {
  const info = getVersionInfo()
  return `Version Hash: #${info.commitHash} | Running on .NET ${info.dotnetVersion} | with .NET Aspire ${info.aspireVersion} | Rendered at: ${info.renderedAt}`
}
