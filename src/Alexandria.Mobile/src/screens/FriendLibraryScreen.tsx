import React, { useState, useEffect, useCallback } from 'react';
import {
    View,
    Text,
    StyleSheet,
    FlatList,
    TouchableOpacity,
    Alert,
    ActivityIndicator,
    Image,
} from 'react-native';
import { getFriendLibraries, getLibraryBooks, requestLoan } from '../services/api';
import { Library, LibraryBook, BookStatus } from '../types';

export default function FriendLibraryScreen({ route, navigation }: any) {
    const { friendId, friendName } = route.params;

    const [libraries, setLibraries] = useState<Library[]>([]);
    const [selectedLibrary, setSelectedLibrary] = useState<Library | null>(null);
    const [books, setBooks] = useState<LibraryBook[]>([]);
    const [loading, setLoading] = useState(true);
    const [loadingBooks, setLoadingBooks] = useState(false);
    const [requestingId, setRequestingId] = useState<number | null>(null);

    const showAlert = (title: string, message: string) => {
        if (typeof window !== 'undefined' && window.alert) {
            window.alert(`${title}: ${message}`);
        } else {
            Alert.alert(title, message);
        }
    };

    const loadLibraries = useCallback(async () => {
        try {
            const data = await getFriendLibraries(friendId);
            setLibraries(data);
        } catch (error) {
            console.error('Failed to load friend libraries:', error);
            showAlert('Error', "Failed to load your friend's libraries");
        } finally {
            setLoading(false);
        }
    }, [friendId]);

    useEffect(() => {
        navigation.setOptions({ title: `${friendName}'s Libraries` });
        loadLibraries();
    }, [loadLibraries, friendName, navigation]);

    const openLibrary = async (library: Library) => {
        setSelectedLibrary(library);
        setLoadingBooks(true);
        try {
            const data = await getLibraryBooks(library.id);
            setBooks(data);
        } catch (error) {
            console.error('Failed to load library books:', error);
            showAlert('Error', 'Failed to load books in this library');
            setSelectedLibrary(null);
        } finally {
            setLoadingBooks(false);
        }
    };

    const handleRequestBorrow = async (book: LibraryBook) => {
        setRequestingId(book.id);
        try {
            await requestLoan(book.id);
            showAlert(
                'Request Sent',
                `Your request to borrow "${book.book.title}" was sent to ${friendName}.`
            );
        } catch (error: any) {
            console.error('Request borrow error:', error);
            const message =
                typeof error.response?.data === 'string'
                    ? error.response.data
                    : 'Failed to send borrow request';
            showAlert('Error', message);
        } finally {
            setRequestingId(null);
        }
    };

    const renderLibrary = ({ item }: { item: Library }) => (
        <TouchableOpacity style={styles.card} onPress={() => openLibrary(item)}>
            <Text style={styles.cardTitle}>{item.name}</Text>
            <Text style={styles.cardSubtitle}>
                {item.isPublic ? 'Public library' : 'Shared with friends'}
            </Text>
        </TouchableOpacity>
    );

    const renderBook = ({ item }: { item: LibraryBook }) => {
        const available = item.status === BookStatus.Available;
        return (
            <View style={styles.card}>
                <View style={styles.bookRow}>
                    {item.book.coverImageUrl && (
                        <Image
                            source={{ uri: item.book.coverImageUrl }}
                            style={styles.bookCover}
                        />
                    )}
                    <View style={styles.bookInfo}>
                        <Text style={styles.cardTitle}>{item.book.title}</Text>
                        {item.book.author && (
                            <Text style={styles.cardSubtitle}>{item.book.author}</Text>
                        )}
                        <View
                            style={[
                                styles.statusBadge,
                                { backgroundColor: available ? '#4CAF50' : '#FF9800' },
                            ]}
                        >
                            <Text style={styles.statusText}>
                                {available ? 'Available' : 'Checked Out'}
                            </Text>
                        </View>
                    </View>
                </View>
                {available && (
                    <TouchableOpacity
                        style={[
                            styles.requestButton,
                            requestingId === item.id && styles.disabledButton,
                        ]}
                        onPress={() => handleRequestBorrow(item)}
                        disabled={requestingId !== null}
                    >
                        {requestingId === item.id ? (
                            <ActivityIndicator color="#1A1A1A" size="small" />
                        ) : (
                            <Text style={styles.requestButtonText}>Request to Borrow</Text>
                        )}
                    </TouchableOpacity>
                )}
            </View>
        );
    };

    if (loading) {
        return (
            <View style={styles.loadingContainer}>
                <ActivityIndicator size="large" color="#E5A823" />
            </View>
        );
    }

    if (selectedLibrary) {
        return (
            <View style={styles.container}>
                <TouchableOpacity
                    style={styles.backRow}
                    onPress={() => {
                        setSelectedLibrary(null);
                        setBooks([]);
                    }}
                >
                    <Text style={styles.backText}>‹ All libraries</Text>
                </TouchableOpacity>
                <Text style={styles.header}>{selectedLibrary.name}</Text>
                {loadingBooks ? (
                    <ActivityIndicator
                        size="large"
                        color="#E5A823"
                        style={styles.booksSpinner}
                    />
                ) : (
                    <FlatList
                        data={books}
                        renderItem={renderBook}
                        keyExtractor={(item) => item.id.toString()}
                        contentContainerStyle={styles.listContent}
                        ListEmptyComponent={
                            <Text style={styles.emptyText}>No books in this library</Text>
                        }
                    />
                )}
            </View>
        );
    }

    return (
        <View style={styles.container}>
            <FlatList
                data={libraries}
                renderItem={renderLibrary}
                keyExtractor={(item) => item.id.toString()}
                contentContainerStyle={styles.listContent}
                ListEmptyComponent={
                    <Text style={styles.emptyText}>
                        {friendName} has no libraries to show
                    </Text>
                }
            />
        </View>
    );
}

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: '#121212',
    },
    loadingContainer: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: '#121212',
    },
    listContent: {
        padding: 15,
    },
    card: {
        backgroundColor: '#1E1E1E',
        padding: 15,
        borderRadius: 8,
        marginBottom: 10,
    },
    cardTitle: {
        fontSize: 18,
        fontWeight: 'bold',
        color: '#FFFFFF',
        marginBottom: 4,
    },
    cardSubtitle: {
        fontSize: 14,
        color: '#B0B0B0',
        marginBottom: 4,
    },
    bookRow: {
        flexDirection: 'row',
    },
    bookCover: {
        width: 50,
        height: 75,
        borderRadius: 4,
        marginRight: 12,
    },
    bookInfo: {
        flex: 1,
    },
    statusBadge: {
        alignSelf: 'flex-start',
        paddingVertical: 3,
        paddingHorizontal: 10,
        borderRadius: 12,
        marginTop: 4,
    },
    statusText: {
        color: 'white',
        fontSize: 12,
        fontWeight: '500',
    },
    requestButton: {
        backgroundColor: '#E5A823',
        borderRadius: 6,
        paddingVertical: 10,
        alignItems: 'center',
        marginTop: 12,
    },
    requestButtonText: {
        color: '#1A1A1A',
        fontSize: 14,
        fontWeight: '600',
    },
    disabledButton: {
        opacity: 0.6,
    },
    backRow: {
        paddingHorizontal: 15,
        paddingTop: 12,
    },
    backText: {
        color: '#E5A823',
        fontSize: 16,
        fontWeight: '500',
    },
    header: {
        fontSize: 20,
        fontWeight: 'bold',
        color: '#FFFFFF',
        paddingHorizontal: 15,
        paddingTop: 8,
    },
    booksSpinner: {
        marginTop: 30,
    },
    emptyText: {
        color: '#888',
        textAlign: 'center',
        marginTop: 40,
        fontSize: 16,
    },
});
