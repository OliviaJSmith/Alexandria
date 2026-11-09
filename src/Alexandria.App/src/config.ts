/**
 * App configuration. Nothing here is secret: the Google web client ID is public by design.
 * Override the API URL with EXPO_PUBLIC_API_URL (e.g. http://10.0.2.2:5130 for an Android emulator).
 */
const developmentApiUrl = 'http://localhost:5130';
const productionApiUrl = 'https://app-uunpz277ms5jq.azurewebsites.net';

export const config = {
  apiUrl: process.env.EXPO_PUBLIC_API_URL ?? (__DEV__ ? developmentApiUrl : productionApiUrl),
  googleWebClientId: '1079278733225-6u5eh5lu72mivtdq7gh0arivkjjo5gv4.apps.googleusercontent.com',
};

export const betaTerms =
  'I acknowledge that as a beta user there is no guarantee my data or account will be persisted ' +
  'for any amount of time. Alexandria will notify all users ahead of time if accounts are to be ' +
  'closed down or if payment will be needed to continue to service an account.';
