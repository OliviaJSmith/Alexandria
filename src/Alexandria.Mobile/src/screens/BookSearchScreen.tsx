import React, { useState, useEffect } from 'react';
import { View, Text, FlatList, StyleSheet, TouchableOpacity, TextInput, Button } from 'react-native';
import { searchBooks } from '../services/api';
import { Book } from '../types';

export default function BookSearchScreen({ navigation }: any) {
  const [searchQuery, setSearchQuery] = useState('');
  const [books, setBooks] = useState<Book[]>([]);
  const [loading, setLoading] = useState(false);

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

  const renderBook = ({ item }: { item: Book }) => (
    <TouchableOpacity
      style={styles.bookCard}
      onPress={() => navigation.navigate('BookDetail', { bookId: item.id })}
    >
      <View style={styles.bookInfo}>
        <Text style={styles.bookTitle}>{item.title}</Text>
        {item.author && <Text style={styles.bookAuthor}>{item.author}</Text>}
        {item.publishedYear && <Text style={styles.bookYear}>{item.publishedYear}</Text>}
      </View>
    </TouchableOpacity>
  );

  return (
    <View style={styles.container}>
      <View style={styles.searchBox}>
        <TextInput
          style={styles.searchInput}
          placeholder="Search for books..."
          value={searchQuery}
          onChangeText={setSearchQuery}
          onSubmitEditing={handleSearch}
        />
        <Button title="Search" onPress={handleSearch} />
      </View>
      
      <Button
        title="Search by Image"
        onPress={() => navigation.navigate('ImageSearch')}
      />
      
      <FlatList
        data={books}
        renderItem={renderBook}
        keyExtractor={(item) => item.id.toString()}
        style={styles.list}
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
  searchBox: {
    flexDirection: 'row',
    padding: 15,
    backgroundColor: 'white',
    alignItems: 'center',
    gap: 10,
  },
  searchInput: {
    flex: 1,
    padding: 10,
    backgroundColor: '#f0f0f0',
    borderRadius: 8,
    fontSize: 16,
  },
  list: {
    flex: 1,
  },
  listContent: {
    padding: 15,
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
  bookInfo: {
    flex: 1,
  },
  bookTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 5,
  },
  bookAuthor: {
    fontSize: 14,
    color: '#666',
    marginBottom: 3,
  },
  bookYear: {
    fontSize: 12,
    color: '#999',
  },
});
