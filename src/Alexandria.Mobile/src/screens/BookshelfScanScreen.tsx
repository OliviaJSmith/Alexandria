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
    FlatList,
} from 'react-native';
import * as ImagePicker from 'expo-image-picker';
import { scanBookshelf, getLibraries, confirmBooksToLibrary, createLibrary } from '../services/api';
import { BookPreview, Library, BookSource } from '../types';

export default function BookshelfScanScreen() {
    const [imageUri, setImageUri] = useState<string | null>(null);
    const [bookPreviews, setBookPreviews] = useState<BookPreview[]>([]);
    const [loading, setLoading] = useState(false);
    const [saving, setSaving] = useState(false);
    const [libraries, setLibraries] = useState<Library[]>([]);
    const [selectedLibraryId, setSelectedLibraryId] = useState<number | null>(null);
    const [showLibraryPicker, setShowLibraryPicker] = useState(false);
    const [editingIndex, setEditingIndex] = useState<number | null>(null);
    const [showCreateLibrary, setShowCreateLibrary] = useState(false);
    const [newLibraryName, setNewLibraryName] = useState('');
    const [newLibraryIsPublic, setNewLibraryIsPublic] = useState(false);
    const [creatingLibrary, setCreatingLibrary] = useState(false);

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
            setSelectedLibraryId(newLibrary.id);
            setShowCreateLibrary(false);
            setShowLibraryPicker(false);
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

    const pickImage = async () => {
        const { status } = await ImagePicker.requestMediaLibraryPermissionsAsync();
        if (status !== 'granted') {
            Alert.alert('Permission Denied', 'We need camera roll permissions to scan bookshelves');
            return;
        }

        const result = await ImagePicker.launchImageLibraryAsync({
            mediaTypes: ImagePicker.MediaTypeOptions.Images,
            allowsEditing: false, // Don't edit for bookshelf - want full image
            quality: 0.9,
        });

        if (!result.canceled && result.assets[0]) {
            setImageUri(result.assets[0].uri);
            setBookPreviews([]);
        }
    };

    const takePhoto = async () => {
        const { status } = await ImagePicker.requestCameraPermissionsAsync();
        if (status !== 'granted') {
            Alert.alert('Permission Denied', 'We need camera permissions to take photos');
            return;
        }

        const result = await ImagePicker.launchCameraAsync({
            allowsEditing: false,
            quality: 0.9,
        });

        if (!result.canceled && result.assets[0]) {
            setImageUri(result.assets[0].uri);
            setBookPreviews([]);
        }
    };

    const handleScan = async () => {
        if (!imageUri) {
            Alert.alert('No Image', 'Please select or take a photo first');
            return;
        }

        setLoading(true);
        try {
            const results = await scanBookshelf(imageUri);
            // Mark all as selected by default
            const withSelection = results.map(book => ({ ...book, selected: true }));
            setBookPreviews(withSelection);

            if (results.length === 0) {
                Alert.alert('No Books Found', 'Could not identify any books in this image. Try a clearer photo with visible book spines or covers.');
            }
        } catch (error: any) {
            console.error('Scan error:', error);
            Alert.alert('Error', 'Failed to scan bookshelf. Please try again.');
        } finally {
            setLoading(false);
        }
    };

    const toggleBookSelection = (index: number) => {
        const updated = [...bookPreviews];
        updated[index] = { ...updated[index], selected: !updated[index].selected };
        setBookPreviews(updated);
    };

    const selectAll = () => {
        setBookPreviews(bookPreviews.map(book => ({ ...book, selected: true })));
    };

    const deselectAll = () => {
        setBookPreviews(bookPreviews.map(book => ({ ...book, selected: false })));
    };

    const updateBook = (index: number, updates: Partial<BookPreview>) => {
        const updated = [...bookPreviews];
        updated[index] = { ...updated[index], ...updates };
        setBookPreviews(updated);
    };

    const removeBook = (index: number) => {
        setBookPreviews(bookPreviews.filter((_, i) => i !== index));
    };

    const handleAddSelectedToLibrary = async () => {
        console.log('handleAddSelectedToLibrary called');
        console.log('selectedLibraryId:', selectedLibraryId);
        console.log('bookPreviews:', bookPreviews.length);
        
        if (!selectedLibraryId) {
            Alert.alert('Error', 'Please select a library first');
            return;
        }

        const selectedBooks = bookPreviews.filter(book => book.selected);
        console.log('selectedBooks:', selectedBooks.length);
        
        if (selectedBooks.length === 0) {
            Alert.alert('No Books Selected', 'Please select at least one book to add');
            return;
        }

        setSaving(true);
        try {
            console.log('Calling confirmBooksToLibrary...');
            const result = await confirmBooksToLibrary(selectedLibraryId, {
                books: selectedBooks,
            });
            console.log('Result:', result);

            const message = `Successfully added ${result.successCount} book(s) to your library.${result.failedCount > 0 ? `\n${result.failedCount} book(s) failed.` : ''}`;
            
            // Use window.alert for web compatibility, then reset form
            if (typeof window !== 'undefined' && window.alert) {
                window.alert(message);
                resetForm();
            } else {
                Alert.alert(
                    'Import Complete',
                    message,
                    [{ text: 'OK', onPress: () => resetForm() }]
                );
            }
        } catch (error) {
            console.error('Add to library error:', error);
            const errorMessage = 'Failed to add books to library. Please try again.';
            if (typeof window !== 'undefined' && window.alert) {
                window.alert(errorMessage);
            } else {
                Alert.alert('Error', errorMessage);
            }
        } finally {
            setSaving(false);
        }
    };

    const resetForm = () => {
        setImageUri(null);
        setBookPreviews([]);
        setEditingIndex(null);
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

    const getSourceLabel = (source: BookSource): string => {
        switch (source) {
            case BookSource.Local:
                return 'Local';
            case BookSource.OpenLibrary:
                return 'Open Library';
            case BookSource.GoogleBooks:
                return 'Google';
            case BookSource.OcrText:
                return 'OCR';
            default:
                return '?';
        }
    };

    const selectedCount = bookPreviews.filter(b => b.selected).length;
    const selectedLibrary = libraries.find(l => l.id === selectedLibraryId);

    const renderBookItem = ({ item, index }: { item: BookPreview; index: number }) => (
        <View style={[styles.bookItem, item.selected && styles.bookItemSelected]}>
            <TouchableOpacity
                style={styles.checkbox}
                onPress={() => toggleBookSelection(index)}
            >
                <View style={[styles.checkboxInner, item.selected && styles.checkboxChecked]}>
                    {item.selected && <Text style={styles.checkmark}>✓</Text>}
                </View>
            </TouchableOpacity>

            <View style={styles.bookInfo}>
                {item.coverImageUrl && (
                    <Image source={{ uri: item.coverImageUrl }} style={styles.thumbnail} />
                )}
                <View style={styles.bookDetails}>
                    <Text style={styles.bookTitle} numberOfLines={2}>{item.title}</Text>
                    {item.author && (
                        <Text style={styles.bookAuthor} numberOfLines={1}>{item.author}</Text>
                    )}
                    <View style={styles.bookMeta}>
                        {item.isbn && <Text style={styles.bookIsbn}>ISBN: {item.isbn}</Text>}
                        <View style={[styles.sourceTag, { backgroundColor: getSourceColor(item.source) }]}>
                            <Text style={styles.sourceTagText}>{getSourceLabel(item.source)}</Text>
                        </View>
                    </View>
                </View>
            </View>

            <View style={styles.bookActions}>
                <TouchableOpacity
                    style={styles.editButton}
                    onPress={() => setEditingIndex(index)}
                >
                    <Text style={styles.editButtonText}>Edit</Text>
                </TouchableOpacity>
                <TouchableOpacity
                    style={styles.removeButton}
                    onPress={() => removeBook(index)}
                >
                    <Text style={styles.removeButtonText}>✕</Text>
                </TouchableOpacity>
            </View>
        </View>
    );

    return (
        <View style={styles.container}>
            <Text style={styles.title}>Scan Bookshelf</Text>
            <Text style={styles.subtitle}>
                Take a photo of your bookshelf to add multiple books at once
            </Text>

            {/* Image Preview */}
            {imageUri && (
                <Image source={{ uri: imageUri }} style={styles.image} />
            )}

            {/* Action Buttons - Initial */}
            {bookPreviews.length === 0 && (
                <View style={styles.buttonContainer}>
                    <TouchableOpacity style={styles.button} onPress={pickImage}>
                        <Text style={styles.buttonText}>Choose from Gallery</Text>
                    </TouchableOpacity>
                    <TouchableOpacity style={styles.button} onPress={takePhoto}>
                        <Text style={styles.buttonText}>Take Photo</Text>
                    </TouchableOpacity>
                    {imageUri && (
                        <TouchableOpacity
                            style={[styles.button, styles.primaryButton]}
                            onPress={handleScan}
                            disabled={loading}
                        >
                            {loading ? (
                                <View style={styles.loadingContainer}>
                                    <ActivityIndicator color="#fff" />
                                    <Text style={styles.loadingText}>Scanning... This may take a moment</Text>
                                </View>
                            ) : (
                                <Text style={styles.primaryButtonText}>Scan Bookshelf</Text>
                            )}
                        </TouchableOpacity>
                    )}
                </View>
            )}

            {/* Results List */}
            {bookPreviews.length > 0 && (
                <View style={styles.resultsContainer}>
                    <View style={styles.resultsHeader}>
                        <Text style={styles.resultsTitle}>
                            Found {bookPreviews.length} Book{bookPreviews.length !== 1 ? 's' : ''}
                        </Text>
                        <View style={styles.selectionButtons}>
                            <TouchableOpacity onPress={selectAll}>
                                <Text style={styles.selectionButtonText}>Select All</Text>
                            </TouchableOpacity>
                            <Text style={styles.selectionDivider}>|</Text>
                            <TouchableOpacity onPress={deselectAll}>
                                <Text style={styles.selectionButtonText}>Deselect All</Text>
                            </TouchableOpacity>
                        </View>
                    </View>

                    <FlatList
                        data={bookPreviews}
                        renderItem={renderBookItem}
                        keyExtractor={(_, index) => index.toString()}
                        style={styles.bookList}
                        contentContainerStyle={styles.bookListContent}
                    />

                    {/* Library Selector */}
                    <View style={styles.librarySection}>
                        <Text style={styles.libraryLabel}>Add to Library:</Text>
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
                    <View style={styles.actionButtons}>
                        <TouchableOpacity style={styles.cancelButton} onPress={resetForm}>
                            <Text style={styles.cancelButtonText}>Cancel</Text>
                        </TouchableOpacity>
                        <TouchableOpacity
                            style={[
                                styles.button,
                                styles.primaryButton,
                                (selectedCount === 0 || !selectedLibraryId) && styles.disabledButton,
                            ]}
                            onPress={handleAddSelectedToLibrary}
                            disabled={saving || selectedCount === 0 || !selectedLibraryId}
                        >
                            {saving ? (
                                <ActivityIndicator color="#fff" />
                            ) : (
                                <Text style={styles.primaryButtonText}>
                                    Add {selectedCount} Book{selectedCount !== 1 ? 's' : ''} to Library
                                </Text>
                            )}
                        </TouchableOpacity>
                    </View>
                </View>
            )}

            {/* Edit Modal */}
            <Modal visible={editingIndex !== null} transparent animationType="slide">
                <View style={styles.modalOverlay}>
                    <View style={styles.editModalContent}>
                        <Text style={styles.modalTitle}>Edit Book Details</Text>
                        {editingIndex !== null && bookPreviews[editingIndex] && (
                            <ScrollView style={styles.editForm}>
                                <View style={styles.fieldContainer}>
                                    <Text style={styles.fieldLabel}>Title *</Text>
                                    <TextInput
                                        style={styles.input}
                                        value={bookPreviews[editingIndex].title}
                                        onChangeText={(text) => updateBook(editingIndex, { title: text })}
                                        placeholder="Book title"
                                    />
                                </View>
                                <View style={styles.fieldContainer}>
                                    <Text style={styles.fieldLabel}>Author</Text>
                                    <TextInput
                                        style={styles.input}
                                        value={bookPreviews[editingIndex].author || ''}
                                        onChangeText={(text) => updateBook(editingIndex, { author: text || undefined })}
                                        placeholder="Author name"
                                    />
                                </View>
                                <View style={styles.fieldContainer}>
                                    <Text style={styles.fieldLabel}>ISBN</Text>
                                    <TextInput
                                        style={styles.input}
                                        value={bookPreviews[editingIndex].isbn || ''}
                                        onChangeText={(text) => updateBook(editingIndex, { isbn: text || undefined })}
                                        placeholder="ISBN-13"
                                        keyboardType="numeric"
                                    />
                                </View>
                                <View style={styles.fieldContainer}>
                                    <Text style={styles.fieldLabel}>Publisher</Text>
                                    <TextInput
                                        style={styles.input}
                                        value={bookPreviews[editingIndex].publisher || ''}
                                        onChangeText={(text) => updateBook(editingIndex, { publisher: text || undefined })}
                                        placeholder="Publisher"
                                    />
                                </View>
                                <View style={styles.row}>
                                    <View style={[styles.fieldContainer, { flex: 1, marginRight: 8 }]}>
                                        <Text style={styles.fieldLabel}>Year</Text>
                                        <TextInput
                                            style={styles.input}
                                            value={bookPreviews[editingIndex].publishedYear?.toString() || ''}
                                            onChangeText={(text) =>
                                                updateBook(editingIndex, { publishedYear: text ? parseInt(text, 10) : undefined })
                                            }
                                            placeholder="Year"
                                            keyboardType="numeric"
                                        />
                                    </View>
                                    <View style={[styles.fieldContainer, { flex: 1 }]}>
                                        <Text style={styles.fieldLabel}>Genre</Text>
                                        <TextInput
                                            style={styles.input}
                                            value={bookPreviews[editingIndex].genre || ''}
                                            onChangeText={(text) => updateBook(editingIndex, { genre: text || undefined })}
                                            placeholder="Genre"
                                        />
                                    </View>
                                </View>
                            </ScrollView>
                        )}
                        <TouchableOpacity
                            style={[styles.button, styles.primaryButton]}
                            onPress={() => setEditingIndex(null)}
                        >
                            <Text style={styles.primaryButtonText}>Done</Text>
                        </TouchableOpacity>
                    </View>
                </View>
            </Modal>

            {/* Library Picker Modal */}
            <Modal visible={showLibraryPicker} transparent animationType="slide">
                <View style={styles.modalOverlay}>
                    <View style={styles.modalContent}>
                        <Text style={styles.modalTitle}>Select Library</Text>
                        <ScrollView style={styles.libraryList}>
                            {libraries.length === 0 ? (
                                <Text style={styles.noLibrariesText}>
                                    You don't have any libraries yet. Create one below!
                                </Text>
                            ) : (
                                libraries.map((library) => (
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
                                ))
                            )}
                        </ScrollView>
                        <TouchableOpacity
                            style={styles.createLibraryButton}
                            onPress={() => setShowCreateLibrary(true)}
                        >
                            <Text style={styles.createLibraryButtonText}>+ Create New Library</Text>
                        </TouchableOpacity>
                        <TouchableOpacity
                            style={styles.modalCloseButton}
                            onPress={() => setShowLibraryPicker(false)}
                        >
                            <Text style={styles.modalCloseText}>Close</Text>
                        </TouchableOpacity>
                    </View>
                </View>
            </Modal>

            {/* Create Library Modal */}
            <Modal visible={showCreateLibrary} transparent animationType="slide">
                <View style={styles.modalOverlay}>
                    <View style={styles.modalContent}>
                        <Text style={styles.modalTitle}>Create New Library</Text>
                        <View style={styles.fieldContainer}>
                            <Text style={styles.fieldLabel}>Library Name *</Text>
                            <TextInput
                                style={styles.input}
                                value={newLibraryName}
                                onChangeText={setNewLibraryName}
                                placeholder="My Library"
                                autoFocus
                            />
                        </View>
                        <TouchableOpacity
                            style={styles.publicToggle}
                            onPress={() => setNewLibraryIsPublic(!newLibraryIsPublic)}
                        >
                            <View style={[styles.checkboxInner, newLibraryIsPublic && styles.checkboxChecked]}>
                                {newLibraryIsPublic && <Text style={styles.checkmark}>✓</Text>}
                            </View>
                            <Text style={styles.publicToggleText}>Make this library public</Text>
                        </TouchableOpacity>
                        <View style={styles.modalButtons}>
                            <TouchableOpacity
                                style={styles.modalCloseButton}
                                onPress={() => {
                                    setShowCreateLibrary(false);
                                    setNewLibraryName('');
                                    setNewLibraryIsPublic(false);
                                }}
                            >
                                <Text style={styles.modalCloseText}>Cancel</Text>
                            </TouchableOpacity>
                            <TouchableOpacity
                                style={[styles.button, styles.primaryButton, creatingLibrary && styles.disabledButton]}
                                onPress={handleCreateLibrary}
                                disabled={creatingLibrary}
                            >
                                {creatingLibrary ? (
                                    <ActivityIndicator color="#fff" />
                                ) : (
                                    <Text style={styles.primaryButtonText}>Create</Text>
                                )}
                            </TouchableOpacity>
                        </View>
                    </View>
                </View>
            </Modal>
        </View>
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
        marginBottom: 16,
    },
    image: {
        width: '100%',
        height: 180,
        borderRadius: 8,
        marginBottom: 16,
        backgroundColor: '#e0e0e0',
    },
    buttonContainer: {
        gap: 10,
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
    loadingContainer: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: 10,
    },
    loadingText: {
        color: '#fff',
        fontSize: 14,
    },
    resultsContainer: {
        flex: 1,
    },
    resultsHeader: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: 12,
    },
    resultsTitle: {
        fontSize: 18,
        fontWeight: '600',
        color: '#333',
    },
    selectionButtons: {
        flexDirection: 'row',
        alignItems: 'center',
    },
    selectionButtonText: {
        color: '#4A90A4',
        fontSize: 14,
        fontWeight: '500',
    },
    selectionDivider: {
        color: '#ccc',
        marginHorizontal: 8,
    },
    bookList: {
        flex: 1,
    },
    bookListContent: {
        paddingBottom: 16,
    },
    bookItem: {
        flexDirection: 'row',
        alignItems: 'center',
        backgroundColor: '#fff',
        borderRadius: 8,
        padding: 12,
        marginBottom: 8,
        borderWidth: 1,
        borderColor: '#e0e0e0',
    },
    bookItemSelected: {
        borderColor: '#4A90A4',
        backgroundColor: '#F5FAFC',
    },
    checkbox: {
        marginRight: 12,
    },
    checkboxInner: {
        width: 24,
        height: 24,
        borderRadius: 4,
        borderWidth: 2,
        borderColor: '#ccc',
        alignItems: 'center',
        justifyContent: 'center',
    },
    checkboxChecked: {
        backgroundColor: '#4A90A4',
        borderColor: '#4A90A4',
    },
    checkmark: {
        color: '#fff',
        fontSize: 16,
        fontWeight: 'bold',
    },
    bookInfo: {
        flex: 1,
        flexDirection: 'row',
    },
    thumbnail: {
        width: 50,
        height: 70,
        borderRadius: 4,
        marginRight: 12,
    },
    bookDetails: {
        flex: 1,
    },
    bookTitle: {
        fontSize: 15,
        fontWeight: '600',
        color: '#333',
        marginBottom: 2,
    },
    bookAuthor: {
        fontSize: 13,
        color: '#666',
        marginBottom: 4,
    },
    bookMeta: {
        flexDirection: 'row',
        alignItems: 'center',
        flexWrap: 'wrap',
        gap: 6,
    },
    bookIsbn: {
        fontSize: 11,
        color: '#999',
    },
    sourceTag: {
        paddingHorizontal: 6,
        paddingVertical: 2,
        borderRadius: 4,
    },
    sourceTagText: {
        color: '#fff',
        fontSize: 10,
        fontWeight: '500',
    },
    bookActions: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: 8,
    },
    editButton: {
        paddingHorizontal: 10,
        paddingVertical: 6,
        backgroundColor: '#f0f0f0',
        borderRadius: 4,
    },
    editButtonText: {
        fontSize: 12,
        color: '#666',
    },
    removeButton: {
        paddingHorizontal: 8,
        paddingVertical: 6,
    },
    removeButtonText: {
        fontSize: 16,
        color: '#999',
    },
    librarySection: {
        marginVertical: 12,
    },
    libraryLabel: {
        fontSize: 14,
        color: '#666',
        marginBottom: 6,
        fontWeight: '500',
    },
    librarySelector: {
        backgroundColor: '#fff',
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
    actionButtons: {
        flexDirection: 'row',
        gap: 12,
    },
    cancelButton: {
        flex: 0.4,
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
        maxHeight: '50%',
    },
    editModalContent: {
        backgroundColor: '#fff',
        borderTopLeftRadius: 20,
        borderTopRightRadius: 20,
        padding: 20,
        maxHeight: '80%',
    },
    modalTitle: {
        fontSize: 18,
        fontWeight: '600',
        color: '#333',
        marginBottom: 16,
        textAlign: 'center',
    },
    editForm: {
        marginBottom: 16,
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
    noLibrariesText: {
        fontSize: 14,
        color: '#666',
        textAlign: 'center',
        paddingVertical: 20,
    },
    createLibraryButton: {
        backgroundColor: '#E3F2FD',
        paddingVertical: 14,
        borderRadius: 8,
        alignItems: 'center',
        marginBottom: 12,
        borderWidth: 1,
        borderColor: '#4A90A4',
        borderStyle: 'dashed',
    },
    createLibraryButtonText: {
        fontSize: 16,
        color: '#4A90A4',
        fontWeight: '600',
    },
    publicToggle: {
        flexDirection: 'row',
        alignItems: 'center',
        marginBottom: 16,
    },
    publicToggleText: {
        marginLeft: 12,
        fontSize: 16,
        color: '#333',
    },
    modalButtons: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        gap: 12,
    },
    modalCloseButton: {
        flex: 1,
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
