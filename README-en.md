[中文](./README.md)

# Markdown Notes App

A web-based Markdown note-taking application with note categorization, version history management, and sharing capabilities.

## Features

- **User Authentication** - Register, Login, JWT token authentication
- **Note Management** - Create, edit, delete Markdown notes
- **Category Management** - Organize notes into folders with drag-and-drop sorting and pinning
- **Version History** - Manual save version history with restore and delete support
- **Note Sharing** - Generate share links for others to view
- **Markdown Preview** - Real-time Markdown content preview
- **Sidebar** - Collapsible sidebar
- **Work Log** - Auto-generate monthly work log template
- **Data Version** - Note version tracking with auto-increment

## Tech Stack

### Frontend

- Vue 3 + Composition API
- Vite
- Element Plus
- Axios
- Vue Router
- marked

### Backend

- ASP.NET Core 9.0
- Entity Framework Core
- MySQL
- JWT Authentication

## Project Structure

```
test-md/
├── client/                      # Frontend (Vue 3)
│   ├── src/
│   │   ├── api/                # API client
│   │   ├── views/              # Page components
│   │   │   ├── Login.vue       # Login page
│   │   │   ├── Register.vue   # Registration page
│   │   │   ├── Home.vue       # Notes list home page
│   │   │   ├── NoteEditor.vue # Markdown editor
│   │   │   └── Shared.vue     # Shared note page
│   │   ├── router/             # Router configuration
│   │   ├── main.js             # Entry file
│   │   ├── App.vue             # Root component
│   │   └── style.css           # Global styles
│   ├── index.html
│   ├── package.json
│   └── vite.config.js          # Vite configuration
│
├── server-dotnet/              # Backend (.NET 9.0)
│   ├── Controllers/            # API Controllers
│   │   ├── AuthController.cs   # Authentication
│   │   ├── NotesController.cs  # Notes API
│   │   ├── FoldersController.cs # Categories API
│   │   └── SharedController.cs # Sharing API
│   ├── Models/                 # Data Models
│   │   ├── User.cs
│   │   ├── Note.cs
│   │   ├── Folder.cs
│   │   └── NoteVersion.cs
│   ├── Data/                   # Database Context
│   │   └── AppDbContext.cs
│   ├── DTOs/                   # Data Transfer Objects
│   │   ├── AuthDtos.cs
│   │   └── NoteVersionDto.cs
│   ├── Properties/             # Launch configuration
│   ├── appsettings.json        # Configuration file
│   ├── Program.cs              # Entry file
│   ├── server-dotnet.csproj
│   └── server-dotnet.slnx
│
├── README.md                   # Chinese README
└── README-en.md               # English README
```

## Quick Start

### Prerequisites

- Node.js 18+
- pnpm 9+
- .NET SDK 9.0+
- MySQL 5.7+

### Installation

```bash
# Install frontend dependencies
cd client
pnpm install

# Install backend dependencies (auto-restore on first build)
cd ../server-dotnet
dotnet restore
```

### Configuration

1. **Backend Configuration** (`server-dotnet/appsettings.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=markdown_notes;User=root;Password=your_password;"
  },
  "Jwt": {
    "Secret": "your-secret-key-change-in-production"
  }
}
```

2. **Create Database** (ensure MySQL is running):

```sql
CREATE DATABASE markdown_notes;
```

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

### Build for Production

```bash
# Frontend build
cd client
pnpm build
```

## API Endpoints

### Authentication

| Method | Path | Description |
|--------|------|-------------|
| POST | /api/auth/register | User registration |
| POST | /api/auth/login | User login |
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
| GET | /api/folders | Get folder list |
| POST | /api/folders | Create folder |
| PUT | /api/folders/:id | Update folder |
| DELETE | /api/folders/:id | Delete folder |
| PUT | /api/folders/reorder | Reorder folders |
| PUT | /api/folders/:id/pin | Pin/unpin folder |

### Sharing

| Method | Path | Description |
|--------|------|-------------|
| GET | /api/shared/:token | View note via share link |

## License

MIT License
