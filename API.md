# Alexandria API Documentation

## Base URL

- **Development**: `http://localhost:5000/api`
- **Production**: `https://your-domain.azurewebsites.net/api`

## Authentication

All API endpoints (except public endpoints) require JWT authentication.

### Headers

```
Authorization: Bearer <your-jwt-token>
Content-Type: application/json
```

### Getting a Token

Authentication is handled via Google OAuth. After successful authentication, the API returns a JWT token that should be included in subsequent requests.

---

## Books API

### Search Books

Search for books using various filters.

**Endpoint**: `GET /books`

**Query Parameters**:
- `query` (string, optional): Search in title, author, or description
- `author` (string, optional): Filter by author name
- `genre` (string, optional): Filter by genre
- `isbn` (string, optional): Filter by ISBN
- `publishedYear` (integer, optional): Filter by publication year

**Example Request**:
```http
GET /api/books?query=tolkien&genre=fantasy
Authorization: Bearer <token>
```

**Example Response**:
```json
[
  {
    "id": 1,
    "title": "The Lord of the Rings",
    "author": "J.R.R. Tolkien",
    "isbn": "978-0618640157",
    "publisher": "Houghton Mifflin",
    "publishedYear": 2005,
    "description": "Epic fantasy novel...",
    "coverImageUrl": "https://example.com/cover.jpg",
    "genre": "Fantasy",
    "pageCount": 1178
  }
]
```

---

### Get Book Details

Get details of a specific book.

**Endpoint**: `GET /books/{id}`

**Path Parameters**:
- `id` (integer): Book ID

**Example Request**:
```http
GET /api/books/1
Authorization: Bearer <token>
```

**Example Response**:
```json
{
  "id": 1,
  "title": "The Lord of the Rings",
  "author": "J.R.R. Tolkien",
  "isbn": "978-0618640157",
  "publisher": "Houghton Mifflin",
  "publishedYear": 2005,
  "description": "Epic fantasy novel...",
  "coverImageUrl": "https://example.com/cover.jpg",
  "genre": "Fantasy",
  "pageCount": 1178
}
```

---

### Create Book

Add a new book to the database.

**Endpoint**: `POST /books`

**Request Body**:
```json
{
  "title": "The Hobbit",
  "author": "J.R.R. Tolkien",
  "isbn": "978-0547928227",
  "publisher": "Houghton Mifflin",
  "publishedYear": 2012,
  "description": "Bilbo Baggins' adventure...",
  "coverImageUrl": "https://example.com/hobbit.jpg",
  "genre": "Fantasy",
  "pageCount": 300
}
```

**Example Response**:
```json
{
  "id": 2,
  "title": "The Hobbit",
  "author": "J.R.R. Tolkien",
  "isbn": "978-0547928227",
  "publisher": "Houghton Mifflin",
  "publishedYear": 2012,
  "description": "Bilbo Baggins' adventure...",
  "coverImageUrl": "https://example.com/hobbit.jpg",
  "genre": "Fantasy",
  "pageCount": 300
}
```

---

### Search by Image

Search for books using an image (cover or barcode).

**Endpoint**: `POST /books/search-by-image`

**Request**: `multipart/form-data`
- `image` (file): Image file (JPEG, PNG)

**Example Request**:
```http
POST /api/books/search-by-image
Authorization: Bearer <token>
Content-Type: multipart/form-data

image=<binary-image-data>
```

**Example Response**:
```json
[
  {
    "id": 1,
    "title": "Found Book Title",
    "author": "Author Name",
    "isbn": "123-4567890123"
  }
]
```

**Note**: Requires OCR service integration (currently returns empty results).

---

### Scan Single Book

Scan a single book (cover or barcode) and get a preview for confirmation before adding to a library.

**Endpoint**: `POST /books/scan-single`

**Request**: `multipart/form-data`
- `image` (file): Image file (JPEG, PNG) of book cover or barcode

**Example Request**:
```http
POST /api/books/scan-single
Authorization: Bearer <token>
Content-Type: multipart/form-data

image=<binary-image-data>
```

