import type { ApiClient } from './client';
import type {
  AuthResponse,
  Book,
  CreateBookRequest,
  FriendRequests,
  Friendship,
  Library,
  LibraryBook,
  LibraryBookStatus,
  Loan,
  LoanStatus,
  PhotoUpload,
  ScanMode,
  ScanResult,
  UpdateLibraryBookRequest,
  UpdateLibraryRequest,
  User,
  UserNameAvailability,
  UserSearchResult,
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
    scanPhoto: (photo: PhotoUpload, mode: ScanMode) => client.postForm<ScanResult>(`/api/books/scan?mode=${mode}`, photoForm(photo)),

    updateUserName: (userName: string) => client.put<User>('/api/users/me', { userName }),
    checkUserName: (userName: string) =>
      client.get<UserNameAvailability>(`/api/users/check-username?userName=${encodeURIComponent(userName)}`),

    /** Your friends; with `search`, only those whose username or real name contains it. */
    getFriends: (search?: string) =>
      client.get<Friendship[]>(search ? `/api/friends?search=${encodeURIComponent(search)}` : '/api/friends'),
    getFriendRequests: () => client.get<FriendRequests>('/api/friends/requests'),
    searchUsers: (userName: string) => client.get<UserSearchResult[]>(`/api/friends/search?userName=${encodeURIComponent(userName)}`),
    sendFriendRequest: (userName: string) => client.post<Friendship>('/api/friends/requests', { userName }),
    acceptFriendRequest: (friendshipId: string) => client.post<Friendship>(`/api/friends/requests/${id(friendshipId)}/accept`),
    declineFriendRequest: (friendshipId: string) => client.post<Friendship>(`/api/friends/requests/${id(friendshipId)}/decline`),
    removeFriendship: (friendshipId: string) => client.delete(`/api/friends/${id(friendshipId)}`),
    getFriendLibraries: (friendId: string) => client.get<Library[]>(`/api/friends/${id(friendId)}/libraries`),

    getLoans: (role: 'Any' | 'Lender' | 'Borrower' = 'Any', status?: LoanStatus) =>
      client.get<Loan[]>(`/api/loans?role=${role}${status ? `&status=${status}` : ''}`),
    lendBook: (libraryBookId: string, borrowerId: string, dueDate: string | null) =>
      client.post<Loan>('/api/loans', { libraryBookId, borrowerId, dueDate }),
    requestToBorrow: (libraryBookId: string) => client.post<Loan>('/api/loans/requests', { libraryBookId }),
    approveLoan: (loanId: string) => client.post<Loan>(`/api/loans/${id(loanId)}/approve`),
    declineLoan: (loanId: string) => client.post<Loan>(`/api/loans/${id(loanId)}/decline`),
    cancelLoan: (loanId: string) => client.post<Loan>(`/api/loans/${id(loanId)}/cancel`),
    returnLoan: (loanId: string) => client.post<Loan>(`/api/loans/${id(loanId)}/return`),
  };
}

/** Web gives us a File; native gives a local URI that React Native's FormData uploads from disk. */
function photoForm(photo: PhotoUpload): FormData {
  const form = new FormData();
  if (photo.file) {
    form.append('image', photo.file, photo.file.name || 'photo.jpg');
  } else {
    const nativeFile = { uri: photo.uri, name: photo.fileName ?? 'photo.jpg', type: photo.mimeType ?? 'image/jpeg' };
    form.append('image', nativeFile as unknown as Blob);
  }
  return form;
}

export type AlexandriaApi = ReturnType<typeof createAlexandriaApi>;
