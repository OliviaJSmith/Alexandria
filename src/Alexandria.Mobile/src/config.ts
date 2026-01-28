// Configuration for the Alexandria mobile app
// Environment variables are passed through app.config.js and accessed via expo-constants

import Constants from 'expo-constants';

const extra = Constants.expoConfig?.extra;

export const config = {
  // API Configuration
  api: {
    // Development: Use your local IP address for testing on physical devices
    // Production: Use your deployed API URL
    baseUrl: extra?.apiBaseUrl || 'http://localhost:5000/api',
  },
  
  // Google OAuth Configuration
  // Get these from Google Cloud Console: https://console.cloud.google.com/apis/credentials
  google: {
    // Web OAuth Client ID (used for Expo Go development)
    webClientId: extra?.googleWebClientId || '',
    // Android OAuth Client ID (use the one associated with your keystore)
    androidClientId: extra?.googleAndroidClientId || '',
    // iOS OAuth Client ID
    iosClientId: extra?.googleIosClientId || '',
  },
  
  // Feature Flags
  features: {
    imageSearch: true,
    socialFeatures: true,
    publicLibraries: true,
  },
};

// Environment-specific configurations
export const environments = {
  development: {
    api: {
      baseUrl: 'http://localhost:5000/api',
    },
  },
  staging: {
    api: {
      baseUrl: 'https://alexandria-staging.azurewebsites.net/api',
    },
  },
  production: {
    api: {
      baseUrl: 'https://alexandria.azurewebsites.net/api',
    },
  },
};

// Get current environment
export const getCurrentEnvironment = () => {
  const env = process.env.NODE_ENV || 'development';
  return environments[env as keyof typeof environments] || environments.development;
};
