import axios from "axios";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { Platform } from "react-native";
import {
  Book,
  Library,
  LibraryBook,
  Loan,
  Friend,
  FriendRequest,
  User,
  BookPreview,
  ConfirmBooksRequest,
  ConfirmBooksResult,
} from "../types";
import { config } from "../config";

/** Represents a file object for React Native FormData uploads */
interface FormDataFile {
  uri: string;
  type: string;
  name: string;
}

const api = axios.create({
  baseURL: config.api.baseUrl,
  headers: {
    "Content-Type": "application/json",
  },
});

// Add token to requests
api.interceptors.request.use(
  async (config) => {
    const token = await AsyncStorage.getItem("authToken");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error),
);

// Books API
export const searchBooks = async (query: {
  query?: string;
  author?: string;
  genre?: string;
  isbn?: string;
  publishedYear?: number;
}): Promise<Book[]> => {
  const response = await api.get("/books", { params: query });
  return response.data;
};

export const getBook = async (id: number): Promise<Book> => {
  const response = await api.get(`/books/${id}`);
  return response.data;
};

export const createBook = async (book: Partial<Book>): Promise<Book> => {
  const response = await api.post("/books", book);
  return response.data;
};

export const searchBooksByImage = async (imageUri: string): Promise<Book[]> => {
  const formData = new FormData();
  formData.append("image", {
    uri: imageUri,
    type: "image/jpeg",
    name: "book-image.jpg",
  } as unknown as Blob);

  const response = await api.post("/books/search-by-image", formData, {
    headers: {
      "Content-Type": "multipart/form-data",
    },
  });
  return response.data;
};

// Libraries API
export const getLibraries = async (isPublic?: boolean): Promise<Library[]> => {
  const response = await api.get("/libraries", {
    params: isPublic !== undefined ? { isPublic } : {},
  });
  return response.data;
};

export const getLibrary = async (id: number): Promise<Library> => {
  const response = await api.get(`/libraries/${id}`);
  return response.data;
};

export const createLibrary = async (library: {
  name: string;
  isPublic: boolean;
}): Promise<Library> => {
  const response = await api.post("/libraries", library);
  return response.data;
};

export const getLibraryBooks = async (
  libraryId: number,
): Promise<LibraryBook[]> => {
  const response = await api.get(`/libraries/${libraryId}/books`);
  return response.data;
};

export const getLentOutBooks = async (): Promise<LibraryBook[]> => {
  const response = await api.get("/libraries/lent-out");
  return response.data;
};

export const addBookToLibrary = async (
  libraryId: number,
  bookId: number,
  status: number = 0,
  forceAdd: boolean = false,
): Promise<LibraryBook> => {
  const response = await api.post(`/libraries/${libraryId}/books`, {
    bookId,
    status,
    forceAdd,
  });
  return response.data;
};

export const removeBookFromLibrary = async (
  libraryId: number,
  libraryBookId: number,
): Promise<void> => {
  await api.delete(`/libraries/${libraryId}/books/${libraryBookId}`);
};

export const updateLibraryBook = async (
  libraryId: number,
  libraryBookId: number,
  updates: {
    status?: number;
    genre?: string;
    loanNote?: string;
  },
): Promise<LibraryBook> => {
  const response = await api.patch(
    `/libraries/${libraryId}/books/${libraryBookId}`,
    updates,
  );
  return response.data;
};

export const moveBookToLibrary = async (
  sourceLibraryId: number,
  libraryBookId: number,
  targetLibraryId: number,
): Promise<LibraryBook> => {
  const response = await api.post(
    `/libraries/${sourceLibraryId}/books/${libraryBookId}/move`,
    {
      targetLibraryId,
    },
  );
  return response.data;
};

// Loans API
export const getLoans = async (
  filter?: "borrowed" | "lent",
): Promise<Loan[]> => {
  const response = await api.get("/loans", {
    params: filter ? { filter } : {},
  });
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
  const response = await api.post("/loans", loan);
  return response.data;
};

export const updateLoanStatus = async (
  id: number,
  status: number,
): Promise<Loan> => {
  const response = await api.patch(`/loans/${id}/status`, { status });
  return response.data;
};

// Friends API
export const getFriends = async (): Promise<Friend[]> => {
  const response = await api.get("/friends");
  return response.data;
};

