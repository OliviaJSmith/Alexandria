// Configuration for the Alexandria mobile app
// Update these values based on your environment

export const config = {
  // API Configuration
  api: {
    // Development: Use your local IP address for testing on physical devices
    // Production: Use your deployed API URL
    baseUrl: process.env.API_BASE_URL || 'http://localhost:5000/api',
  },
  
  // Authentication Configuration
  auth: {
    googleClientId: process.env.GOOGLE_CLIENT_ID || '',
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
