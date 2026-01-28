import React, { useState, useEffect } from 'react';
import { View, Text, FlatList, StyleSheet, TouchableOpacity } from 'react-native';
import { getLibraries, getLibraryBooks } from '../services/api';
import { Library, LibraryBook } from '../types';

export default function LibrariesScreen({ navigation }: any) {
  const [libraries, setLibraries] = useState<Library[]>([]);
  const [selectedLibrary, setSelectedLibrary] = useState<Library | null>(null);
  const [libraryBooks, setLibraryBooks] = useState<LibraryBook[]>([]);
  const [showPublic, setShowPublic] = useState(false);

  useEffect(() => {
    loadLibraries();
  }, [showPublic]);

  const loadLibraries = async () => {
    try {
      const data = await getLibraries(showPublic ? true : undefined);
      setLibraries(data);
    } catch (error) {
      console.error('Load libraries error:', error);
    }
  };

  const loadLibraryBooks = async (libraryId: number) => {
    try {
      const data = await getLibraryBooks(libraryId);
      setLibraryBooks(data);
    } catch (error) {
      console.error('Load library books error:', error);
    }
  };

  const handleLibraryPress = async (library: Library) => {
    setSelectedLibrary(library);
    await loadLibraryBooks(library.id);
  };

  const renderLibrary = ({ item }: { item: Library }) => (
    <TouchableOpacity
      style={styles.libraryCard}
      onPress={() => handleLibraryPress(item)}
    >
      <Text style={styles.libraryName}>{item.name}</Text>
      <Text style={styles.libraryType}>
        {item.isPublic ? 'Public' : 'Private'}
      </Text>
    </TouchableOpacity>
  );

  const renderBook = ({ item }: { item: LibraryBook }) => (
    <View style={styles.bookCard}>
      <Text style={styles.bookTitle}>{item.book.title}</Text>
      {item.book.author && <Text style={styles.bookAuthor}>{item.book.author}</Text>}
      <Text style={styles.bookStatus}>
        Status: {['Available', 'Checked Out', 'Waiting'][item.status]}
      </Text>
    </View>
  );

  if (selectedLibrary) {
    return (
      <View style={styles.container}>
        <View style={styles.header}>
          <TouchableOpacity style={styles.backButton} onPress={() => setSelectedLibrary(null)}>
            <Text style={styles.backButtonText}>← Back</Text>
          </TouchableOpacity>
          <Text style={styles.headerTitle}>{selectedLibrary.name}</Text>
        </View>
        <FlatList
          data={libraryBooks}
          renderItem={renderBook}
          keyExtractor={(item) => item.id.toString()}
          contentContainerStyle={styles.listContent}
        />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.toggleBar}>
        <TouchableOpacity 
          style={[styles.toggleButton, !showPublic && styles.toggleButtonActive]}
          onPress={() => setShowPublic(false)}
        >
          <Text style={[styles.toggleText, !showPublic && styles.toggleTextActive]}>My Libraries</Text>
        </TouchableOpacity>
        <TouchableOpacity 
          style={[styles.toggleButton, showPublic && styles.toggleButtonActive]}
          onPress={() => setShowPublic(true)}
        >
          <Text style={[styles.toggleText, showPublic && styles.toggleTextActive]}>Public Libraries</Text>
        </TouchableOpacity>
      </View>
      <FlatList
        data={libraries}
        renderItem={renderLibrary}
        keyExtractor={(item) => item.id.toString()}
        contentContainerStyle={styles.listContent}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#121212',
    overflow: 'visible',
  },
  toggleBar: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    padding: 15,
    backgroundColor: '#1E1E1E',
  },
  header: {
    padding: 15,
    backgroundColor: '#1E1E1E',
    flexDirection: 'row',
    alignItems: 'center',
    gap: 15,
  },
  headerTitle: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#FFFFFF',
  },
  listContent: {
    padding: 15,
  },
  libraryCard: {
    backgroundColor: '#1E1E1E',
    padding: 20,
    borderRadius: 8,
    marginBottom: 10,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.3,
    shadowRadius: 4,
    elevation: 3,
  },
  libraryName: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#FFFFFF',
    marginBottom: 5,
  },
  libraryType: {
    fontSize: 14,
    color: '#B0B0B0',
  },
  bookCard: {
    backgroundColor: '#1E1E1E',
    padding: 15,
    borderRadius: 8,
    marginBottom: 10,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.3,
    shadowRadius: 4,
    elevation: 3,
  },
  bookTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#FFFFFF',
    marginBottom: 5,
  },
  bookAuthor: {
    fontSize: 14,
    color: '#B0B0B0',
    marginBottom: 3,
  },
  bookStatus: {
    fontSize: 12,
    color: '#888',
  },
  backButton: {
    backgroundColor: '#C4891E',
    paddingVertical: 8,
    paddingHorizontal: 16,
    borderRadius: 8,
  },
  backButtonText: {
    color: '#1A1A1A',
    fontSize: 14,
    fontWeight: '600',
  },
  toggleButton: {
    paddingVertical: 10,
    paddingHorizontal: 20,
    borderRadius: 8,
    backgroundColor: '#2C2C2C',
  },
  toggleButtonActive: {
    backgroundColor: '#E5A823',
  },
  toggleText: {
    color: '#888',
    fontSize: 14,
    fontWeight: '500',
  },
  toggleTextActive: {
    color: '#1A1A1A',
  },
});
