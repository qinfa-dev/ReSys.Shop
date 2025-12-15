// src/ReSys.AdminClient/src/services/auth.service.ts
import axiosInstance from '/api-client/axios';
import {
  ApiResponse,
  LoginParam,
  LoginResult,
  RegisterParam,
  RegisterResult,
  SessionResult,
  LogoutSingleParam,
  RefreshParam,
  RefreshResult,
  ExternalProvider,
  ProfileResult,
  UpdateProfileParam,
  Updated,
} from '../types/auth'; // Adjust path as necessary

/**
 * Handles all authentication and account-related API calls.
 */
export const AuthService = {
  /**
   * Registers a new user with the application.
   * @param params Registration details.
   * @returns The registered user's profile information.
   */
  async register(params: RegisterParam): Promise<RegisterResult> {
    const response = await axiosInstance.post<ApiResponse<RegisterResult>>(
      '/account/auth/internal/register',
      params
    );
    return response.data.data!;
  },

  /**
   * Authenticates a user and returns JWT tokens.
   * Stores tokens in localStorage upon successful login.
   * @param params Login credentials.
   * @returns AuthenticationResult containing access and refresh tokens.
   */
  async login(params: LoginParam): Promise<LoginResult> {
    const response = await axiosInstance.post<ApiResponse<LoginResult>>(
      '/account/auth/internal/login',
      params
    );
    const authResult = response.data.data!;
    localStorage.setItem('jwt_token', authResult.accessToken);
    localStorage.setItem('refresh_token', authResult.refreshToken);
    return authResult;
  },

  /**
   * Retrieves the current authenticated user's session information.
   * Requires an active JWT token.
   * @returns User session details.
   */
  async getSession(): Promise<SessionResult> {
    const response = await axiosInstance.get<ApiResponse<SessionResult>>(
      '/account/auth/session'
    );
    return response.data.data!;
  },

  /**
   * Logs out the current user's session.
   * Optionally revokes a specific refresh token.
   * @param params Optional refresh token to revoke.
   */
  async logout(params?: LogoutSingleParam): Promise<void> {
    await axiosInstance.post<ApiResponse<void>>('/account/auth/session/logout/me', params);
    localStorage.removeItem('jwt_token');
    localStorage.removeItem('refresh_token');
  },

  /**
   * Refreshes the access token using a valid refresh token.
   * Stores new tokens in localStorage upon successful refresh.
   * @param params Refresh token and rememberMe flag.
   * @returns New AuthenticationResult.
   */
  async refreshAccessToken(params: RefreshParam): Promise<RefreshResult> {
    const response = await axiosInstance.post<ApiResponse<RefreshResult>>(
      '/account/auth/session/refresh',
      params
    );
    const authResult = response.data.data!;
    localStorage.setItem('jwt_token', authResult.accessToken);
    localStorage.setItem('refresh_token', authResult.refreshToken);
    return authResult;
  },

  /**
   * Retrieves a list of configured external authentication providers.
   * @returns List of external providers.
   */
  async getExternalProviders(): Promise<ExternalProvider[]> {
    const response = await axiosInstance.get<ApiResponse<ExternalProvider[]>>(
      '/account/auth/external/providers'
    );
    return response.data.data!;
  },

  /**
   * Retrieves the current authenticated user's profile information.
   * Requires an active JWT token.
   * @returns User profile details.
   */
  async getProfile(): Promise<ProfileResult> {
    const response = await axiosInstance.get<ApiResponse<ProfileResult>>(
      '/account/profile'
    );
    return response.data.data!;
  },

  /**
   * Updates the current authenticated user's profile information.
   * Requires an active JWT token.
   * @param params Profile update details.
   */
  async updateProfile(params: UpdateProfileParam): Promise<Updated> {
    const response = await axiosInstance.put<ApiResponse<Updated>>(
      '/account/profile',
      params
    );
    return response.data.data!;
  },

  // Add more API calls for other modules (Email, Password, Phone, External specific calls) here as needed.
};
