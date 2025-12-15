// src/ReSys.AdminClient/src/stores/auth.ts
import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router'; // Assuming vue-router is used for redirection

import { AuthService } from '@/services/auth.service';
import {
  LoginParam,
  RegisterParam,
  RefreshParam,
  SessionResult,
  LoginResult,
  ProfileResult,
  UpdateProfileParam,
} from '@/types/auth'; // Import all necessary types

export const useAuthStore = defineStore('auth', () => {
  const router = useRouter(); // Initialize router inside the store

  const accessToken = ref<string | null>(localStorage.getItem('jwt_token'));
  const refreshToken = ref<string | null>(localStorage.getItem('refresh_token'));
  const user = ref<SessionResult | null>(null); // Stores basic session info
  const userProfile = ref<ProfileResult | null>(null); // Stores detailed profile info
  const isLoading = ref<boolean>(false);
  const authError = ref<string | null>(null);

  // --- Getters ---
  const isAuthenticated = computed<boolean>(() => !!accessToken.value);
  const isAdmin = computed<boolean>(() => user.value?.roles.includes('Admin') ?? false);
  // Add other roles/permissions checks as needed

  // --- Actions ---

  /**
   * Sets the authentication tokens and state.
   * @param authResult The authentication result from login or refresh.
   */
  function setAuthTokens(authResult: AuthenticationResult) {
    accessToken.value = authResult.accessToken;
    refreshToken.value = authResult.refreshToken;
    localStorage.setItem('jwt_token', authResult.accessToken);
    localStorage.setItem('refresh_token', authResult.refreshToken);
    authError.value = null; // Clear any previous errors
  }

  /**
   * Clears all authentication tokens and state.
   */
  function clearAuthTokens() {
    accessToken.value = null;
    refreshToken.value = null;
    user.value = null;
    userProfile.value = null;
    localStorage.removeItem('jwt_token');
    localStorage.removeItem('refresh_token');
    authError.value = null;
  }

  /**
   * Handles user login.
   * @param params Login credentials.
   * @returns true if login is successful, false otherwise.
   */
  async function performLogin(params: LoginParam): Promise<boolean> {
    isLoading.value = true;
    authError.value = null;
    try {
      const authResult: LoginResult = await AuthService.login(params);
      setAuthTokens(authResult);
      await fetchUserSession(); // Fetch user details after login
      router.push('/'); // Redirect to home or dashboard
      return true;
    } catch (error: any) {
      console.error('Login failed:', error);
      authError.value = error.response?.data?.message || 'Login failed. Please check your credentials.';
      clearAuthTokens();
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  /**
   * Handles user registration.
   * @param params Registration details.
   * @returns true if registration is successful, false otherwise.
   */
  async function performRegister(params: RegisterParam): Promise<boolean> {
    isLoading.value = true;
    authError.value = null;
    try {
      const registeredUser: RegisterResult = await AuthService.register(params);
      console.log('User registered successfully:', registeredUser);
      // Depending on the flow, you might auto-login, or redirect to login with a success message
      router.push('/login?registered=true');
      return true;
    } catch (error: any) {
      console.error('Registration failed:', error);
      authError.value = error.response?.data?.message || 'Registration failed.';
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  /**
   * Fetches the current authenticated user's session details.
   * This should be called on app startup or after login/refresh.
   */
  async function fetchUserSession(): Promise<void> {
    if (!isAuthenticated.value) {
      user.value = null;
      userProfile.value = null;
      return;
    }

    isLoading.value = true;
    authError.value = null;
    try {
      user.value = await AuthService.getSession();
      userProfile.value = await AuthService.getProfile();
    } catch (error: any) {
      console.error('Failed to fetch user session or profile:', error);
      authError.value = error.response?.data?.message || 'Failed to retrieve user session.';
      clearAuthTokens(); // Clear tokens if session fetching fails (e.g., token expired)
      if (router.currentRoute.value.meta.requiresAuth) {
        router.push('/login'); // Redirect to login if on a protected route
      }
    } finally {
      isLoading.value = false;
    }
  }

  /**
   * Handles user logout for the current session.
   */
  async function logout(): Promise<void> {
    isLoading.value = true;
    authError.value = null;
    try {
      // Pass refresh token if you want to explicitly revoke the current one on logout
      await AuthService.logout({ refreshToken: refreshToken.value || undefined });
      console.log('Logged out successfully.');
    } catch (error: any) {
      console.error('Logout API call failed:', error);
      authError.value = error.response?.data?.message || 'Logout failed on server side.';
      // Even if API logout fails, clear local state for UX
    } finally {
      clearAuthTokens();
      router.push('/login'); // Redirect to login page after logout
      isLoading.value = false;
    }
  }

  /**
   * Refreshes the access token using the stored refresh token.
   * @returns true if token refresh is successful, false otherwise.
   */
  async function refreshAuthTokens(): Promise<boolean> {
    if (!refreshToken.value) {
      console.warn('No refresh token available to refresh.');
      clearAuthTokens();
      router.push('/login');
      return false;
    }

    isLoading.value = true;
    authError.value = null;
    try {
      const newAuthResult: RefreshResult = await AuthService.refreshAccessToken({
        refreshToken: refreshToken.value,
        rememberMe: true, // Assuming you want to persist the session
      });
      setAuthTokens(newAuthResult);
      await fetchUserSession(); // Update user session after token refresh
      return true;
    } catch (error: any) {
      console.error('Failed to refresh access token:', error);
      authError.value = error.response?.data?.message || 'Session expired. Please log in again.';
      clearAuthTokens();
      router.push('/login'); // Redirect to login on refresh failure
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  /**
   * Updates the user's profile.
   * @param params Profile update data.
   * @returns true if profile update is successful, false otherwise.
   */
  async function updateProfile(params: UpdateProfileParam): Promise<boolean> {
    isLoading.value = true;
    authError.value = null;
    try {
      await AuthService.updateProfile(params);
      await fetchUserSession(); // Re-fetch profile to reflect changes
      return true;
    } catch (error: any) {
      console.error('Failed to update profile:', error);
      authError.value = error.response?.data?.message || 'Failed to update profile.';
      return false;
    } finally {
      isLoading.value = false;
    }
  }

  // Initial fetch of user session when the store is initialized
  // This helps re-authenticate user on page refresh if tokens are valid.
  if (isAuthenticated.value) {
    fetchUserSession();
  }

  return {
    accessToken,
    refreshToken,
    user,
    userProfile,
    isLoading,
    authError,
    isAuthenticated,
    isAdmin,
    performLogin,
    performRegister,
    fetchUserSession,
    logout,
    refreshAuthTokens,
    updateProfile,
    // Add other actions for email/password/phone management here as needed.
  };
});
