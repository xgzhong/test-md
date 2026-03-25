# Markdown Notes App

[中文](./README.md)

A web-based Markdown note-taking application with note categorization, version history management, and sharing capabilities.

## Features

- **User Authentication** - Register, Login, JWT token authentication (HttpOnly Cookie storage)
- **Note Management** - Create, edit, delete Markdown notes
- **Category Management** - Tree folder structure organization
  - Multi-level categories (hierarchical structure)
  - Drag-and-drop to reorder within same level
  - Drag-and-drop to change parent-child relationships (with visual indicator)
  - Pin, rename, and delete categories
- **Version History** - Manual save version history with restore and delete support
- **Note Sharing** - Generate share links for others to view (XSS protection enabled)
- **Markdown Editor** - WYSIWYG editor using Vditor
- **Sidebar** - Collapsible sidebar with resizable width
- **Folder Detail Page** - View sub-categories and notes within a category
- **Breadcrumb Navigation** - Show category path in note editor
- **Work Log** - Auto-generate monthly work log template
- **Snowflake ID** - Distributed ID generation using Yitter.IdGenerator

## Tech Stack

### Frontend

- Vue 3 + Composition API
- Vite
- Element Plus
- Axios
- Vue Router
- Vditor (Markdown editor)
- DOMPurify (XSS protection)

### Backend

- ASP.NET Core 10.0
- Entity Framework Core
- MySQL
- JWT Authentication
- Yitter.IdGenerator (Snowflake ID)
- BCrypt.Net-Next (Password hashing)
- Rate Limiting

## Project Structure

```
test-md/
├── client/                      # Frontend (Vue 3)
│   ├── src/
│   │   ├── api/                 # API client
│   │   ├── components/           # Reusable components
│   │   │   ├── Sidebar.vue       # Sidebar component
│   │   │   └── FolderTreeItem.vue # Folder tree component
│   │   ├── views/               # Page components
│   │   │   ├── Login.vue        # Login page
│   │   │   ├── Register.vue     # Registration page
│   │   │   ├── Home.vue         # Notes list home page
│   │   │   ├── NoteEditorVditor.vue # Markdown editor
│   │   │   ├── FolderDetail.vue  # Category detail page
│   │   │   └── Shared.vue       # Shared note page
│   │   ├── router/              # Router configuration
│   │   ├── main.js              # Entry file
│   │   ├── App.vue              # Root component
│   │   └── style.css            # Global styles
│   ├── index.html
│   ├── package.json
│   └── vite.config.js           # Vite configuration
│
├── server-dotnet/               # Backend (.NET 10.0)
│   ├── Controllers/             # API Controllers
│   │   ├── AuthController.cs   # Authentication
│   │   ├── NotesController.cs   # Notes API
│   │   ├── FoldersController.cs # Categories API
│   │   └── SharedController.cs  # Sharing API
│   ├── Models/                 # Data Models
│   │   ├── User.cs
│   │   ├── Note.cs
│   │   ├── Folder.cs
│   │   └── NoteVersion.cs
│   ├── Data/                   # Database Context
│   │   └── AppDbContext.cs
│   ├── DTOs/                   # Data Transfer Objects
│   │   └── AuthDtos.cs
│   ├── Middleware/             # Middleware
│   │   └── GlobalExceptionHandler.cs
│   ├── Converters/             # JSON Converters
│   │   ├── LongToStringConverter.cs
│   │   └── DateTimeOffsetConverter.cs
│   ├── Constants/             # Application Constants
│   │   └── AppConstants.cs
│   ├── appsettings.json       # Configuration file
│   └── Program.cs              # Entry file
│
├── README.md                   # Chinese README
├── README-en.md                # English README
└── LICENSE                     # MIT License
```

## Quick Start

### Prerequisites

- Node.js 18+
- pnpm 9+
- .NET SDK 10.0+
- MySQL 5.7+

### Installation

```bash
# Install frontend dependencies
cd client
pnpm install

# Install backend dependencies
cd ../server-dotnet
dotnet restore
```