**Example Response**:
```json
{
  "existingBookId": 1,
  "title": "The Lord of the Rings",
  "author": "J.R.R. Tolkien",
  "isbn": "978-0618640157",
  "publisher": "Houghton Mifflin",
  "publishedYear": 2005,
  "description": "Epic fantasy novel...",
  "coverImageUrl": "https://example.com/cover.jpg",
  "genre": "Fantasy",
  "pageCount": 1178,
  "source": 0,
  "confidence": 0.95,
  "externalId": null
}
```

**Source Values**:
- `0`: Local (found in database)
- `1`: OpenLibrary
- `2`: GoogleBooks
- `3`: OcrText (extracted from image text)

**Note**: If `existingBookId` is set, the book was found in the local database.

---

### Scan Bookshelf

Scan a bookshelf image to detect multiple books at once.

**Endpoint**: `POST /books/scan-bookshelf`

**Request**: `multipart/form-data`
- `image` (file): Image file (JPEG, PNG) of a bookshelf

**Example Request**:
```http
POST /api/books/scan-bookshelf
Authorization: Bearer <token>
Content-Type: multipart/form-data

image=<binary-image-data>
```

**Example Response**:
```json
[
  {
    "existingBookId": null,
    "title": "The Hobbit",
    "author": "J.R.R. Tolkien",
    "isbn": "978-0547928227",
    "source": 1,
    "confidence": 0.85,
    "externalId": "OL27516W"
  },
  {
    "existingBookId": 5,
    "title": "1984",
    "author": "George Orwell",
    "isbn": "978-0451524935",
    "source": 0,
    "confidence": 1.0
  }
]
```

---

### Lookup Book by ISBN

Look up book details by ISBN from external sources (Open Library, Google Books).

**Endpoint**: `GET /books/lookup/{isbn}`

**Path Parameters**:
- `isbn` (string): ISBN-10 or ISBN-13 (will be normalized)

**Example Request**:
```http
GET /api/books/lookup/978-0618640157
Authorization: Bearer <token>
```

**Example Response**:
```json
{
  "existingBookId": null,
  "title": "The Lord of the Rings",
  "author": "J.R.R. Tolkien",
  "isbn": "9780618640157",
  "publisher": "Houghton Mifflin",
  "publishedYear": 2005,
  "description": "Epic fantasy novel...",
  "coverImageUrl": "https://covers.openlibrary.org/b/isbn/9780618640157-L.jpg",
  "source": 1,
  "confidence": 1.0,
  "externalId": "OL27516W"
}
```

**Note**: Returns 404 if ISBN is not found in any external source.

---

## Libraries API

### Get Libraries

Get user's libraries or public libraries.

**Endpoint**: `GET /libraries`

**Query Parameters**:
- `isPublic` (boolean, optional): Filter by public/private libraries
  - `true`: Only public libraries
  - `false`: Only private libraries
  - (omitted): User's own libraries

**Example Request**:
```http
GET /api/libraries?isPublic=true
Authorization: Bearer <token>
```

**Example Response**:
```json
[
  {
    "id": 1,
    "name": "My Home Library",
    "isPublic": true,
    "userId": 5,
    "createdAt": "2026-01-14T18:00:00Z"
  }
]
```

---

### Get Library Details

Get details of a specific library.

**Endpoint**: `GET /libraries/{id}`

**Path Parameters**:
- `id` (integer): Library ID

**Example Response**:
```json
{
  "id": 1,
  "name": "My Home Library",
  "isPublic": true,
  "userId": 5,
  "createdAt": "2026-01-14T18:00:00Z"
}
```

---

### Create Library

Create a new library.

**Endpoint**: `POST /libraries`

**Request Body**:
```json
{
  "name": "Office Books",
  "isPublic": false
}
```

**Example Response**:
```json
{
  "id": 2,
  "name": "Office Books",
  "isPublic": false,
  "userId": 5,
  "createdAt": "2026-01-14T18:30:00Z"
}
```

---

### Get Library Books

Get all books in a library.

