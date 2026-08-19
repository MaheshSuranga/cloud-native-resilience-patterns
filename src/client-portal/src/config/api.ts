/**
 * API configuration for backend service URLs.
 *
 * In development: Vite proxy handles routing, so base URLs are empty (relative paths).
 * In production: VITE_API_BASE_URL env var points to the Recommendations App Service.
 *
 * Set VITE_API_BASE_URL at build time, e.g.:
 *   VITE_API_BASE_URL=https://resilience-demo-recommendations-xxxxx.azurewebsites.net npm run build
 */

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

export const apiConfig = {
  /** Base URL for the Recommendations / Homepage backend */
  baseUrl: API_BASE_URL,

  /** Build a full URL for a given API path (e.g. '/recommendations/user_std') */
  url(path: string): string {
    return `${API_BASE_URL}${path}`;
  }
};
