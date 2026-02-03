export interface User {
  id: number;
  email: string;
  name: string;
  profilePictureUrl?: string;
}

export interface Book {
  id: number;
  title: string;
  author?: string;
  isbn?: string;
  publisher?: string;
  publishedYear?: number;
  description?: string;
  coverImageUrl?: string;
  genre?: string;
  pageCount?: number;
}

export interface Library {
  id: number;
  name: string;
  isPublic: boolean;
  userId: number;
  createdAt: string;
}

export interface LibraryBook {
  id: number;
  libraryId: number;
  book: Book;
  status: BookStatus;
  loanNote?: string;
  addedAt: string;
}

export enum BookStatus {
  Available = 0,
  CheckedOut = 1,
  WaitingToBeLoanedOut = 2,
}

export enum LoanStatus {
  Pending = 0,
  Active = 1,
  Returned = 2,
  Overdue = 3,
  Cancelled = 4,
}

export interface Loan {
  id: number;
  libraryBookId: number;
  lenderId: number;
  lenderName: string;
  borrowerId: number;
  borrowerName: string;
  loanDate: string;
  dueDate?: string;
  returnedDate?: string;
  status: LoanStatus;
  book: Book;
}

export interface Friend {
  id: number;
  friend: User;
  createdAt: string;
}

export interface FriendRequest {
  id: number;
  fromUser: User;
  createdAt: string;
}

// Book scanning types
export enum BookSource {
  Local = 0,
  OpenLibrary = 1,
  GoogleBooks = 2,
  OcrText = 3,
}

export interface BookPreview {
  existingBookId?: number;
  title: string;
  author?: string;
  isbn?: string;
  publisher?: string;
  publishedYear?: number;
  description?: string;
  coverImageUrl?: string;
  genre?: string;
  pageCount?: number;
  source: BookSource;
  confidence: number;
  externalId?: string;
  // UI state for bulk edit
  selected?: boolean;
}

export interface ConfirmBooksRequest {
  books: BookPreview[];
}

export interface ConfirmedBookResult {
  bookId: number;
  libraryBookId?: number;
  title: string;
  wasCreated: boolean;
  addedToLibrary: boolean;
  error?: string;
}

export interface ConfirmBooksResult {
  results: ConfirmedBookResult[];
  successCount: number;
  failedCount: number;
}
