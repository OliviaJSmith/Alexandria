import React, { useEffect } from "react";
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  Alert,
  ActivityIndicator,
} from "react-native";
import * as Google from "expo-auth-session/providers/google";
import * as WebBrowser from "expo-web-browser";
import { setAuthToken, loginWithGoogle } from "../services/api";
import { config } from "../config";

WebBrowser.maybeCompleteAuthSession();

export default function LoginScreen({ navigation }: any) {
  const [request, response, promptAsync] = Google.useAuthRequest({
    //androidClientId: config.google.androidClientId,
    //iosClientId: config.google.iosClientId,
    webClientId: config.google.webClientId,
  });

  useEffect(() => {
    handleSignInResponse();
  }, [response]);

  const handleSignInResponse = async () => {
    if (response?.type === "success") {
      const { authentication } = response;
      if (authentication?.accessToken) {
        try {
          // Exchange Google token for API JWT token
          const authResponse = await loginWithGoogle(
            authentication.accessToken,
          );

          // Store the API JWT token
          await setAuthToken(authResponse.token);

          console.log("Logged in as:", authResponse.user.email);
          navigation.replace("Main");
        } catch (error) {
          console.error("Failed to authenticate with API:", error);
          Alert.alert("Error", "Failed to complete sign-in. Please try again.");
        }
      }
    } else if (response?.type === "error") {
      Alert.alert("Error", "Google sign-in failed. Please try again.");
    }
  };

  const handleLogin = async () => {
    if (!request) {
      Alert.alert(
        "Error",
        "Google Sign-In is not ready yet. Please try again.",
      );
      return;
    }
    await promptAsync();
  };

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Alexandria</Text>
      <Text style={styles.subtitle}>Your Home Library</Text>

      <View style={styles.form}>
        <TouchableOpacity
          style={[styles.button, !request && styles.buttonDisabled]}
          onPress={handleLogin}
          disabled={!request}
        >
          {!request ? (
            <ActivityIndicator color="#1A1A1A" />
          ) : (
            <Text style={styles.buttonText}>Sign in with Google</Text>
          )}
        </TouchableOpacity>

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
    backgroundColor: "#121212",
    alignItems: "center",
    justifyContent: "center",
    padding: 20,
    overflow: "visible",
  },
  title: {
    fontSize: 36,
    fontWeight: "bold",
    color: "#FFFFFF",
    marginBottom: 10,
  },
  subtitle: {
    fontSize: 18,
    color: "#B0B0B0",
    marginBottom: 40,
  },
  form: {
    width: "100%",
    maxWidth: 400,
  },
  note: {
    marginTop: 20,
    textAlign: "center",
    color: "#888",
    fontSize: 12,
  },
  button: {
    backgroundColor: "#E5A823",
    padding: 15,
    borderRadius: 8,
    alignItems: "center",
  },
  buttonDisabled: {
    opacity: 0.6,
  },
  buttonText: {
    color: "#1A1A1A",
    fontSize: 16,
    fontWeight: "600",
  },
});