export const getPendingFriendRequests = async (): Promise<FriendRequest[]> => {
  const response = await api.get("/friends/requests");
  return response.data;
};

export const searchUserByEmail = async (
  email: string,
): Promise<User | null> => {
  try {
    const response = await api.get("/friends/search", { params: { email } });
    return response.data;
  } catch (error: any) {
    if (error.response?.status === 404) {
      return null;
    }
    throw error;
  }
};

export const sendFriendRequest = async (friendId: number): Promise<void> => {
  await api.post(`/friends/${friendId}`);
};

export const acceptFriendRequest = async (
  friendshipId: number,
): Promise<void> => {
  await api.put(`/friends/${friendshipId}/accept`);
};

export const removeFriend = async (friendshipId: number): Promise<void> => {
  await api.delete(`/friends/${friendshipId}`);
};

// Book Scanning API
const createImageFormData = async (
  imageUri: string,
  fieldName: string,
  fileName: string,
): Promise<FormData> => {
  const formData = new FormData();

  if (Platform.OS === "web") {
    // On web, fetch the blob from the URI (which is a blob URL or data URL)
    const response = await fetch(imageUri);
    const blob = await response.blob();
    formData.append(fieldName, blob, fileName);
  } else {
    // On native, use the React Native style object
    formData.append(fieldName, {
      uri: imageUri,
      type: "image/jpeg",
      name: fileName,
    } as unknown as Blob);
  }

  return formData;
};

export const scanSingleBook = async (
  imageUri: string,
): Promise<BookPreview> => {
  const formData = await createImageFormData(
    imageUri,
    "image",
    "book-image.jpg",
  );

  const response = await api.post("/books/scan-single", formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return response.data;
};

export const scanBookshelf = async (
  imageUri: string,
): Promise<BookPreview[]> => {
  const formData = await createImageFormData(
    imageUri,
    "image",
    "bookshelf-image.jpg",
  );

  const response = await api.post("/books/scan-bookshelf", formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return response.data;
};

export const lookupBookByIsbn = async (isbn: string): Promise<BookPreview> => {
  const response = await api.get(`/books/lookup/${encodeURIComponent(isbn)}`);
  return response.data;
};

export const confirmBooksToLibrary = async (
  libraryId: number,
  request: ConfirmBooksRequest,
): Promise<ConfirmBooksResult> => {
  console.log("confirmBooksToLibrary called with libraryId:", libraryId);
  console.log("Request payload:", JSON.stringify(request, null, 2));
  try {
    const response = await api.post(
      `/libraries/${libraryId}/confirm-books`,
      request,
    );
    console.log("confirmBooksToLibrary response:", response.data);
    return response.data;
  } catch (error: any) {
    console.error("confirmBooksToLibrary error:", error);
    console.error("Error response:", error.response?.data);
    throw error;
  }
};

// Authentication
export interface AuthResponse {
  token: string;
  user: {
    id: number;
    email: string;
    name: string;
    profilePictureUrl?: string;
  };
}

export const loginWithGoogle = async (
  googleAccessToken: string,
): Promise<AuthResponse> => {
  console.log("loginWithGoogle: Making request to /auth/google");
  console.log("loginWithGoogle: Token length:", googleAccessToken?.length);
  try {
    const response = await api.post("/auth/google", {
      accessToken: googleAccessToken,
    });
    console.log("loginWithGoogle: Response received", response.status);
    // Store the JWT token
    await AsyncStorage.setItem("authToken", response.data.token);
    console.log("loginWithGoogle: Token stored");
    return response.data;
  } catch (error: any) {
    console.error(
      "loginWithGoogle: Request failed",
      error?.response?.status,
      error?.response?.data,
    );
    throw error;
  }
};

export const getCurrentUser = async (): Promise<AuthResponse["user"]> => {
  const response = await api.get("/auth/me");
  return response.data;
};

export const setAuthToken = async (token: string): Promise<void> => {
  await AsyncStorage.setItem("authToken", token);
};

export const getAuthToken = async (): Promise<string | null> => {
  return await AsyncStorage.getItem("authToken");
};

export const removeAuthToken = async (): Promise<void> => {
  await AsyncStorage.removeItem("authToken");
};

export const logout = async (): Promise<void> => {
  await AsyncStorage.removeItem("authToken");
};
