import React, { useState, useEffect } from 'react';
import { View, Text, FlatList, StyleSheet, TouchableOpacity, Button } from 'react-native';
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
          <Button title="← Back" onPress={() => setSelectedLibrary(null)} />
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
        <Button
          title="My Libraries"
          onPress={() => setShowPublic(false)}
        />
        <Button
          title="Public Libraries"
          onPress={() => setShowPublic(true)}
        />
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
    backgroundColor: '#f5f5f5',
  },
  toggleBar: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    padding: 15,
    backgroundColor: 'white',
  },
  header: {
    padding: 15,
    backgroundColor: 'white',
    flexDirection: 'row',
    alignItems: 'center',
    gap: 15,
  },
  headerTitle: {
    fontSize: 20,
    fontWeight: 'bold',
  },
  listContent: {
    padding: 15,
  },
  libraryCard: {
    backgroundColor: 'white',
    padding: 20,
    borderRadius: 8,
    marginBottom: 10,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  libraryName: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 5,
  },
  libraryType: {
    fontSize: 14,
    color: '#666',
  },
  bookCard: {
    backgroundColor: 'white',
    padding: 15,
    borderRadius: 8,
    marginBottom: 10,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  bookTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 5,
  },
  bookAuthor: {
    fontSize: 14,
    color: '#666',
    marginBottom: 3,
  },
  bookStatus: {
    fontSize: 12,
    color: '#999',
  },
});
