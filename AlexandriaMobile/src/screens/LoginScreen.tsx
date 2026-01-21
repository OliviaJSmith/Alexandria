import React, { useState } from 'react';
import { View, Text, TextInput, TouchableOpacity, StyleSheet, Alert } from 'react-native';
import { setAuthToken } from '../services/api';

export default function LoginScreen({ navigation }: any) {
  const [email, setEmail] = useState('');

  const handleLogin = async () => {
    if (!email) {
      Alert.alert('Error', 'Please enter your email');
      return;
    }

    // Placeholder for actual Google Auth implementation
    // For now, just store a dummy token
    const dummyToken = 'dummy-jwt-token-' + Date.now();
    await setAuthToken(dummyToken);
    
    Alert.alert('Success', 'Logged in successfully!');
    navigation.replace('Main');
  };

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Alexandria</Text>
      <Text style={styles.subtitle}>Your Home Library</Text>
      
      <View style={styles.form}>
        <TextInput
          style={styles.input}
          placeholder="Email"
          placeholderTextColor="#888"
          value={email}
          onChangeText={setEmail}
          autoCapitalize="none"
          keyboardType="email-address"
        />
        
        <TouchableOpacity style={styles.button} onPress={handleLogin}>
          <Text style={styles.buttonText}>Login with Google</Text>
        </TouchableOpacity>
        
        <Text style={styles.note}>
          Note: Google authentication will be configured with your credentials
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
  input: {
    backgroundColor: '#2C2C2C',
    padding: 15,
    borderRadius: 8,
    marginBottom: 20,
    fontSize: 16,
    borderWidth: 1,
    borderColor: '#3C3C3C',
    color: '#FFFFFF',
  },
  note: {
    marginTop: 20,
    textAlign: 'center',
    color: '#888',
    fontSize: 12,
  },
  button: {
    backgroundColor: '#E5A823',
    padding: 15,
    borderRadius: 8,
    alignItems: 'center',
  },
  buttonText: {
    color: '#1A1A1A',
    fontSize: 16,
    fontWeight: '600',
  },
});
