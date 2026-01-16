# Alexandria
A home library management application

## Overview
Alexandria is a full-stack home library management application that allows users to:
- Catalog and manage their personal book collections
- Search for books using text queries or image recognition
- Share libraries publicly or keep them private
- Loan books to friends and track lending status
- Connect with friends and view their public libraries

## Technology Stack

### Backend
- **Framework**: ASP.NET Core Web API (.NET 10)
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: JWT + Google OAuth
- **Deployment**: .NET Aspire + Azure

### Frontend
- **Mobile**: React Native (Expo)
- **Navigation**: React Navigation
- **State Management**: React Hooks + Axios for API calls

## Features

### Book Management
- Search books by title, author, genre, ISBN, or publication year
- Image-based book search (requires OCR service integration)
- Add books to personal libraries
- Categorize books with custom genres and tags

### Library Management
- Create multiple libraries (e.g., "Home Library", "Office Books")
- Set libraries as public (shareable) or private
- Track book status: Available, Checked Out, Waiting to be Loaned

### Loan System
- Loan books to friends with optional due dates
- Track active loans (both borrowed and lent)
- View loan history and status
- Automatic status updates when books are returned

### Social Features
- Connect with friends via friend requests
- View friends' public libraries
- See which books are available from friends

### UI/UX Design
- Clean, minimalist interface
- Eye-friendly color scheme
- Intuitive navigation with bottom tabs
- Smooth transitions and interactions

## Project Structure

```
Alexandria/
├── src/
│   ├── Alexandria.API/          # Backend Web API
│   │   ├── Controllers/         # API endpoints
│   │   ├── Models/              # Database models
│   │   ├── Data/                # DbContext and migrations
│   │   ├── DTOs/                # Data transfer objects
│   │   └── Services/            # Business logic
│   └── Alexandria.AppHost/      # Aspire orchestration
├── AlexandriaMobile/            # React Native mobile app
│   ├── src/
│   │   ├── screens/             # App screens
│   │   ├── navigation/          # Navigation setup
│   │   ├── services/            # API client
│   │   └── types/               # TypeScript types
│   └── App.tsx
└── Alexandria.sln               # Solution file
```

## Getting Started

### Prerequisites
- .NET 10 SDK
- Node.js 20+
- PostgreSQL (or use Docker)
- Expo CLI (for React Native development)

### Backend Setup

1. **Configure Database Connection**
   
   Update `appsettings.json` in `Alexandria.API`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=alexandria;Username=your_user;Password=your_password"
     }
   }
   ```

2. **Configure Authentication**
   
   Set up Google OAuth credentials:
   ```json
   {
     "Authentication": {
       "Google": {
         "ClientId": "your-google-client-id",
         "ClientSecret": "your-google-client-secret"
       }
     }
   }
   ```

3. **Run Database Migrations**
   ```bash
   cd src/Alexandria.API
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

4. **Run the API**
   ```bash
   cd src/Alexandria.API
   dotnet run
   ```

   API will be available at: `https://localhost:5001`

### Using Aspire for Deployment

1. **Run with Aspire**
   ```bash
   cd src/Alexandria.AppHost
   dotnet run
   ```

   This will:
   - Start PostgreSQL in a container
   - Configure the database connection
   - Launch the API
   - Open the Aspire dashboard

### Mobile App Setup

1. **Install Dependencies**
   ```bash
   cd AlexandriaMobile
   npm install
   ```

2. **Configure API URL**
   
   Update `src/services/api.ts`:
   ```typescript
   const API_BASE_URL = 'http://your-api-url:5000/api';
   ```

3. **Run the App**
   
   For web:
   ```bash
   npm run web
   ```
   
   For iOS (requires Mac):
   ```bash
   npm run ios
   ```
   
   For Android:
   ```bash
   npm run android
   ```

## API Endpoints

### Books
- `GET /api/books` - Search books
- `GET /api/books/{id}` - Get book details
- `POST /api/books` - Create a new book
- `POST /api/books/search-by-image` - Search by image

### Libraries
- `GET /api/libraries` - Get user's libraries
- `GET /api/libraries/{id}` - Get library details
- `POST /api/libraries` - Create a library
- `GET /api/libraries/{id}/books` - Get books in a library
- `POST /api/libraries/{id}/books` - Add book to library
- `DELETE /api/libraries/{id}/books/{bookId}` - Remove book from library

### Loans
- `GET /api/loans` - Get loans (with filters)
- `GET /api/loans/{id}` - Get loan details
- `POST /api/loans` - Create a loan
- `PATCH /api/loans/{id}/status` - Update loan status

### Friends
- `GET /api/friends` - Get friends list
- `POST /api/friends/{id}` - Send friend request
- `PUT /api/friends/{id}/accept` - Accept friend request
- `DELETE /api/friends/{id}` - Remove friend

## Deployment

### Azure Deployment

The application is designed to be deployed on Azure using:
- **Azure App Service** for the API
- **Azure Database for PostgreSQL** for the database
- **Azure Container Apps** (optional, via Aspire)

### GitHub Actions

Configure GitHub Actions workflows for CI/CD:
1. Create `.github/workflows/deploy.yml`
2. Configure Azure credentials as GitHub secrets
3. Push to trigger deployment

## Development Roadmap

- [x] Backend API with Entity Framework Core
- [x] PostgreSQL database integration
- [x] JWT authentication setup
- [x] Book management endpoints
- [x] Library management system
- [x] Loan tracking system
- [x] Friends/social features
- [x] React Native mobile app structure
- [x] Navigation and screens
- [x] API integration
- [x] Aspire deployment configuration
- [ ] Complete Google OAuth integration
- [ ] Implement OCR service for image search
- [ ] Add comprehensive testing
- [ ] Deploy to Azure
- [ ] Add push notifications
- [ ] Implement real-time updates

## Contributing

Contributions are welcome! Please read the contributing guidelines before submitting PRs.

## License

This project is licensed under the MIT License.

## Support

For issues and questions, please use the GitHub issue tracker.

