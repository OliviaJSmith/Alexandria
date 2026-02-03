import React, { useState, useEffect } from 'react';
import { View, Text, FlatList, StyleSheet, TouchableOpacity, Modal, TextInput, Alert, ScrollView, Image } from 'react-native';
import { getLibraries, getLibraryBooks, removeBookFromLibrary, updateLibraryBook, moveBookToLibrary } from '../services/api';
import { Library, LibraryBook, BookStatus } from '../types';

const GENRES = [
  'Fiction', 'Non-Fiction', 'Science Fiction', 'Fantasy', 'Mystery',
  'Romance', 'Thriller', 'Biography', 'History', 'Self-Help',
  'Science', 'Technology', 'Art', 'Poetry', 'Children'
];

export default function LibrariesScreen({ navigation }: any) {
  const [libraries, setLibraries] = useState<Library[]>([]);
  const [selectedLibrary, setSelectedLibrary] = useState<Library | null>(null);
  const [libraryBooks, setLibraryBooks] = useState<LibraryBook[]>([]);
  const [showPublic, setShowPublic] = useState(false);
  
  // Edit modal state
  const [editingBook, setEditingBook] = useState<LibraryBook | null>(null);
  const [showEditModal, setShowEditModal] = useState(false);
  const [editStatus, setEditStatus] = useState<BookStatus>(BookStatus.Available);
  const [editGenre, setEditGenre] = useState<string>('');
  const [editLoanNote, setEditLoanNote] = useState<string>('');
  const [showMoveModal, setShowMoveModal] = useState(false);
  const [saving, setSaving] = useState(false);

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

  const handleBookPress = (book: LibraryBook) => {
    setEditingBook(book);
    setEditStatus(book.status);
    setEditGenre(book.book.genre || '');
    setEditLoanNote(book.loanNote || '');
    setShowEditModal(true);
  };

  const handleDeleteBook = () => {
    if (!editingBook || !selectedLibrary) return;
    
    Alert.alert(
      'Delete Book',
      `Are you sure you want to remove "${editingBook.book.title}" from this library?`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Delete',
          style: 'destructive',
          onPress: async () => {
            try {
              await removeBookFromLibrary(selectedLibrary.id, editingBook.id);
              setShowEditModal(false);
              setEditingBook(null);
              await loadLibraryBooks(selectedLibrary.id);
            } catch (error) {
              console.error('Delete book error:', error);
              Alert.alert('Error', 'Failed to delete book');
            }
          },
        },
      ]
    );
  };

  const handleSaveChanges = async () => {
    if (!editingBook || !selectedLibrary) return;
    
    setSaving(true);
    try {
      await updateLibraryBook(selectedLibrary.id, editingBook.id, {
        status: editStatus,
        genre: editGenre || undefined,
        loanNote: editStatus === BookStatus.CheckedOut ? editLoanNote : '',
      });
      setShowEditModal(false);
      setEditingBook(null);
      await loadLibraryBooks(selectedLibrary.id);
    } catch (error) {
      console.error('Update book error:', error);
      Alert.alert('Error', 'Failed to update book');
    } finally {
      setSaving(false);
    }
  };

  const handleMoveBook = async (targetLibrary: Library) => {
    if (!editingBook || !selectedLibrary) return;
    
    setSaving(true);
    try {
      await moveBookToLibrary(selectedLibrary.id, editingBook.id, targetLibrary.id);
      setShowMoveModal(false);
      setShowEditModal(false);
      setEditingBook(null);
      await loadLibraryBooks(selectedLibrary.id);
      Alert.alert('Success', `Book moved to "${targetLibrary.name}"`);
    } catch (error) {
      console.error('Move book error:', error);
      Alert.alert('Error', 'Failed to move book');
    } finally {
      setSaving(false);
    }
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
    <TouchableOpacity style={styles.bookCard} onPress={() => handleBookPress(item)}>
      <View style={styles.bookRow}>
        {item.book.coverImageUrl && (
          <Image source={{ uri: item.book.coverImageUrl }} style={styles.bookCover} />
        )}
        <View style={styles.bookInfo}>
          <Text style={styles.bookTitle}>{item.book.title}</Text>
          {item.book.author && <Text style={styles.bookAuthor}>{item.book.author}</Text>}
          {item.book.genre && <Text style={styles.bookGenre}>{item.book.genre}</Text>}
          <Text style={[styles.bookStatus, item.status === BookStatus.CheckedOut && styles.statusLoaned]}>
            Status: {['Available', 'Loaned Out', 'Waiting'][item.status]}
          </Text>
          {item.status === BookStatus.CheckedOut && item.loanNote && (
            <Text style={styles.loanNote}>Loaned to: {item.loanNote}</Text>
          )}
        </View>
      </View>
      <Text style={styles.tapToEdit}>Tap to edit</Text>
    </TouchableOpacity>
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
          ListEmptyComponent={
            <Text style={styles.emptyText}>No books in this library yet</Text>
          }
        />

        {/* Edit Book Modal */}
        <Modal visible={showEditModal} transparent animationType="slide">
          <View style={styles.modalOverlay}>
            <View style={styles.modalContent}>
              <ScrollView showsVerticalScrollIndicator={false}>
                <Text style={styles.modalTitle}>Edit Book</Text>
                {editingBook && (
                  <Text style={styles.modalSubtitle}>{editingBook.book.title}</Text>
                )}

                {/* Status Selection */}
                <Text style={styles.sectionLabel}>Status</Text>
                <View style={styles.statusOptions}>
                  {[
                    { value: BookStatus.Available, label: 'Available' },
                    { value: BookStatus.CheckedOut, label: 'Loaned Out' },
                    { value: BookStatus.WaitingToBeLoanedOut, label: 'Waiting' },
                  ].map((option) => (
                    <TouchableOpacity
                      key={option.value}
                      style={[
                        styles.statusOption,
                        editStatus === option.value && styles.statusOptionSelected,
                      ]}
                      onPress={() => setEditStatus(option.value)}
                    >
                      <Text
                        style={[
                          styles.statusOptionText,
                          editStatus === option.value && styles.statusOptionTextSelected,
                        ]}
                      >
                        {option.label}
                      </Text>
                    </TouchableOpacity>
                  ))}
                </View>

                {/* Loan Note (only shown when status is Checked Out) */}
                {editStatus === BookStatus.CheckedOut && (
                  <>
                    <Text style={styles.sectionLabel}>Loaned To</Text>
                    <TextInput
                      style={styles.textInput}
                      placeholder="Enter name of borrower..."
                      placeholderTextColor="#888"
                      value={editLoanNote}
                      onChangeText={setEditLoanNote}
                    />
                  </>
                )}

                {/* Genre Selection */}
                <Text style={styles.sectionLabel}>Genre</Text>
                <View style={styles.genreGrid}>
                  {GENRES.map((genre) => (
                    <TouchableOpacity
                      key={genre}
                      style={[
                        styles.genreChip,
                        editGenre === genre && styles.genreChipSelected,
                      ]}
                      onPress={() => setEditGenre(editGenre === genre ? '' : genre)}
                    >
                      <Text
                        style={[
                          styles.genreChipText,
                          editGenre === genre && styles.genreChipTextSelected,
                        ]}
                      >
                        {genre}
                      </Text>
                    </TouchableOpacity>
                  ))}
                </View>

                {/* Action Buttons */}
                <View style={styles.actionButtons}>
                  <TouchableOpacity
                    style={styles.moveButton}
                    onPress={() => setShowMoveModal(true)}
                  >
                    <Text style={styles.moveButtonText}>Move to Library</Text>
                  </TouchableOpacity>
                  <TouchableOpacity
                    style={styles.deleteButton}
                    onPress={handleDeleteBook}
                  >
                    <Text style={styles.deleteButtonText}>Delete</Text>
                  </TouchableOpacity>
                </View>

                {/* Save/Cancel */}
                <View style={styles.modalButtonRow}>
                  <TouchableOpacity
                    style={styles.cancelButton}
                    onPress={() => {
                      setShowEditModal(false);
                      setEditingBook(null);
                    }}
                  >
                    <Text style={styles.cancelButtonText}>Cancel</Text>
                  </TouchableOpacity>
                  <TouchableOpacity
                    style={[styles.saveButton, saving && styles.buttonDisabled]}
                    onPress={handleSaveChanges}
                    disabled={saving}
                  >
                    <Text style={styles.saveButtonText}>
                      {saving ? 'Saving...' : 'Save'}
                    </Text>
                  </TouchableOpacity>
                </View>
              </ScrollView>
            </View>
          </View>
        </Modal>

        {/* Move Book Modal */}
        <Modal visible={showMoveModal} transparent animationType="slide">
          <View style={styles.modalOverlay}>
            <View style={styles.modalContent}>
              <Text style={styles.modalTitle}>Move to Library</Text>
              <Text style={styles.modalSubtitle}>Select destination library</Text>
              
              <ScrollView style={styles.libraryList}>
                {libraries
                  .filter((lib) => lib.id !== selectedLibrary.id)
                  .map((lib) => (
                    <TouchableOpacity
                      key={lib.id}
                      style={styles.libraryOption}
                      onPress={() => handleMoveBook(lib)}
                      disabled={saving}
                    >
                      <Text style={styles.libraryOptionText}>{lib.name}</Text>
                      <Text style={styles.libraryOptionSubtext}>
                        {lib.isPublic ? 'Public' : 'Private'}
                      </Text>
                    </TouchableOpacity>
                  ))}
                {libraries.filter((lib) => lib.id !== selectedLibrary.id).length === 0 && (
                  <Text style={styles.emptyText}>No other libraries available</Text>
                )}
              </ScrollView>

              <TouchableOpacity
                style={styles.cancelButton}
                onPress={() => setShowMoveModal(false)}
              >
                <Text style={styles.cancelButtonText}>Cancel</Text>
              </TouchableOpacity>
            </View>
          </View>
        </Modal>
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
  bookRow: {
    flexDirection: 'row',
  },
  bookCover: {
    width: 60,
    height: 90,
    borderRadius: 4,
    marginRight: 12,
  },
  bookInfo: {
    flex: 1,
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
  bookGenre: {
    fontSize: 12,
    color: '#888',
    marginBottom: 3,
  },
  bookStatus: {
    fontSize: 12,
    color: '#4CAF50',
    marginTop: 4,
  },
  statusLoaned: {
    color: '#FF9800',
  },
  loanNote: {
    fontSize: 12,
    color: '#FF9800',
    fontStyle: 'italic',
    marginTop: 2,
  },
  tapToEdit: {
    fontSize: 11,
    color: '#666',
    textAlign: 'right',
    marginTop: 8,
  },
  emptyText: {
    color: '#888',
    textAlign: 'center',
    marginTop: 40,
    fontSize: 16,
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
  // Modal styles
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.8)',
    justifyContent: 'flex-end',
  },
  modalContent: {
    backgroundColor: '#1E1E1E',
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    padding: 20,
    maxHeight: '85%',
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
    marginBottom: 20,
  },
  sectionLabel: {
    fontSize: 14,
    fontWeight: '600',
    color: '#FFFFFF',
    marginTop: 15,
    marginBottom: 10,
  },
  statusOptions: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  statusOption: {
    paddingVertical: 10,
    paddingHorizontal: 16,
    borderRadius: 8,
    backgroundColor: '#2C2C2C',
    borderWidth: 1,
    borderColor: '#3C3C3C',
  },
  statusOptionSelected: {
    backgroundColor: '#E5A823',
    borderColor: '#E5A823',
  },
  statusOptionText: {
    color: '#FFFFFF',
    fontSize: 14,
  },
  statusOptionTextSelected: {
    color: '#1A1A1A',
    fontWeight: '600',
  },
  textInput: {
    backgroundColor: '#2C2C2C',
    borderRadius: 8,
    padding: 12,
    color: '#FFFFFF',
    fontSize: 16,
    borderWidth: 1,
    borderColor: '#3C3C3C',
  },
  genreGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  genreChip: {
    paddingVertical: 8,
    paddingHorizontal: 12,
    borderRadius: 16,
    backgroundColor: '#2C2C2C',
    borderWidth: 1,
    borderColor: '#3C3C3C',
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
  actionButtons: {
    flexDirection: 'row',
    gap: 10,
    marginTop: 20,
  },
  moveButton: {
    flex: 1,
    backgroundColor: '#2196F3',
    paddingVertical: 12,
    borderRadius: 8,
    alignItems: 'center',
  },
  moveButtonText: {
    color: '#FFFFFF',
    fontSize: 14,
    fontWeight: '600',
  },
  deleteButton: {
    flex: 1,
    backgroundColor: '#F44336',
    paddingVertical: 12,
    borderRadius: 8,
    alignItems: 'center',
  },
  deleteButtonText: {
    color: '#FFFFFF',
    fontSize: 14,
    fontWeight: '600',
  },
  modalButtonRow: {
    flexDirection: 'row',
    gap: 10,
    marginTop: 20,
  },
  cancelButton: {
    flex: 1,
    backgroundColor: '#2C2C2C',
    paddingVertical: 14,
    borderRadius: 8,
    alignItems: 'center',
  },
  cancelButtonText: {
    color: '#FFFFFF',
    fontSize: 16,
    fontWeight: '600',
  },
  saveButton: {
    flex: 1,
    backgroundColor: '#E5A823',
    paddingVertical: 14,
    borderRadius: 8,
    alignItems: 'center',
  },
  saveButtonText: {
    color: '#1A1A1A',
    fontSize: 16,
    fontWeight: '600',
  },
  buttonDisabled: {
    opacity: 0.6,
  },
  libraryList: {
    maxHeight: 300,
  },
  libraryOption: {
    backgroundColor: '#2C2C2C',
    padding: 15,
    borderRadius: 8,
    marginBottom: 8,
  },
  libraryOptionText: {
    color: '#FFFFFF',
    fontSize: 16,
    fontWeight: '500',
  },
  libraryOptionSubtext: {
    color: '#888',
    fontSize: 12,
    marginTop: 4,
  },
});
