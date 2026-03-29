# Markdown Notes

[中文](./README.md)

A web-based Markdown note-taking application with note categorization, version history management, and sharing capabilities.

## Features

### User Authentication

- **Register/Login** - JWT token authentication
- **Secure Storage** - Tokens stored in HttpOnly Cookie to prevent XSS attacks

### Note Management

- **Markdown Editor** - WYSIWYG editing with Vditor
- **Version History** - Manual save with restore and delete support
- **Sharing** - Generate share links with DOMPurify HTML sanitization
- **Work Log** - Auto-generate monthly work log templates

### Category Management

- **Tree Structure** - Multi-level category hierarchy
- **Drag Reorder** - Drag to reorder categories within the same level
- **Parent-Child** - Drag to change category parent (with visual indicators)
- **Category Actions** - Pin, rename, and delete categories

### User Interface

- **Sidebar** - Collapsible with resizable width
- **Breadcrumb Navigation** - Shows category path in editor
- **Folder Detail Page** - View sub-categories and notes

## Tech Stack

### Frontend

- **Framework**: Vue 3 + Composition API + TypeScript
- **Build**: Vite 5
- **UI**: Element Plus
- **State Management**: Pinia
- **Router**: Vue Router 4
- **Markdown Editor**: Vditor
- **HTTP Client**: Axios
- **XSS Protection**: DOMPurify

### Backend

- **Framework**: ASP.NET Core 10.0
- **ORM**: Entity Framework Core
- **Database**: MySQL
- **Auth**: JWT + BCrypt
- **ID Generation**: Yitter.IdGenerator (Snowflake ID)
- **Rate Limiting**: Built-in middleware

## Project Structure

```
test-md/
├── client/                         # Frontend (Vue 3 + TypeScript)
│   ├── src/
│   │   ├── api/                   # API client (Axios)
│   │   ├── components/             # Reusable components
│   │   │   ├── Sidebar.vue         # Sidebar component
│   │   │   └── FolderTreeItem.vue  # Folder tree component
│   │   ├── composables/            # Composable functions
│   │   │   ├── useSidebar.ts       # Sidebar logic
│   │   │   └── useCommon.ts        # Common utilities
│   │   ├── stores/                 # Pinia stores
│   │   │   ├── auth.ts             # Auth state
│   │   │   └── notes.ts            # Notes state
│   │   ├── views/                  # Page components
│   │   │   ├── Login.vue           # Login page
│   │   │   ├── Register.vue        # Registration page
│   │   │   ├── Home.vue            # Notes list home
│   │   │   ├── NoteEditorVditor.vue # Markdown editor
│   │   │   ├── FolderDetail.vue   # Folder detail page
│   │   │   ├── About.vue           # About page
│   │   │   └── Shared.vue          # Shared note page
│   │   ├── router/                 # Router configuration
│   │   ├── utils/                  # Utilities
│   │   ├── App.vue                 # Root component
│   │   └── main.ts                 # Entry file
│   ├── index.html
│   ├── package.json
│   ├── tsconfig.json               # TypeScript config
│   └── vite.config.ts             # Vite config
│
├── server-dotnet/                  # Backend (.NET 10.0)
│   ├── Controllers/               # API Controllers
│   │   ├── AuthController.cs       # Authentication
│   │   ├── NotesController.cs      # Notes API
│   │   ├── FoldersController.cs   # Folders API
│   │   └── SharedController.cs    # Sharing API
│   ├── Models/                    # Data Models
│   │   ├── User.cs
│   │   ├── Note.cs
│   │   ├── Folder.cs
│   │   └── NoteVersion.cs
│   ├── Data/                      # Database Context
│   │   └── AppDbContext.cs
│   ├── DTOs/                      # Data Transfer Objects
│   │   └── AuthDtos.cs
│   ├── Middleware/                # Middleware
│   │   └── GlobalExceptionHandler.cs
│   ├── Converters/                # JSON Converters
│   │   ├── LongToStringConverter.cs
│   │   └── DateTimeOffsetConverter.cs
│   ├── Constants/                 # Application Constants
│   │   └── AppConstants.cs
│   ├── appsettings.json           # Configuration
│   └── Program.cs                 # Entry point
│
├── README.md                      # Chinese README
├── README-en.md                   # English README
└── LICENSE                        # MIT License
```

