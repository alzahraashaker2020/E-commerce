/**
 * Development environment configuration.
 *
 * `apiBaseUrl` points at the local ASP.NET Core Web API (Shop.Api), which the
 * backend Program.cs hosts on http://localhost:5000. All services under
 * core/services build their request URLs from this value.
 */
export const environment = {
  production: false,
  // Base URL of the Shop.Api backend. Override in environment.prod.ts for prod.
  apiBaseUrl: `http://${window.location.hostname}:5000/api`
};
