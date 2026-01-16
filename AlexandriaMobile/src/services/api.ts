import axios from 'axios';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { Book, Library, LibraryBook, Loan, Friend } from '../types';
import { config } from '../config';

const API_BASE_URL = config.api.baseUrl;

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add token to requests
api.interceptors.request.use(
  async (config) => {
    const token = await AsyncStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Books API
export const searchBooks = async (query: {
  query?: string;
  author?: string;
  genre?: string;
  isbn?: string;
  publishedYear?: number;
}): Promise<Book[]> => {
  const response = await api.get('/books', { params: query });
  return response.data;
};

export const getBook = async (id: number): Promise<Book> => {
  const response = await api.get(`/books/${id}`);
  return response.data;
};

export const createBook = async (book: Partial<Book>): Promise<Book> => {
  const response = await api.post('/books', book);
  return response.data;
};

export const searchBooksByImage = async (imageUri: string): Promise<Book[]> => {
  const formData = new FormData();
  formData.append('image', {
    uri: imageUri,
    type: 'image/jpeg',
    name: 'book-image.jpg',
  } as any);

  const response = await api.post('/books/search-by-image', formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
  return response.data;
};

// Libraries API
export const getLibraries = async (isPublic?: boolean): Promise<Library[]> => {
  const response = await api.get('/libraries', { 
    params: isPublic !== undefined ? { isPublic } : {} 
  });
  return response.data;
};

export const getLibrary = async (id: number): Promise<Library> => {
  const response = await api.get(`/libraries/${id}`);
  return response.data;
};

export const createLibrary = async (library: { name: string; isPublic: boolean }): Promise<Library> => {
  const response = await api.post('/libraries', library);
  return response.data;
};

export const getLibraryBooks = async (libraryId: number): Promise<LibraryBook[]> => {
  const response = await api.get(`/libraries/${libraryId}/books`);
  return response.data;
};

export const addBookToLibrary = async (
  libraryId: number,
  bookId: number,
  status: number = 0
): Promise<LibraryBook> => {
  const response = await api.post(`/libraries/${libraryId}/books`, { bookId, status });
  return response.data;
};

export const removeBookFromLibrary = async (
  libraryId: number,
  libraryBookId: number
): Promise<void> => {
  await api.delete(`/libraries/${libraryId}/books/${libraryBookId}`);
};

// Loans API
export const getLoans = async (filter?: 'borrowed' | 'lent'): Promise<Loan[]> => {
  const response = await api.get('/loans', { params: filter ? { filter } : {} });
  return response.data;
};

export const getLoan = async (id: number): Promise<Loan> => {
  const response = await api.get(`/loans/${id}`);
  return response.data;
};

export const createLoan = async (loan: {
  libraryBookId: number;
  borrowerId: number;
  dueDate?: string;
}): Promise<Loan> => {
  const response = await api.post('/loans', loan);
  return response.data;
};

export const updateLoanStatus = async (id: number, status: number): Promise<Loan> => {
  const response = await api.patch(`/loans/${id}/status`, { status });
  return response.data;
};

// Friends API
export const getFriends = async (): Promise<Friend[]> => {
  const response = await api.get('/friends');
  return response.data;
};

export const sendFriendRequest = async (friendId: number): Promise<void> => {
  await api.post(`/friends/${friendId}`);
};

export const acceptFriendRequest = async (friendshipId: number): Promise<void> => {
  await api.put(`/friends/${friendshipId}/accept`);
};

export const removeFriend = async (friendshipId: number): Promise<void> => {
  await api.delete(`/friends/${friendshipId}`);
};

// Authentication
export const setAuthToken = async (token: string): Promise<void> => {
  await AsyncStorage.setItem('authToken', token);
};

export const getAuthToken = async (): Promise<string | null> => {
  return await AsyncStorage.getItem('authToken');
};

export const removeAuthToken = async (): Promise<void> => {
  await AsyncStorage.removeItem('authToken');
};
