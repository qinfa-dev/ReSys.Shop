// src/ReSys.AdminClient/src/types/auth.d.ts

/**
 * Generic API response wrapper from the backend.
 * `data` field will be present on success, `errors` field on failure.
 */
export interface ApiResponse<T = undefined> {
  data?: T;
  message?: string;
  errors?: { code: string; description: string; type: number }[]; // Assuming basic error structure
  statusCode?: number;
  isSuccess: boolean;
}

/**
 * Common Authentication Result containing JWT tokens.
 */
export interface AuthenticationResult {
  accessToken: string;
  accessTokenExpiresAt: string; // ISO 8601 string (e.g., "2025-12-14T10:30:00Z")
  refreshToken: string;
  refreshTokenExpiresAt: string; // ISO 8601 string
  tokenType: string; // "Bearer"
}

/**
 * Common type for a successful update operation (backend returns 'Updated' object).
 */
export type Updated = object; 

/**
 * Common type for a successful delete operation (backend returns 'Deleted' object).
 */
export type Deleted = object;

// --- InternalModule.Register ---
export interface RegisterParam {
  userName?: string;
  email: string;
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
  confirmPassword: string;
  password: string;
  dateOfBirth?: string; // ISO 8601 string
}

export interface RegisterResult {
  id: string;
  email: string;
  phoneNumber?: string;
  username: string;
  firstName?: string;
  lastName?: string;
  dateOfBirth?: string; // ISO 8601 string
  profileImagePath?: string;
  lastSignInAt?: string; // ISO 8601 string
  lastSignInIp?: string;
}

// --- InternalModule.Login ---
export interface LoginParam {
  credential: string; // Can be username, email, or phone number
  password: string;
  rememberMe?: boolean;
}

export interface LoginResult extends AuthenticationResult {}

// --- ExternalModule.GetExternalProviders ---
export interface ExternalProvider {
  provider: string; // e.g., "google", "facebook"
  displayName: string;
  loginUrl: string; // URL for frontend to initiate OAuth flow
  iconUrl?: string;
  isEnabled: boolean;
  requiredScopes: string[];
  configurationUrl: string; // URL to get detailed OAuth config
}

// --- ExternalModule.GetOAuthConfig ---
export interface OAuthConfigResult {
  provider: string;
  providerName: string;
  clientId: string;
  authorizationUrl: string;
  tokenUrl: string;
  scopes: string[];
  responseType: string;
  additionalParameters: { [key: string]: string };
  tokenExchangeUrl: string; // Backend endpoint to exchange the provider's token
  requiresPKCE: boolean;
}

// --- ExternalModule.ExchangeToken ---
export interface ExchangeTokenParam {
  provider: string; // Redundant but part of the backend Param
  accessToken?: string;
  idToken?: string;
  authorizationCode?: string;
  redirectUri?: string;
  rememberMe?: boolean;
}

export interface ExchangeUserProfile {
  email: string;
  firstName?: string;
  lastName?: string;
  profilePictureUrl?: string;
  emailVerified: boolean;
  hasExternalLogins: boolean;
  externalProviders: string[];
  additionalClaims: { [key: string]: string };
}

export interface ExchangeTokenResult extends AuthenticationResult {
  isNewUser: boolean;
  isNewLogin: boolean;
  userProfile?: ExchangeUserProfile;
}

// --- ExternalModule.VerifyExternalToken ---
export interface VerifyExternalTokenParam {
  accessToken?: string;
  idToken?: string;
}

export interface VerifyExternalTokenResult {
  provider: string;
  providerKey: string;
  firstName?: string;
  lastName?: string;
  email: string;
  profilePictureUrl?: string;
  emailVerified: boolean;
  additionalClaims: { [key: string]: string };
}

// --- SessionModule.Get ---
export interface SessionResult {
  userId: string;
  userName: string;
  email: string;
  phoneNumber?: string;
  isEmailConfirmed: boolean;
  isPhoneNumberConfirmed: boolean;
  roles: string[];
  permissions: string[];
}

// --- SessionModule.Refresh ---
export interface RefreshParam {
  refreshToken: string;
  rememberMe?: boolean;
}

export interface RefreshResult extends AuthenticationResult {}

// --- LogOutModule.Single ---
export interface LogoutSingleParam {
  refreshToken?: string;
}

// --- LogOutModule.FromAll ---
export interface LogoutFromAllParam {
  refreshToken?: string;
}

// --- EmailModule.Change ---
export interface ChangeEmailParam {
  currentEmail: string;
  newEmail: string;
  password: string;
}

export interface ChangeEmailResult {
  confirmMessage: string;
}

// --- EmailModule.Confirm ---
export interface ConfirmEmailParam {
  userId: string;
  code: string;
  changedEmail?: string;
}

export interface ConfirmEmailResult {
  confirmMessage: string;
}

// --- EmailModule.ResendConfirmation ---
export interface ResendConfirmationParam {
  email?: string;
}

export interface ResendConfirmationResult {
  confirmMessage: string;
}

// --- PasswordModule.Change ---
export interface ChangePasswordParam {
  currentPassword: string;
  newPassword: string;
  newPasswordConfirm: string;
}

// --- PasswordModule.Forgot ---
export interface ForgotPasswordParam {
  email: string;
}

export interface ForgotPasswordResult {
  message: string;
}

// --- PasswordModule.Reset ---
export interface ResetPasswordParam {
  email: string;
  resetCode: string;
  newPassword: string;
}

export interface ResetPasswordResult {
  message: string;
}

// --- PhoneModule.Change ---
export interface ChangePhoneParam {
  newPhone: string;
}

export interface ChangePhoneResult {
  confirmMessage: string;
}

// --- PhoneModule.Confirm ---
export interface ConfirmPhoneParam {
  newPhone: string;
  code: string;
}

// --- PhoneModule.ResendVerification ---
export interface ResendVerificationParam {
  phoneNumber: string;
}

export interface ResendVerificationResult {
  message: string;
}

// --- ProfileModule.Get & ProfileModule.Model.Result (shared) ---
export interface ProfileResult {
  id: string;
  email: string;
  phoneNumber?: string;
  username: string;
  firstName?: string;
  lastName?: string;
  dateOfBirth?: string; // ISO 8601 string
  profileImagePath?: string;
  lastSignInAt?: string; // ISO 8601 string
  lastSignInIp?: string;
}

// --- ProfileModule.Update & ProfileModule.Model.Param (shared) ---
export interface UpdateProfileParam {
  username: string;
  firstName?: string;
  lastName?: string;
  dateOfBirth?: string; // ISO 8601 string
  profileImagePath?: string;
}
