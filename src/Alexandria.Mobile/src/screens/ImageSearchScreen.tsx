import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  Alert,
  Image,
  ScrollView,
  TouchableOpacity,
  TextInput,
  ActivityIndicator,
  Modal,
} from 'react-native';
import * as ImagePicker from 'expo-image-picker';
import { scanSingleBook, getLibraries, confirmBooksToLibrary } from '../services/api';
import { BookPreview, Library, BookSource } from '../types';

export default function ImageSearchScreen() {
  const [imageUri, setImageUri] = useState<string | null>(null);
  const [bookPreview, setBookPreview] = useState<BookPreview | null>(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [libraries, setLibraries] = useState<Library[]>([]);
  const [selectedLibraryId, setSelectedLibraryId] = useState<number | null>(null);
  const [showLibraryPicker, setShowLibraryPicker] = useState(false);
  const [editedPreview, setEditedPreview] = useState<BookPreview | null>(null);

  useEffect(() => {
    loadLibraries();
  }, []);

  const loadLibraries = async () => {
    try {
      const libs = await getLibraries();
      setLibraries(libs);
      if (libs.length > 0) {
        setSelectedLibraryId(libs[0].id);
      }
    } catch (error) {
      console.error('Failed to load libraries:', error);
    }
  };

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
      setBookPreview(null);
      setEditedPreview(null);
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
      setBookPreview(null);
      setEditedPreview(null);
    }
  };

  const handleScan = async () => {
    if (!imageUri) {
      Alert.alert('No Image', 'Please select or take a photo first');
      return;
    }

    setLoading(true);
    try {
      const result = await scanSingleBook(imageUri);
      setBookPreview(result);
      setEditedPreview({ ...result });
    } catch (error: any) {
      console.error('Scan error:', error);
      if (error.response?.status === 404) {
        Alert.alert('Not Found', 'Could not identify the book from this image. Try a clearer photo or manual entry.');
      } else {
        Alert.alert('Error', 'Failed to scan book. Please try again.');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleAddToLibrary = async () => {
    if (!editedPreview || !selectedLibraryId) {
      Alert.alert('Error', 'Please select a library first');
      return;
    }

    setSaving(true);
    try {
      const result = await confirmBooksToLibrary(selectedLibraryId, {
        books: [editedPreview],
      });

      if (result.successCount > 0) {
        Alert.alert(
          'Success',
          `"${editedPreview.title}" has been added to your library!`,
          [{ text: 'OK', onPress: () => resetForm() }]
        );
      } else {
        Alert.alert('Error', result.results[0]?.error || 'Failed to add book to library');
      }
    } catch (error) {
      console.error('Add to library error:', error);
      Alert.alert('Error', 'Failed to add book to library. Please try again.');
    } finally {
      setSaving(false);
    }
  };

  const resetForm = () => {
    setImageUri(null);
    setBookPreview(null);
    setEditedPreview(null);
  };

  const getSourceLabel = (source: BookSource): string => {
    switch (source) {
      case BookSource.Local:
        return 'From Your Database';
      case BookSource.OpenLibrary:
        return 'From Open Library';
      case BookSource.GoogleBooks:
        return 'From Google Books';
      case BookSource.OcrText:
        return 'From Image Text';
      default:
        return 'Unknown';
    }
  };

  const getSourceColor = (source: BookSource): string => {
    switch (source) {
      case BookSource.Local:
        return '#4CAF50';
      case BookSource.OpenLibrary:
        return '#2196F3';
      case BookSource.GoogleBooks:
        return '#FF9800';
      case BookSource.OcrText:
        return '#9C27B0';
      default:
        return '#757575';
    }
  };

  const selectedLibrary = libraries.find(l => l.id === selectedLibraryId);

  return (
    <ScrollView style={styles.container}>
      <Text style={styles.title}>Scan a Book</Text>
      <Text style={styles.subtitle}>
        Take a photo of a book cover or barcode to identify it
      </Text>

      {/* Image Preview */}
      {imageUri && (
        <Image source={{ uri: imageUri }} style={styles.image} />
      )}

      {/* Action Buttons */}
      <View style={styles.buttonContainer}>
        <TouchableOpacity style={styles.button} onPress={pickImage}>
          <Text style={styles.buttonText}>Choose from Library</Text>
        </TouchableOpacity>
        <TouchableOpacity style={styles.button} onPress={takePhoto}>
          <Text style={styles.buttonText}>Take Photo</Text>
        </TouchableOpacity>
        {imageUri && !bookPreview && (
          <TouchableOpacity
            style={[styles.button, styles.primaryButton]}
            onPress={handleScan}
            disabled={loading}
          >
            {loading ? (
              <ActivityIndicator color="#fff" />
            ) : (
              <Text style={styles.primaryButtonText}>Scan Book</Text>
            )}
          </TouchableOpacity>
        )}
      </View>

      {/* Book Preview Card */}
      {editedPreview && (
        <View style={styles.previewCard}>
          <View style={styles.previewHeader}>
            <Text style={styles.previewTitle}>Book Details</Text>
            <View style={[styles.sourceBadge, { backgroundColor: getSourceColor(editedPreview.source) }]}>
              <Text style={styles.sourceBadgeText}>{getSourceLabel(editedPreview.source)}</Text>
            </View>
          </View>

          {/* Cover Image */}
          {editedPreview.coverImageUrl && (
            <Image
              source={{ uri: editedPreview.coverImageUrl }}
              style={styles.coverImage}
              resizeMode="contain"
            />
          )}

          {/* Confidence Indicator */}
          <View style={styles.confidenceContainer}>
            <Text style={styles.confidenceLabel}>Confidence:</Text>
            <View style={styles.confidenceBar}>
              <View
                style={[
                  styles.confidenceFill,
                  { width: `${editedPreview.confidence * 100}%` },
                ]}
              />
            </View>
            <Text style={styles.confidenceValue}>
              {Math.round(editedPreview.confidence * 100)}%
            </Text>
          </View>

          {/* Editable Fields */}
          <View style={styles.fieldContainer}>
            <Text style={styles.fieldLabel}>Title *</Text>
            <TextInput
              style={styles.input}
              value={editedPreview.title}
              onChangeText={(text) => setEditedPreview({ ...editedPreview, title: text })}
              placeholder="Book title"
            />
          </View>

          <View style={styles.fieldContainer}>
            <Text style={styles.fieldLabel}>Author</Text>
            <TextInput
              style={styles.input}
              value={editedPreview.author || ''}
              onChangeText={(text) => setEditedPreview({ ...editedPreview, author: text || undefined })}
              placeholder="Author name"
            />
          </View>

          <View style={styles.fieldContainer}>
            <Text style={styles.fieldLabel}>ISBN</Text>
            <TextInput
              style={styles.input}
              value={editedPreview.isbn || ''}
              onChangeText={(text) => setEditedPreview({ ...editedPreview, isbn: text || undefined })}
              placeholder="ISBN-13"
              keyboardType="numeric"
            />
          </View>

          <View style={styles.row}>
            <View style={[styles.fieldContainer, { flex: 1, marginRight: 8 }]}>
              <Text style={styles.fieldLabel}>Year</Text>
              <TextInput
                style={styles.input}
                value={editedPreview.publishedYear?.toString() || ''}
                onChangeText={(text) =>
                  setEditedPreview({
                    ...editedPreview,
                    publishedYear: text ? parseInt(text, 10) : undefined,
                  })
                }
                placeholder="Year"
                keyboardType="numeric"
              />
            </View>
            <View style={[styles.fieldContainer, { flex: 1 }]}>
              <Text style={styles.fieldLabel}>Pages</Text>
              <TextInput
                style={styles.input}
                value={editedPreview.pageCount?.toString() || ''}
                onChangeText={(text) =>
                  setEditedPreview({
                    ...editedPreview,
                    pageCount: text ? parseInt(text, 10) : undefined,
                  })
                }
                placeholder="Pages"
                keyboardType="numeric"
              />
            </View>
          </View>

          <View style={styles.fieldContainer}>
            <Text style={styles.fieldLabel}>Publisher</Text>
            <TextInput
              style={styles.input}
              value={editedPreview.publisher || ''}
              onChangeText={(text) => setEditedPreview({ ...editedPreview, publisher: text || undefined })}
              placeholder="Publisher"
            />
          </View>

          <View style={styles.fieldContainer}>
            <Text style={styles.fieldLabel}>Genre</Text>
            <TextInput
              style={styles.input}
              value={editedPreview.genre || ''}
              onChangeText={(text) => setEditedPreview({ ...editedPreview, genre: text || undefined })}
              placeholder="Genre"
            />
          </View>

          {/* Library Selector */}
          <View style={styles.fieldContainer}>
            <Text style={styles.fieldLabel}>Add to Library</Text>
            <TouchableOpacity
              style={styles.librarySelector}
              onPress={() => setShowLibraryPicker(true)}
            >
              <Text style={styles.librarySelectorText}>
                {selectedLibrary?.name || 'Select a library...'}
              </Text>
            </TouchableOpacity>
          </View>

          {/* Action Buttons */}
          <View style={styles.previewActions}>
            <TouchableOpacity style={styles.cancelButton} onPress={resetForm}>
              <Text style={styles.cancelButtonText}>Cancel</Text>
            </TouchableOpacity>
            <TouchableOpacity
              style={[styles.button, styles.primaryButton, !selectedLibraryId && styles.disabledButton]}
              onPress={handleAddToLibrary}
              disabled={saving || !selectedLibraryId}
            >
              {saving ? (
                <ActivityIndicator color="#fff" />
              ) : (
                <Text style={styles.primaryButtonText}>Add to Library</Text>
              )}
            </TouchableOpacity>
          </View>
        </View>
      )}

      {/* Library Picker Modal */}
      <Modal visible={showLibraryPicker} transparent animationType="slide">
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <Text style={styles.modalTitle}>Select Library</Text>
            <ScrollView style={styles.libraryList}>
              {libraries.map((library) => (
                <TouchableOpacity
                  key={library.id}
                  style={[
                    styles.libraryItem,
                    library.id === selectedLibraryId && styles.libraryItemSelected,
                  ]}
                  onPress={() => {
                    setSelectedLibraryId(library.id);
                    setShowLibraryPicker(false);
                  }}
                >
                  <Text style={styles.libraryItemText}>{library.name}</Text>
                  {library.isPublic && (
                    <Text style={styles.publicBadge}>Public</Text>
                  )}
                </TouchableOpacity>
              ))}
            </ScrollView>
            <TouchableOpacity
              style={styles.modalCloseButton}
              onPress={() => setShowLibraryPicker(false)}
            >
              <Text style={styles.modalCloseText}>Close</Text>
            </TouchableOpacity>
          </View>
        </View>
      </Modal>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
    padding: 16,
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 8,
  },
  subtitle: {
    fontSize: 14,
    color: '#666',
    marginBottom: 20,
  },
  image: {
    width: '100%',
    height: 250,
    borderRadius: 8,
    marginBottom: 16,
    backgroundColor: '#e0e0e0',
  },
  buttonContainer: {
    gap: 10,
    marginBottom: 20,
  },
  button: {
    backgroundColor: '#fff',
    paddingVertical: 14,
    paddingHorizontal: 20,
    borderRadius: 8,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#ddd',
  },
  buttonText: {
    fontSize: 16,
    color: '#333',
    fontWeight: '500',
  },
  primaryButton: {
    backgroundColor: '#4A90A4',
    borderColor: '#4A90A4',
  },
  primaryButtonText: {
    fontSize: 16,
    color: '#fff',
    fontWeight: '600',
  },
  disabledButton: {
    opacity: 0.5,
  },
  previewCard: {
    backgroundColor: '#fff',
    borderRadius: 12,
    padding: 16,
    marginBottom: 20,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  previewHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 16,
  },
  previewTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#333',
  },
  sourceBadge: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 12,
  },
  sourceBadgeText: {
    color: '#fff',
    fontSize: 12,
    fontWeight: '500',
  },
  coverImage: {
    width: '100%',
    height: 200,
    marginBottom: 16,
    borderRadius: 8,
  },
  confidenceContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 16,
  },
  confidenceLabel: {
    fontSize: 14,
    color: '#666',
    marginRight: 8,
  },
  confidenceBar: {
    flex: 1,
    height: 8,
    backgroundColor: '#e0e0e0',
    borderRadius: 4,
    overflow: 'hidden',
  },
  confidenceFill: {
    height: '100%',
    backgroundColor: '#4CAF50',
    borderRadius: 4,
  },
  confidenceValue: {
    fontSize: 14,
    color: '#666',
    marginLeft: 8,
    width: 40,
    textAlign: 'right',
  },
  fieldContainer: {
    marginBottom: 12,
  },
  fieldLabel: {
    fontSize: 14,
    color: '#666',
    marginBottom: 4,
    fontWeight: '500',
  },
  input: {
    backgroundColor: '#f5f5f5',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 16,
    color: '#333',
    borderWidth: 1,
    borderColor: '#e0e0e0',
  },
  row: {
    flexDirection: 'row',
  },
  librarySelector: {
    backgroundColor: '#f5f5f5',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 14,
    borderWidth: 1,
    borderColor: '#e0e0e0',
  },
  librarySelectorText: {
    fontSize: 16,
    color: '#333',
  },
  previewActions: {
    flexDirection: 'row',
    gap: 12,
    marginTop: 16,
  },
  cancelButton: {
    flex: 1,
    backgroundColor: '#fff',
    paddingVertical: 14,
    borderRadius: 8,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#ddd',
  },
  cancelButtonText: {
    fontSize: 16,
    color: '#666',
    fontWeight: '500',
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.5)',
    justifyContent: 'flex-end',
  },
  modalContent: {
    backgroundColor: '#fff',
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    padding: 20,
    maxHeight: '60%',
  },
  modalTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#333',
    marginBottom: 16,
    textAlign: 'center',
  },
  libraryList: {
    marginBottom: 16,
  },
  libraryItem: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingVertical: 14,
    paddingHorizontal: 16,
    backgroundColor: '#f5f5f5',
    borderRadius: 8,
    marginBottom: 8,
  },
  libraryItemSelected: {
    backgroundColor: '#E3F2FD',
    borderWidth: 1,
    borderColor: '#4A90A4',
  },
  libraryItemText: {
    fontSize: 16,
    color: '#333',
  },
  publicBadge: {
    fontSize: 12,
    color: '#4A90A4',
    fontWeight: '500',
  },
  modalCloseButton: {
    backgroundColor: '#f5f5f5',
    paddingVertical: 14,
    borderRadius: 8,
    alignItems: 'center',
  },
  modalCloseText: {
    fontSize: 16,
    color: '#666',
    fontWeight: '500',
  },
});
