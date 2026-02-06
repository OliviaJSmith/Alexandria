import React, { useEffect, useState } from 'react';
import { View, Text, TouchableOpacity, StyleSheet, Alert, ActivityIndicator } from 'react-native';
import * as Google from 'expo-auth-session/providers/google';
import * as WebBrowser from 'expo-web-browser';
import { loginWithGoogle } from '../services/api';
import { config } from '../config';

WebBrowser.maybeCompleteAuthSession();

export default function LoginScreen({ navigation }: any) {
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
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
          const authResponse = await loginWithGoogle(authentication.accessToken);
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
        <TouchableOpacity
          style={[styles.button, (!request || isLoading) && styles.buttonDisabled]}
          onPress={handleLogin}
          disabled={!request || isLoading}
        >
          {!request || isLoading ? (
            <ActivityIndicator color="#1A1A1A" />
          ) : (
            <Text style={styles.buttonText}>Sign in with Google</Text>
          )}
        </TouchableOpacity>

        {isLoading && <Text style={styles.loadingText}>Signing you in...</Text>}

        {errorMessage && <Text style={styles.errorText}>{errorMessage}</Text>}

        <Text style={styles.note}>Sign in to sync your library across devices</Text>
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
