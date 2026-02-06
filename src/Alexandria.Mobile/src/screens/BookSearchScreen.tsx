import React, { useState, useEffect } from 'react';
import { View, Text, FlatList, StyleSheet, TouchableOpacity, TextInput, Image, Modal, Alert, ActivityIndicator, Switch, ScrollView } from 'react-native';
import { searchBooks, getLibraries, addBookToLibrary, createBook, createLibrary } from '../services/api';
import { Book, Library } from '../types';

const GENRE_OPTIONS = [
  'Fiction', 'Non-Fiction', 'Mystery', 'Science Fiction', 'Fantasy', 
  'Romance', 'Thriller', 'Biography', 'History', 'Self-Help',
  'Science', 'Children', 'Young Adult', 'Horror', 'Poetry', 'Other'
];

export default function BookSearchScreen({ navigation }: any) {
  const [searchQuery, setSearchQuery] = useState('');
  const [books, setBooks] = useState<Book[]>([]);
  const [loading, setLoading] = useState(false);
  const [libraries, setLibraries] = useState<Library[]>([]);
  const [selectedBook, setSelectedBook] = useState<Book | null>(null);
  const [selectedLibrary, setSelectedLibrary] = useState<Library | null>(null);
  const [showLibraryPicker, setShowLibraryPicker] = useState(false);
  const [addingToLibrary, setAddingToLibrary] = useState(false);
  
  // Create library form state
  const [showCreateLibrary, setShowCreateLibrary] = useState(false);
  const [newLibraryName, setNewLibraryName] = useState('');
  const [newLibraryIsPublic, setNewLibraryIsPublic] = useState(false);
  const [creatingLibrary, setCreatingLibrary] = useState(false);
  
  // Genre selection state
  const [selectedGenre, setSelectedGenre] = useState<string | null>(null);
  const [showGenrePicker, setShowGenrePicker] = useState(false);

  useEffect(() => {
    loadLibraries();
  }, []);

  const loadLibraries = async () => {
    try {
      const libs = await getLibraries();
      setLibraries(libs);
    } catch (error) {
      console.error('Failed to load libraries:', error);
    }
  };

  const handleSearch = async () => {
    if (!searchQuery.trim()) return;
    
    setLoading(true);
    try {
      const results = await searchBooks({ query: searchQuery });
      setBooks(results);
    } catch (error) {
      console.error('Search error:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleAddToLibrary = (book: Book) => {
    setSelectedBook(book);
    setSelectedGenre(book.genre || null);
    setSelectedLibrary(null);
    setShowLibraryPicker(true);
  };

  const handleCreateLibrary = async () => {
    if (!newLibraryName.trim()) {
      Alert.alert('Error', 'Please enter a library name');
      return;
    }
    
    setCreatingLibrary(true);
    try {
      const newLibrary = await createLibrary({
        name: newLibraryName.trim(),
        isPublic: newLibraryIsPublic,
      });
      setLibraries([...libraries, newLibrary]);
      setShowCreateLibrary(false);
      setNewLibraryName('');
      setNewLibraryIsPublic(false);
      Alert.alert('Success', `Library "${newLibrary.name}" created!`);
    } catch (error) {
      console.error('Failed to create library:', error);
      Alert.alert('Error', 'Failed to create library. Please try again.');
    } finally {
      setCreatingLibrary(false);
    }
  };

  const confirmAddToLibrary = async (forceAdd: boolean = false) => {
    if (!selectedBook || !selectedLibrary) {
      Alert.alert('Error', 'Please select a library');
      return;
    }
    
    setAddingToLibrary(true);
    try {
      let bookId = selectedBook.id;
      
      // If the book doesn't exist in our database (id is 0), create it first
      if (bookId === 0) {
        const newBook = await createBook({
          title: selectedBook.title,
          author: selectedBook.author,
          isbn: selectedBook.isbn,
          publisher: selectedBook.publisher,
          publishedYear: selectedBook.publishedYear,
          description: selectedBook.description,
          coverImageUrl: selectedBook.coverImageUrl,
          genre: selectedGenre || selectedBook.genre,
          pageCount: selectedBook.pageCount,
        });
        bookId = newBook.id;
      }
      
      await addBookToLibrary(selectedLibrary.id, bookId, 0, forceAdd);
      Alert.alert('Success', `"${selectedBook.title}" has been added to ${selectedLibrary.name}!`);
      setShowLibraryPicker(false);
      setSelectedBook(null);
      setSelectedLibrary(null);
      setSelectedGenre(null);
    } catch (error: any) {
      console.error('Failed to add book to library:', error);
      
      // Check if this is a duplicate book conflict (409)
      if (error?.response?.status === 409 && error?.response?.data?.isDuplicate) {
        Alert.alert(
          'Book Already in Library',
          'This book is already in your library. Do you want to add another copy?',
          [
            { text: 'Cancel', style: 'cancel' },
            { 
              text: 'Add Another Copy', 
              onPress: () => confirmAddToLibrary(true)
            }
          ]
        );
      } else {
        Alert.alert('Error', 'Failed to add book to library. Please try again.');
      }
    } finally {
      setAddingToLibrary(false);
    }
  };

  const renderBook = ({ item }: { item: Book }) => (
    <View style={styles.bookCard}>
      <View style={styles.bookContent}>
        {item.coverImageUrl ? (
          <Image source={{ uri: item.coverImageUrl }} style={styles.coverImage} />
        ) : (
          <View style={styles.placeholderCover}>
            <Text style={styles.placeholderText}>No Cover</Text>
          </View>
        )}
        <View style={styles.bookInfo}>
          <Text style={styles.bookTitle} numberOfLines={2}>{item.title}</Text>
          {item.author && <Text style={styles.bookAuthor} numberOfLines={1}>{item.author}</Text>}
          {item.publishedYear && <Text style={styles.bookYear}>{item.publishedYear}</Text>}
          {item.isbn && <Text style={styles.bookIsbn}>ISBN: {item.isbn}</Text>}
          {item.description && (
            <Text style={styles.bookDescription} numberOfLines={2}>{item.description}</Text>
          )}
        </View>
      </View>
      <TouchableOpacity 
        style={styles.addButton} 
        onPress={() => handleAddToLibrary(item)}
      >
        <Text style={styles.addButtonText}>+ Add to Library</Text>
      </TouchableOpacity>
    </View>
  );

  const renderGenreOption = (genre: string) => (
    <TouchableOpacity 
      key={genre}
      style={[
        styles.genreChip,
        selectedGenre === genre && styles.genreChipSelected
      ]} 
      onPress={() => {
        setSelectedGenre(genre);
        setShowGenrePicker(false);
      }}
    >
      <Text style={[
        styles.genreChipText,
        selectedGenre === genre && styles.genreChipTextSelected
      ]}>{genre}</Text>
    </TouchableOpacity>
  );

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <View style={styles.searchBox}>
          <TextInput
            style={styles.searchInput}
            placeholder="Search for books..."
            placeholderTextColor="#888"
            value={searchQuery}
            onChangeText={setSearchQuery}
            onSubmitEditing={handleSearch}
          />
          <TouchableOpacity style={styles.button} onPress={handleSearch}>
            <Text style={styles.buttonText}>Search</Text>
          </TouchableOpacity>
        </View>
        
        <TouchableOpacity 
          style={styles.imageSearchButton} 
          onPress={() => navigation.navigate('ImageSearch')}
        >
          <Text style={styles.buttonText}>Search by Image</Text>
        </TouchableOpacity>
      </View>
      
      {loading ? (
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color="#E5A823" />
          <Text style={styles.loadingText}>Searching...</Text>
        </View>
      ) : books.length === 0 ? (
        <View style={styles.emptyContainer}>
          <Text style={styles.emptyText}>Search for books by title, author, or ISBN</Text>
        </View>
      ) : (
        <FlatList
          data={books}
          renderItem={renderBook}
          keyExtractor={(item, index) => `${item.id}-${item.isbn || index}`}
          style={styles.list}
          contentContainerStyle={styles.listContent}
        />
      )}

      {/* Library Picker Modal */}
      <Modal
        visible={showLibraryPicker}
        transparent
        animationType="slide"
        onRequestClose={() => {
          setShowLibraryPicker(false);
          setSelectedBook(null);
          setSelectedGenre(null);
        }}
      >
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <Text style={styles.modalTitle}>Add to Library</Text>
            {selectedBook && (
              <Text style={styles.modalSubtitle} numberOfLines={1}>"{selectedBook.title}"</Text>
            )}
            
            {addingToLibrary ? (
              <View style={styles.modalLoading}>
                <ActivityIndicator size="large" color="#E5A823" />
                <Text style={styles.loadingText}>Adding to library...</Text>
              </View>
            ) : (
              <ScrollView style={styles.modalScrollView}>
                {/* Genre Selection */}
                <View style={styles.sectionContainer}>
                  <Text style={styles.sectionTitle}>Genre (optional)</Text>
                  <TouchableOpacity 
                    style={styles.genreSelector}
                    onPress={() => setShowGenrePicker(!showGenrePicker)}
                  >
                    <Text style={styles.genreSelectorText}>
                      {selectedGenre || 'Select a genre...'}
                    </Text>
                    <Text style={styles.genreSelectorArrow}>{showGenrePicker ? '▲' : '▼'}</Text>
                  </TouchableOpacity>
                  
                  {showGenrePicker && (
                    <View style={styles.genreList}>
                      {GENRE_OPTIONS.map(renderGenreOption)}
                    </View>
                  )}
                </View>

                {/* Library Selection */}
                <View style={styles.sectionContainer}>
                  <Text style={styles.sectionTitle}>Select Library</Text>
                  
                  {libraries.length === 0 ? (
                    <Text style={styles.noLibrariesText}>
                      You don't have any libraries yet. Create one below!
                    </Text>
                  ) : (
                    libraries.map((library) => (
                      <TouchableOpacity 
                        key={library.id}
                        style={[
                          styles.libraryOption,
                          selectedLibrary?.id === library.id && styles.libraryOptionSelected
                        ]} 
                        onPress={() => setSelectedLibrary(library)}
                        disabled={addingToLibrary}
                      >
                        <View style={styles.libraryOptionContent}>
                          <Text style={[
                            styles.libraryOptionText,
                            selectedLibrary?.id === library.id && styles.libraryOptionTextSelected
                          ]}>{library.name}</Text>
                          <Text style={styles.libraryOptionSubtext}>
                            {library.isPublic ? '🌐 Public' : '🔒 Private'}
                          </Text>
                        </View>
                        {selectedLibrary?.id === library.id && (
                          <Text style={styles.checkmark}>✓</Text>
                        )}
                      </TouchableOpacity>
                    ))
                  )}
                  
                  {/* Create New Library Button */}
                  <TouchableOpacity 
                    style={styles.createLibraryButton}
                    onPress={() => setShowCreateLibrary(true)}
                  >
                    <Text style={styles.createLibraryButtonText}>+ Create New Library</Text>
                  </TouchableOpacity>
                </View>
              </ScrollView>
            )}
            
            <View style={styles.modalButtonRow}>
              <TouchableOpacity 
                style={styles.cancelButton} 
                onPress={() => {
                  setShowLibraryPicker(false);
                  setSelectedBook(null);
                  setSelectedLibrary(null);
                  setSelectedGenre(null);
                  setShowGenrePicker(false);
                }}
              >
                <Text style={styles.cancelButtonText}>Cancel</Text>
              </TouchableOpacity>
              
              <TouchableOpacity 
                style={[
                  styles.saveButton,
                  !selectedLibrary && styles.saveButtonDisabled
                ]} 
                onPress={confirmAddToLibrary}
                disabled={!selectedLibrary || addingToLibrary}
              >
                <Text style={styles.saveButtonText}>Save to Library</Text>
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>

      {/* Create Library Modal */}
      <Modal
        visible={showCreateLibrary}
        transparent
        animationType="fade"
        onRequestClose={() => setShowCreateLibrary(false)}
      >
        <View style={styles.modalOverlay}>
          <View style={styles.createLibraryModal}>
            <Text style={styles.modalTitle}>Create New Library</Text>
            
            {creatingLibrary ? (
              <View style={styles.modalLoading}>
                <ActivityIndicator size="large" color="#E5A823" />
                <Text style={styles.loadingText}>Creating library...</Text>
              </View>
            ) : (
              <>
                <View style={styles.formGroup}>
                  <Text style={styles.formLabel}>Library Name</Text>
                  <TextInput
                    style={styles.formInput}
                    placeholder="My Book Collection"
                    placeholderTextColor="#666"
                    value={newLibraryName}
                    onChangeText={setNewLibraryName}
                  />
                </View>
                
                <View style={styles.formGroup}>
                  <Text style={styles.formLabel}>Visibility</Text>
                  <View style={styles.switchRow}>
                    <Text style={styles.switchLabel}>
                      {newLibraryIsPublic ? '🌐 Public - Others can see your library' : '🔒 Private - Only you can see'}
                    </Text>
                    <Switch
                      value={newLibraryIsPublic}
                      onValueChange={setNewLibraryIsPublic}
                      trackColor={{ false: '#444', true: '#E5A823' }}
                      thumbColor={newLibraryIsPublic ? '#FFF' : '#888'}
                    />
                  </View>
                </View>
                
                <View style={styles.createLibraryActions}>
                  <TouchableOpacity 
                    style={styles.cancelButton}
                    onPress={() => {
                      setShowCreateLibrary(false);
                      setNewLibraryName('');
                      setNewLibraryIsPublic(false);
                    }}
                  >
                    <Text style={styles.cancelButtonText}>Cancel</Text>
                  </TouchableOpacity>
                  
                  <TouchableOpacity 
                    style={styles.confirmButton}
                    onPress={handleCreateLibrary}
                  >
                    <Text style={styles.confirmButtonText}>Create Library</Text>
                  </TouchableOpacity>
                </View>
              </>
            )}
          </View>
        </View>
      </Modal>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#121212',
  },
  header: {
    backgroundColor: '#1E1E1E',
    paddingBottom: 15,
  },
  searchBox: {
    flexDirection: 'row',
    padding: 15,
    alignItems: 'center',
    gap: 10,
  },
  searchInput: {
    flex: 1,
    padding: 10,
    backgroundColor: '#2C2C2C',
    borderRadius: 8,
    fontSize: 16,
    color: '#FFFFFF',
  },
  list: {
    flex: 1,
  },
  listContent: {
    padding: 15,
  },
  bookCard: {
    backgroundColor: '#1E1E1E',
    borderRadius: 8,
    marginBottom: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.3,
    shadowRadius: 4,
    elevation: 3,
    overflow: 'hidden',
  },
  bookContent: {
    flexDirection: 'row',
    padding: 12,
  },
  coverImage: {
    width: 80,
    height: 120,
    borderRadius: 4,
    backgroundColor: '#2C2C2C',
  },
  placeholderCover: {
    width: 80,
    height: 120,
    borderRadius: 4,
    backgroundColor: '#2C2C2C',
    justifyContent: 'center',
    alignItems: 'center',
  },
  placeholderText: {
    color: '#666',
    fontSize: 10,
  },
  bookInfo: {
    flex: 1,
    marginLeft: 12,
  },
  bookTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#FFFFFF',
    marginBottom: 4,
  },
  bookAuthor: {
    fontSize: 14,
    color: '#B0B0B0',
    marginBottom: 2,
  },
  bookYear: {
    fontSize: 12,
    color: '#888',
    marginBottom: 2,
  },
  bookIsbn: {
    fontSize: 11,
    color: '#666',
    marginBottom: 4,
  },
  bookDescription: {
    fontSize: 12,
    color: '#999',
    marginTop: 4,
  },
  addButton: {
    backgroundColor: '#E5A823',
    padding: 12,
    alignItems: 'center',
  },
  addButtonText: {
    color: '#1A1A1A',
    fontSize: 14,
    fontWeight: '600',
  },
  button: {
    backgroundColor: '#E5A823',
    paddingVertical: 10,
    paddingHorizontal: 20,
    borderRadius: 8,
    alignItems: 'center',
  },
  imageSearchButton: {
    backgroundColor: '#C4891E',
    padding: 12,
    marginHorizontal: 15,
    borderRadius: 8,
    alignItems: 'center',
  },
  buttonText: {
    color: '#1A1A1A',
    fontSize: 14,
    fontWeight: '600',
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  loadingText: {
    color: '#B0B0B0',
    marginTop: 10,
    fontSize: 14,
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20,
  },
  emptyText: {
    color: '#666',
    fontSize: 16,
    textAlign: 'center',
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.7)',
    justifyContent: 'flex-end',
  },
  modalContent: {
    backgroundColor: '#1E1E1E',
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    padding: 20,
    maxHeight: '80%',
  },
  modalScrollView: {
    maxHeight: 400,
  },
  modalTitle: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#FFFFFF',
    textAlign: 'center',
    marginBottom: 5,
  },
  modalSubtitle: {
    fontSize: 14,
    color: '#B0B0B0',
    textAlign: 'center',
    marginBottom: 15,
  },
  modalLoading: {
    padding: 30,
    alignItems: 'center',
  },
  sectionContainer: {
    marginBottom: 20,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#FFFFFF',
    marginBottom: 10,
  },
  libraryList: {
    maxHeight: 250,
  },
  libraryOption: {
    backgroundColor: '#2C2C2C',
    padding: 15,
    borderRadius: 8,
    marginBottom: 10,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  libraryOptionSelected: {
    backgroundColor: '#3D3D3D',
    borderWidth: 2,
    borderColor: '#E5A823',
  },
  libraryOptionContent: {
    flex: 1,
  },
  libraryOptionText: {
    fontSize: 16,
    color: '#FFFFFF',
    fontWeight: '500',
  },
  libraryOptionTextSelected: {
    color: '#E5A823',
  },
  libraryOptionSubtext: {
    fontSize: 12,
    color: '#888',
    marginTop: 2,
  },
  checkmark: {
    fontSize: 20,
    color: '#E5A823',
    fontWeight: 'bold',
    marginLeft: 10,
  },
  noLibrariesText: {
    color: '#888',
    fontSize: 14,
    textAlign: 'center',
    padding: 15,
  },
  createLibraryButton: {
    backgroundColor: '#333',
    padding: 15,
    borderRadius: 8,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#E5A823',
    borderStyle: 'dashed',
    marginTop: 5,
  },
  createLibraryButtonText: {
    color: '#E5A823',
    fontSize: 14,
    fontWeight: '600',
  },
  modalButtonRow: {
    flexDirection: 'row',
    marginTop: 15,
    gap: 10,
  },
  cancelButton: {
    backgroundColor: '#333',
    padding: 15,
    borderRadius: 8,
    alignItems: 'center',
    flex: 1,
  },
  cancelButtonText: {
    color: '#FFFFFF',
    fontSize: 16,
  },
  saveButton: {
    backgroundColor: '#E5A823',
    padding: 15,
    borderRadius: 8,
    alignItems: 'center',
    flex: 1,
  },
  saveButtonDisabled: {
    backgroundColor: '#666',
    opacity: 0.6,
  },
  saveButtonText: {
    color: '#1A1A1A',
    fontSize: 16,
    fontWeight: '600',
  },
  // Genre picker styles
  genreSelector: {
    backgroundColor: '#2C2C2C',
    padding: 12,
    borderRadius: 8,
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  genreSelectorText: {
    color: '#FFFFFF',
    fontSize: 14,
  },
  genreSelectorArrow: {
    color: '#888',
    fontSize: 12,
  },
  genreList: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    marginTop: 10,
    gap: 8,
  },
  genreChip: {
    backgroundColor: '#2C2C2C',
    paddingVertical: 8,
    paddingHorizontal: 14,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: '#444',
  },
  genreChipSelected: {
    backgroundColor: '#E5A823',
    borderColor: '#E5A823',
  },
  genreChipText: {
    color: '#FFFFFF',
    fontSize: 13,
  },
  genreChipTextSelected: {
    color: '#1A1A1A',
    fontWeight: '600',
  },
  // Create library modal styles
  createLibraryModal: {
    backgroundColor: '#1E1E1E',
    margin: 20,
    borderRadius: 16,
    padding: 20,
    alignSelf: 'center',
    width: '90%',
    maxWidth: 400,
  },
  formGroup: {
    marginBottom: 20,
  },
  formLabel: {
    fontSize: 14,
    fontWeight: '600',
    color: '#FFFFFF',
    marginBottom: 8,
  },
  formInput: {
    backgroundColor: '#2C2C2C',
    padding: 12,
    borderRadius: 8,
    fontSize: 16,
    color: '#FFFFFF',
  },
  switchRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    backgroundColor: '#2C2C2C',
    padding: 12,
    borderRadius: 8,
  },
  switchLabel: {
    flex: 1,
    color: '#B0B0B0',
    fontSize: 13,
    marginRight: 10,
  },
  createLibraryActions: {
    flexDirection: 'row',
    marginTop: 10,
  },
  confirmButton: {
    backgroundColor: '#E5A823',
    padding: 15,
    borderRadius: 8,
    alignItems: 'center',
    flex: 1,
    marginLeft: 5,
  },
  confirmButtonText: {
    color: '#1A1A1A',
    fontSize: 16,
    fontWeight: '600',
  },
});
