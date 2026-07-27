import React, { useEffect, useState } from 'react';
import { View, Text, TouchableOpacity, StyleSheet, Alert, ActivityIndicator } from 'react-native';
import * as Google from 'expo-auth-session/providers/google';
import * as WebBrowser from 'expo-web-browser';
import { loginWithGoogle, getTermsAcceptedAt, setTermsAccepted } from '../services/api';
import { config } from '../config';

WebBrowser.maybeCompleteAuthSession();

const BETA_TERMS =
  'I acknowledge that as a beta user there is no guarantee my data or account will be persisted ' +
  'for any amount of time. Alexandria will notify all users ahead of time if accounts are to be ' +
  'closed down or if payment will be needed to continue to service an account.';

export default function LoginScreen({ navigation }: any) {
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [termsChecked, setTermsChecked] = useState(false);

  useEffect(() => {
    // Pre-check the box if the user already accepted the terms on this device
    getTermsAcceptedAt()
      .then((acceptedAt) => setTermsChecked(!!acceptedAt))
      .catch((error) => console.error('Failed to read terms acceptance:', error));
  }, []);
  const [request, response, promptAsync] = Google.useAuthRequest({
    //androidClientId: config.google.androidClientId,
    //iosClientId: config.google.iosClientId,
    webClientId: config.google.webClientId,
  });

  useEffect(() => {
    console.log('LoginScreen: useEffect triggered, response type:', response?.type);
    handleSignInResponse();
  }, [response]);

  const handleSignInResponse = async () => {
    console.log('handleSignInResponse: response =', JSON.stringify(response, null, 2));
    setErrorMessage(null);
    
    if (response?.type === 'success') {
      const { authentication } = response;
      console.log('handleSignInResponse: Got success, authentication exists:', !!authentication);
      console.log('handleSignInResponse: accessToken exists:', !!authentication?.accessToken);
      
      if (authentication?.accessToken) {
        setIsLoading(true);
        try {
          // Exchange Google token for Alexandria JWT token
          console.log('handleSignInResponse: Calling loginWithGoogle...');
          const authResponse = await loginWithGoogle(authentication.accessToken, true);
          await setTermsAccepted();
          console.log('handleSignInResponse: Login successful, user:', authResponse.user?.name);
          console.log('handleSignInResponse: Navigating to Main...');
          navigation.replace('Main');
        } catch (error: any) {
          console.error('handleSignInResponse: Failed to authenticate:', error);
          const errorMsg = error?.response?.data || error?.message || 'Unknown error';
          console.error('handleSignInResponse: Error details:', errorMsg);
          setErrorMessage(String(errorMsg));
          Alert.alert('Authentication Error', String(errorMsg));
        } finally {
          setIsLoading(false);
        }
      } else {
        console.log('handleSignInResponse: No access token in authentication');
        setErrorMessage('No access token received from Google');
      }
    } else if (response?.type === 'error') {
      console.error('handleSignInResponse: Google error:', response.error);
      setErrorMessage(`Google sign-in error: ${response.error?.message || 'Unknown'}`);
      Alert.alert('Error', 'Google sign-in failed. Please try again.');
    } else if (response?.type === 'dismiss') {
      console.log('handleSignInResponse: User dismissed the sign-in');
    }
  };

  const handleLogin = async () => {
    setErrorMessage(null);
    if (!termsChecked) {
      setErrorMessage('Please accept the beta terms and conditions to continue.');
      return;
    }
    if (!request) {
      Alert.alert('Error', 'Google Sign-In is not ready yet. Please try again.');
      return;
    }
    console.log('handleLogin: Calling promptAsync...');
    const result = await promptAsync();
    console.log('handleLogin: promptAsync returned:', result?.type);
  };

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Alexandria</Text>
      <Text style={styles.subtitle}>Your Home Library</Text>
      
      <View style={styles.form}>
        <View style={styles.termsBox}>
          <Text style={styles.termsTitle}>Beta Terms &amp; Conditions</Text>
          <Text style={styles.termsText}>{BETA_TERMS}</Text>
          <TouchableOpacity
            style={styles.checkboxRow}
            onPress={() => {
              setErrorMessage(null);
              setTermsChecked(!termsChecked);
            }}
            accessibilityRole="checkbox"
            accessibilityState={{ checked: termsChecked }}
          >
            <View style={[styles.checkbox, termsChecked && styles.checkboxChecked]}>
              {termsChecked && <Text style={styles.checkboxMark}>✓</Text>}
            </View>
            <Text style={styles.checkboxLabel}>I acknowledge and agree to the terms above</Text>
          </TouchableOpacity>
        </View>

        <TouchableOpacity
          style={[styles.button, (!request || isLoading || !termsChecked) && styles.buttonDisabled]}
          onPress={handleLogin}
          disabled={!request || isLoading || !termsChecked}
        >
          {!request || isLoading ? (
            <ActivityIndicator color="#1A1A1A" />
          ) : (
            <Text style={styles.buttonText}>Sign in with Google</Text>
          )}
        </TouchableOpacity>
        
        {isLoading && (
          <Text style={styles.loadingText}>Signing you in...</Text>
        )}
        
        {errorMessage && (
          <Text style={styles.errorText}>{errorMessage}</Text>
        )}
        
        <Text style={styles.note}>
          Sign in to sync your library across devices
        </Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#121212',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 20,
    overflow: 'visible',
  },
  title: {
    fontSize: 36,
    fontWeight: 'bold',
    color: '#FFFFFF',
    marginBottom: 10,
  },
  subtitle: {
    fontSize: 18,
    color: '#B0B0B0',
    marginBottom: 40,
  },
  form: {
    width: '100%',
    maxWidth: 400,
  },
  note: {
    marginTop: 20,
    textAlign: 'center',
    color: '#888',
    fontSize: 12,
  },
  loadingText: {
    marginTop: 15,
    textAlign: 'center',
    color: '#B0B0B0',
    fontSize: 14,
  },
  errorText: {
    marginTop: 15,
    textAlign: 'center',
    color: '#FF6B6B',
    fontSize: 14,
  },
  termsBox: {
    backgroundColor: '#1E1E1E',
    borderRadius: 8,
    padding: 15,
    marginBottom: 20,
    borderWidth: 1,
    borderColor: '#333',
  },
  termsTitle: {
    color: '#FFFFFF',
    fontSize: 14,
    fontWeight: '600',
    marginBottom: 8,
  },
  termsText: {
    color: '#B0B0B0',
    fontSize: 13,
    lineHeight: 19,
    marginBottom: 12,
  },
  checkboxRow: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  checkbox: {
    width: 22,
    height: 22,
    borderRadius: 4,
    borderWidth: 2,
    borderColor: '#888',
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 10,
  },
  checkboxChecked: {
    backgroundColor: '#E5A823',
    borderColor: '#E5A823',
  },
  checkboxMark: {
    color: '#1A1A1A',
    fontSize: 14,
    fontWeight: '700',
  },
  checkboxLabel: {
    color: '#FFFFFF',
    fontSize: 13,
    flex: 1,
  },
  button: {
    backgroundColor: '#E5A823',
    padding: 15,
    borderRadius: 8,
    alignItems: 'center',
  },
  buttonDisabled: {
    opacity: 0.6,
  },
  buttonText: {
    color: '#1A1A1A',
    fontSize: 16,
    fontWeight: '600',
  },
});
