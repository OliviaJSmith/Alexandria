import React, { useState } from 'react';
import { View, Text, Button, StyleSheet, Alert, Image } from 'react-native';
import * as ImagePicker from 'expo-image-picker';
import { searchBooksByImage } from '../services/api';
import { Book } from '../types';

export default function ImageSearchScreen() {
  const [imageUri, setImageUri] = useState<string | null>(null);
  const [books, setBooks] = useState<Book[]>([]);
  const [loading, setLoading] = useState(false);

  const pickImage = async () => {
    const { status } = await ImagePicker.requestMediaLibraryPermissionsAsync();
    if (status !== 'granted') {
      Alert.alert('Permission Denied', 'We need camera roll permissions to search by image');
      return;
    }

    const result = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ImagePicker.MediaTypeOptions.Images,
      allowsEditing: true,
      quality: 0.8,
    });

    if (!result.canceled && result.assets[0]) {
      setImageUri(result.assets[0].uri);
    }
  };

  const takePhoto = async () => {
    const { status } = await ImagePicker.requestCameraPermissionsAsync();
    if (status !== 'granted') {
      Alert.alert('Permission Denied', 'We need camera permissions to take photos');
      return;
    }

    const result = await ImagePicker.launchCameraAsync({
      allowsEditing: true,
      quality: 0.8,
    });

    if (!result.canceled && result.assets[0]) {
      setImageUri(result.assets[0].uri);
    }
  };

  const handleSearch = async () => {
    if (!imageUri) {
      Alert.alert('No Image', 'Please select or take a photo first');
      return;
    }

    setLoading(true);
    try {
      const results = await searchBooksByImage(imageUri);
      setBooks(results);
      if (results.length === 0) {
        Alert.alert('No Results', 'No books found matching this image. This feature requires OCR service integration.');
      }
    } catch (error) {
      console.error('Image search error:', error);
      Alert.alert('Error', 'Failed to search by image. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Search Books by Image</Text>
      <Text style={styles.subtitle}>
        Take a photo of a book cover or barcode to search
      </Text>

      {imageUri && (
        <Image source={{ uri: imageUri }} style={styles.image} />
      )}

      <View style={styles.buttonContainer}>
        <Button title="Choose from Library" onPress={pickImage} />
        <Button title="Take Photo" onPress={takePhoto} />
        {imageUri && (
          <Button 
            title={loading ? "Searching..." : "Search"} 
            onPress={handleSearch} 
            disabled={loading}
          />
        )}
      </View>

      <Text style={styles.note}>
        Note: Image recognition requires OCR service integration
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
    padding: 20,
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 10,
  },
  subtitle: {
    fontSize: 14,
    color: '#666',
    marginBottom: 20,
  },
  image: {
    width: '100%',
    height: 300,
    borderRadius: 8,
    marginBottom: 20,
  },
  buttonContainer: {
    gap: 10,
  },
  note: {
    marginTop: 20,
    textAlign: 'center',
    color: '#999',
    fontSize: 12,
  },
});
