/**
 * Authentication-related models mirroring the backend auth ViewModels.
 */

/** Admin login request. */
export interface AuthenticateRequest {
  username: string;
  password: string;
}

/** Admin login response carrying the issued JWT. */
export interface AuthenticateResponse {
  token: string;
  expires: string;
  username: string;
  role: string;
}
