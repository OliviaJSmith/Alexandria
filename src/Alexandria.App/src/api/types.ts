/** Mirrors the API's DTOs (src/Alexandria.Api/Dtos). Enums are serialized as strings. */

export type User = {
  id: string;
  email: string;
  displayName: string;
  userName: string;
  profilePictureUrl: string | null;
  termsAcceptedAt: string | null;
};

export type AuthResponse = {
  token: string;
  expiresAt: string;
  user: User;
};

export type Library = {
  id: string;
  name: string;
  isPublic: boolean;
  ownerId: string;
  createdAt: string;
  updatedAt: string;
};

export type LibraryBookStatus = 'Available' | 'CheckedOut' | 'WaitingToBeLoanedOut';

export const libraryBookStatuses: LibraryBookStatus[] = ['Available', 'CheckedOut', 'WaitingToBeLoanedOut'];

export const statusLabels: Record<LibraryBookStatus, string> = {
  Available: 'On the shelf',
  CheckedOut: 'Lent out',
  WaitingToBeLoanedOut: 'Waiting to lend',
};

/** A catalog book. `id` is null for Open Library / Google Books results not yet in Alexandria. */
export type Book = {
  id: string | null;
  title: string;
  author: string | null;
  isbn: string | null;
  publisher: string | null;
  publishedYear: number | null;
  description: string | null;
  coverImageUrl: string | null;
  genre: string | null;
  pageCount: number | null;
};

export type LibraryBook = {
  id: string;
  libraryId: string;
  status: LibraryBookStatus;
  loanNote: string | null;
  addedAt: string;
  book: Book;
};

export type CreateBookRequest = Omit<Book, 'id'>;

export type UpdateLibraryRequest = {
  name?: string;
  isPublic?: boolean;
};

export type UpdateLibraryBookRequest = {
  status?: LibraryBookStatus;
  loanNote?: string;
  genre?: string;
};