**Endpoint**: `GET /libraries/{id}/books`

**Path Parameters**:
- `id` (integer): Library ID

**Example Response**:
```json
[
  {
    "id": 1,
    "libraryId": 1,
    "status": 0,
    "addedAt": "2026-01-14T18:00:00Z",
    "book": {
      "id": 1,
      "title": "The Lord of the Rings",
      "author": "J.R.R. Tolkien",
      "isbn": "978-0618640157"
    }
  }
]
```

**Status Values**:
- `0`: Available
- `1`: CheckedOut
- `2`: WaitingToBeLoanedOut

---

### Add Book to Library

Add a book to a library.

**Endpoint**: `POST /libraries/{id}/books`

**Path Parameters**:
- `id` (integer): Library ID

**Request Body**:
```json
{
  "bookId": 1,
  "status": 0
}
```

**Example Response**:
```json
{
  "id": 1,
  "libraryId": 1,
  "status": 0,
  "addedAt": "2026-01-14T18:45:00Z",
  "book": {
    "id": 1,
    "title": "The Lord of the Rings",
    "author": "J.R.R. Tolkien"
  }
}
```

---

### Remove Book from Library

Remove a book from a library.

**Endpoint**: `DELETE /libraries/{libraryId}/books/{libraryBookId}`

**Path Parameters**:
- `libraryId` (integer): Library ID
- `libraryBookId` (integer): Library Book ID

**Example Response**: `204 No Content`

---

### Confirm Books to Library

Confirm and add scanned books (from scan-single or scan-bookshelf) to a library. Creates new book records if they don't exist, then adds them to the library.

**Endpoint**: `POST /libraries/{id}/confirm-books`

**Path Parameters**:
- `id` (integer): Library ID

**Request Body**:
```json
{
  "books": [
    {
      "existingBookId": 1,
      "title": "The Lord of the Rings",
      "author": "J.R.R. Tolkien",
      "isbn": "978-0618640157",
      "source": 0,
      "confidence": 1.0
    },
    {
      "existingBookId": null,
      "title": "The Hobbit",
      "author": "J.R.R. Tolkien",
      "isbn": "978-0547928227",
      "publisher": "Houghton Mifflin",
      "publishedYear": 2012,
      "source": 1,
      "confidence": 0.9,
      "externalId": "OL27516W"
    }
  ]
}
```

**Example Response**:
```json
{
  "results": [
    {
      "bookId": 1,
      "libraryBookId": 10,
      "title": "The Lord of the Rings",
      "wasCreated": false,
      "addedToLibrary": true,
      "error": null
    },
    {
      "bookId": 15,
      "libraryBookId": 11,
      "title": "The Hobbit",
      "wasCreated": true,
      "addedToLibrary": true,
      "error": null
    }
  ],
  "successCount": 2,
  "failedCount": 0
}
```

**Notes**:
- If `existingBookId` is set, that book is used directly
- If `existingBookId` is null, a new book record is created
- ISBN is normalized to ISBN-13 format before saving

---

## Loans API

### Get Loans

Get loans (borrowed or lent).

**Endpoint**: `GET /loans`

**Query Parameters**:
- `filter` (string, optional): Filter loans
  - `borrowed`: Books borrowed by user
  - `lent`: Books lent by user
  - (omitted): All loans involving user

**Example Request**:
```http
GET /api/loans?filter=borrowed
Authorization: Bearer <token>
```

**Example Response**:
```json
[
  {
    "id": 1,
    "libraryBookId": 1,
    "lenderId": 5,
    "lenderName": "John Doe",
    "borrowerId": 10,
    "borrowerName": "Jane Smith",
    "loanDate": "2026-01-10T18:00:00Z",
    "dueDate": "2026-01-24T18:00:00Z",
    "returnedDate": null,
    "status": 1,
    "book": {
      "id": 1,
      "title": "The Lord of the Rings",
      "author": "J.R.R. Tolkien"
    }
  }
]
```

**Status Values**:
- `0`: Pending
- `1`: Active
- `2`: Returned
- `3`: Overdue
- `4`: Cancelled