### Configuration

1. **Database Setup** (ensure MySQL is running):

```sql
CREATE DATABASE markdown_notes CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

2. **Environment Variables** - Set the following environment variables:

Key configurations:
- `DB_CONNECTION_STRING` - Database connection string
- `JWT_SECRET` - JWT secret (use a long random string in production, at least 32 characters)
- `CORS_ALLOWED_ORIGINS` - CORS allowed frontend origins (comma-separated for multiple)

3. **Backend Configuration** (`server-dotnet/appsettings.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "${DB_CONNECTION_STRING}"
  },
  "Jwt": {
    "Secret": "${JWT_SECRET}"
  },
  "CORS": {
    "AllowedOrigins": "${CORS_ALLOWED_ORIGINS}"
  }
}
```

**Important**: Always configure sensitive information via environment variables in production. Do not use hardcoded defaults.

### Start Development Servers

```bash
# Start backend (terminal 1)
cd server-dotnet
dotnet run
# Backend runs at http://localhost:5000

# Start frontend (terminal 2)
cd client
pnpm dev
# Frontend runs at http://localhost:5173
```

### Initialize Database

Database tables are automatically created on first run.

### Build for Production

```bash
# Frontend build
cd client
pnpm build
# Build output is in client/dist

# Backend publish
cd server-dotnet
dotnet publish -c Release
```

## API Endpoints

### Authentication

| Method | Path | Description |
|--------|------|-------------|
| POST | /api/auth/register | User registration |
| POST | /api/auth/login | User login |
| POST | /api/auth/logout | User logout |
| GET | /api/auth/me | Get current user info |

### Notes

| Method | Path | Description |
|--------|------|-------------|
| GET | /api/notes | Get notes list (supports folderId, search params) |
| GET | /api/notes/:id | Get note details |
| POST | /api/notes | Create note |
| PUT | /api/notes/:id | Update note |
| DELETE | /api/notes/:id | Delete note (soft delete) |
| POST | /api/notes/:id/share | Share note |
| POST | /api/notes/:id/unshare | Unshare note |
| GET | /api/notes/:id/versions | Get version history |
| POST | /api/notes/:id/restore/:versionId | Restore version |
| DELETE | /api/notes/:id/versions/:versionId | Delete version |

### Folders (Categories)

| Method | Path | Description |
|--------|------|-------------|
| GET | /api/folders | Get folder list (tree structure) |
| POST | /api/folders | Create folder |
| PUT | /api/folders/:id | Update folder (name, parent) |
| DELETE | /api/folders/:id | Delete folder |
| PUT | /api/folders/reorder | Reorder folders |
| PUT | /api/folders/:id/pin | Pin/unpin folder |

### Sharing

| Method | Path | Description |
|--------|------|-------------|
| GET | /api/shared/:token | View note via share link |

## Database Design

### Tables

All tables use Snowflake ID as primary key (`BIGINT`) with audit fields:

- **users** - User table
- **folders** - Category table
- **notes** - Notes table
- **note_versions** - Note versions table

### Field Standards

| Field Type | Description |
|------------|-------------|
| Id | Snowflake ID (BIGINT), generated by Yitter.IdGenerator |
| Version | Data version (BIGINT), in Snowflake ID format |
| CreatedAt/UpdatedAt | Timestamp (DateTimeOffset) |
| CreatedBy/UpdatedBy | Creator/Modifier ID (BIGINT) |

### JSON Serialization

API response format:
- `long` type IDs are returned as strings (e.g., `"id": "785339216482373"`)
- `DateTime` is formatted as `yyyy-MM-dd HH:mm:ss`

## Security Features

- JWT tokens stored in HttpOnly Cookie to prevent XSS attacks
- Passwords encrypted using BCrypt
- Shared content sanitized with DOMPurify to prevent XSS
- Rate limiting enabled (5 requests/minute for auth endpoints)
- CORS cross-origin policy configured

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

MIT License
