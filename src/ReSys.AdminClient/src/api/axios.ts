import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_APP_API_BASE_URL;

const axiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  },
});

// Request interceptor for adding JWT token (will be implemented later)
axiosInstance.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('jwt_token'); // Or wherever you store your token
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor for handling common errors, e.g., 401 Unauthorized
axiosInstance.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response && error.response.status === 401) {
      // Handle unauthorized errors, e.g., redirect to login
      console.log('Unauthorized request. Redirecting to login...');
      // router.push('/login'); // Example: redirect with Vue Router
    }
    return Promise.reject(error);
  }
);

export default axiosInstance;
