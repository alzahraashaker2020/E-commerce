/**
 * Production environment configuration.
 *
 * Replace `apiBaseUrl` with the deployed backend origin at release time. The
 * value here is a sensible same-origin default for a reverse-proxied deployment
 * where the API is served under /api.
 */
export const environment = {
  production: true,
  apiBaseUrl: '/api'
};
