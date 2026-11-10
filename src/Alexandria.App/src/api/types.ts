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

/** Another user's public profile (no email). */
export type UserSummary = {
  id: string;
  userName: string;
  displayName: string;
  profilePictureUrl: string | null;
};

export type FriendshipStatus = 'Pending' | 'Accepted' | 'Rejected' | 'Blocked';

export type Friendship = {
  friendshipId: string;
  user: UserSummary;
  status: FriendshipStatus;
  /** True when the other user sent the request. */
  isIncoming: boolean;
  createdAt: string;
  updatedAt: string;
};

export type FriendRequests = { incoming: Friendship[]; outgoing: Friendship[] };

export type UserRelationship = 'None' | 'Friends' | 'RequestSent' | 'RequestReceived';

export type UserSearchResult = { user: UserSummary; relationship: UserRelationship; friendshipId: string | null };

export type UserNameAvailability = { userName: string; isValid: boolean; isAvailable: boolean };

export type LoanStatus = 'Pending' | 'Active' | 'Returned' | 'Overdue' | 'Cancelled';

export type Loan = {
  id: string;
  libraryBookId: string;
  libraryId: string;
  libraryName: string;
  book: Book;
  lender: UserSummary;
  borrower: UserSummary;
  status: LoanStatus;
  isOverdue: boolean;
  requestedAt: string;
  loanDate: string | null;
  dueDate: string | null;
  returnedDate: string | null;
};

export type ScanMode = 'Single' | 'Shelf';

export type ScannedBook = {
  detectedTitle: string;
  detectedAuthor: string | null;
  detectedIsbn: string | null;
  /** Best catalog match; its id is null when it isn't in Alexandria yet. */
  match: Book | null;
};

export type ScanResult = { books: ScannedBook[]; message: string | null };

/** A photo to upload: a browser File on web, or a local file URI on native. */
export type PhotoUpload = { uri: string; file?: File; mimeType?: string; fileName?: string | null };

export type UpdateLibraryBookRequest = {
  status?: LibraryBookStatus;
  loanNote?: string;
  genre?: string;
};

/** The VAPID public key (base64url) browsers need to subscribe to push. */
export type WebPushKey = { publicKey: string };

/** A browser push subscription, as PushSubscription.toJSON() gives it. */
export type PushSubscriptionRequest = { endpoint: string; p256dh: string; auth: string };