---

### Get Loan Details

Get details of a specific loan.

**Endpoint**: `GET /loans/{id}`

**Path Parameters**:
- `id` (integer): Loan ID

**Example Response**: Same as Get Loans response (single item)

---

### Create Loan

Create a new loan.

**Endpoint**: `POST /loans`

**Request Body**:
```json
{
  "libraryBookId": 1,
  "borrowerId": 10,
  "dueDate": "2026-01-24T18:00:00Z"
}
```

**Example Response**:
```json
{
  "id": 1,
  "libraryBookId": 1,
  "lenderId": 5,
  "lenderName": "John Doe",
  "borrowerId": 10,
  "borrowerName": "Jane Smith",
  "loanDate": "2026-01-14T18:00:00Z",
  "dueDate": "2026-01-24T18:00:00Z",
  "returnedDate": null,
  "status": 1,
  "book": {
    "id": 1,
    "title": "The Lord of the Rings"
  }
}
```

---

### Update Loan Status

Update the status of a loan.

**Endpoint**: `PATCH /loans/{id}/status`

**Path Parameters**:
- `id` (integer): Loan ID

**Request Body**:
```json
{
  "status": 2
}
```

**Example Response**: Same as Get Loan Details

---

## Friends API

### Get Friends

Get user's friends list.

**Endpoint**: `GET /friends`

**Example Response**:
```json
[
  {
    "id": 1,
    "friend": {
      "id": 10,
      "email": "jane@example.com",
      "name": "Jane Smith",
      "profilePictureUrl": "https://example.com/jane.jpg"
    },
    "createdAt": "2026-01-01T18:00:00Z"
  }
]
```

---

### Send Friend Request

Send a friend request to another user.

**Endpoint**: `POST /friends/{friendId}`

**Path Parameters**:
- `friendId` (integer): User ID to send request to

**Example Response**: `200 OK`

---

### Accept Friend Request

Accept a pending friend request.

**Endpoint**: `PUT /friends/{friendshipId}/accept`

**Path Parameters**:
- `friendshipId` (integer): Friendship ID

**Example Response**: `200 OK`

---

### Remove Friend

Remove a friend or decline a friend request.

**Endpoint**: `DELETE /friends/{friendshipId}`

**Path Parameters**:
- `friendshipId` (integer): Friendship ID

**Example Response**: `204 No Content`

---

## Error Responses

All endpoints may return the following error responses:

### 400 Bad Request
```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "field": ["Error message"]
  }
}
```

### 401 Unauthorized
```json
{
  "status": 401,
  "message": "Unauthorized"
}
```

### 403 Forbidden
```json
{
  "status": 403,
  "message": "Access denied"
}
```

### 404 Not Found
```json
{
  "status": 404,
  "message": "Resource not found"
}
```

### 500 Internal Server Error
```json
{
  "status": 500,
  "message": "An error occurred while processing your request"
}
```

---

## Rate Limiting

Currently no rate limiting is implemented. For production, consider implementing rate limiting using:

- ASP.NET Core Rate Limiting middleware
- Azure API Management
- Third-party services (e.g., CloudFlare)

---

## Versioning

API version: `v1`

Future versions will be accessible via:
- Route prefix: `/api/v2/...`
- Header: `Api-Version: 2.0`

---

## Testing the API

### Using curl

```bash
# Get books
curl -H "Authorization: Bearer <token>" \
  https://localhost:5001/api/books

# Create a book
curl -X POST \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"title":"Test Book","author":"Test Author"}' \
  https://localhost:5001/api/books
```

### Using Postman

1. Import the API collection
2. Set up environment variables:
   - `base_url`: `http://localhost:5000/api`
   - `token`: Your JWT token
3. Use `{{base_url}}` and `{{token}}` in requests

### Using the .http file

The project includes `Alexandria.API.http` for testing with Visual Studio or VS Code with REST Client extension.

---

## Support

For API issues or questions:
- GitHub Issues: [Link to repository issues]
- Documentation: See README.md and CONFIGURATION.md
