import type { ApiClient } from './client';
import type {
  AuthResponse,
  Book,
  CreateBookRequest,
  Library,
  LibraryBook,
  LibraryBookStatus,
  UpdateLibraryBookRequest,
  UpdateLibraryRequest,
  User,
} from './types';

const id = (value: string) => encodeURIComponent(value);

/** Typed wrappers for every API endpoint the app uses. */
export function createAlexandriaApi(client: ApiClient) {
  return {
    signInWithGoogle: (accessToken: string) =>
      client.post<AuthResponse>('/api/auth/google', { accessToken, acceptedTerms: true }),

    getCurrentUser: () => client.get<User>('/api/users/me'),

    getMyLibraries: () => client.get<Library[]>('/api/libraries'),
    getLentOutBooks: () => client.get<LibraryBook[]>('/api/libraries/lent-out'),
    getLibrary: (libraryId: string) => client.get<Library>(`/api/libraries/${id(libraryId)}`),
    createLibrary: (name: string, isPublic: boolean) => client.post<Library>('/api/libraries', { name, isPublic }),
    updateLibrary: (libraryId: string, changes: UpdateLibraryRequest) =>
      client.patch<Library>(`/api/libraries/${id(libraryId)}`, changes),

    getLibraryBooks: (libraryId: string) => client.get<LibraryBook[]>(`/api/libraries/${id(libraryId)}/books`),
    addBookToLibrary: (libraryId: string, bookId: string, forceAdd = false, status: LibraryBookStatus = 'Available') =>
      client.post<LibraryBook>(`/api/libraries/${id(libraryId)}/books`, { bookId, forceAdd, status }),
    updateLibraryBook: (libraryId: string, libraryBookId: string, changes: UpdateLibraryBookRequest) =>
      client.patch<LibraryBook>(`/api/libraries/${id(libraryId)}/books/${id(libraryBookId)}`, changes),
    removeLibraryBook: (libraryId: string, libraryBookId: string) =>
      client.delete(`/api/libraries/${id(libraryId)}/books/${id(libraryBookId)}`),
    moveLibraryBook: (libraryId: string, libraryBookId: string, targetLibraryId: string) =>
      client.post<LibraryBook>(`/api/libraries/${id(libraryId)}/books/${id(libraryBookId)}/move`, { targetLibraryId }),

    searchBooks: (query: string) => client.get<Book[]>(`/api/books?query=${encodeURIComponent(query)}`),
    lookupIsbn: (isbn: string) => client.get<Book>(`/api/books/lookup/${encodeURIComponent(isbn)}`),
    createBook: (book: CreateBookRequest) => client.post<Book>('/api/books', book),
  };
}

export type AlexandriaApi = ReturnType<typeof createAlexandriaApi>;