## Quick Start

### Requirements

| Dependency | Version |
|------------|---------|
| Node.js | 18+ |
| pnpm | 9+ |
| .NET SDK | 10.0+ |
| MySQL | 5.7+ |

### Installation

```bash
# Clone the repository
git clone https://github.com/xgzhong/test-md.git
cd test-md

# Install frontend dependencies
cd client
pnpm install

# Install backend dependencies
cd ../server-dotnet
dotnet restore
```

### Configuration

1. **Create Database** (ensure MySQL is running):

```sql
CREATE DATABASE markdown_notes CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

2. **Set Environment Variables**:

| Variable | Description | Example |
|----------|-------------|---------|
| `DB_CONNECTION_STRING` | Database connection string | `Server=localhost;Database=markdown_notes;User=root;Password=xxx;` |
| `JWT_SECRET` | JWT secret (at least 32 chars in production) | `your-super-secret-key-at-least-32-chars` |
| `CORS_ALLOWED_ORIGINS` | Allowed frontend origins (comma-separated) | `http://localhost:5173,https://yourdomain.com` |

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

> **Important**: Always configure sensitive information via environment variables in production. Do not use hardcoded defaults.

### Start Development Servers

```bash
# Terminal 1: Start backend
cd server-dotnet
dotnet run
# Backend at: http://localhost:5000

# Terminal 2: Start frontend
cd client
pnpm dev
# Frontend at: http://localhost:5173
```

> Database tables are automatically created on first run.

### Build for Production

```bash
# Frontend build
cd client
pnpm build
# Build output in client/dist

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

### Folders

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

| Table | Description |
|-------|-------------|
| users | User table |
| folders | Category table |
| notes | Notes table |
| note_versions | Note versions table |

### Field Standards

| Field Type | Description |
|------------|-------------|
| Id | Snowflake ID (BIGINT), generated by Yitter.IdGenerator |
| Version | Data version (BIGINT), for optimistic concurrency |
| CreatedAt/UpdatedAt | Timestamp (DateTimeOffset) |
| CreatedBy/UpdatedBy | Creator/Modifier ID (BIGINT) |

### JSON Serialization

- `long` type IDs are returned as strings (e.g., `"id": "785339216482373"`)
- `DateTime` is formatted as `yyyy-MM-dd HH:mm:ss`

## Security Features

- **JWT Token** - Stored in HttpOnly Cookie to prevent XSS attacks
- **Password Hashing** - BCrypt algorithm for secure storage
- **XSS Protection** - Shared content sanitized with DOMPurify
- **Rate Limiting** - Auth: 5 req/min, Notes: 60 req/sec to prevent brute force
- **CORS** - Strict cross-origin policy configuration
- **Soft Delete** - Notes are soft-deleted and can be recovered
- **Request Cancellation** - Auto-cancel pending requests on route change

## Performance Optimization

- **Editor Preloading** - Preload Vditor editor on note card hover
- **N+1 Query Optimization** - Use bulk update instead of loop update
- **Pagination** - Notes list supports paginated loading
- **Request Deduplication** - Note auto-save with debounce

## FAQ

**Q: Editor is blank, what should I do?**
A: Ensure the backend API is running. Check browser console for errors.

**Q: Drag and drop categories not working?**
A: Make sure your browser supports HTML5 Drag and Drop API.

**Q: How to customize folder icons?**
A: Current version uses Element Plus built-in icons. Custom icons coming soon.

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

MIT License
