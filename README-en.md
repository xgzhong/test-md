# Markdown Notes App

A web-based Markdown note-taking application with category management, version history, and sharing features.

## Key Features

- **User Authentication** - Register, login, JWT token authentication
- **Note Management** - Create, edit, delete Markdown notes
- **Category Management** - Organize notes into folders
- **Version History** - Auto-save note history with restore capability
- **Note Sharing** - Generate shareable links for others to view
- **Markdown Rendering** - Real-time Markdown preview

## Tech Stack

### Frontend

- Vue 3 + Composition API
- Vite
- Element Plus
- Axios
- Vue Router
- marked

### Backend

- ASP.NET Core 10.0
- Entity Framework Core
- SQLite
- JWT Authentication

## Project Structure

```
test-md/
├── client/                 # Frontend (Vue 3)
│   ├── src/
│   │   ├── api/           # API requests
│   │   ├── views/         # Page components
│   │   ├── router/        # Router config
│   │   └── main.js        # Entry point
│   └── package.json
│
├── server-dotnet/          # Backend (.NET 10.0)
│   ├── Controllers/       # API controllers
│   ├── Models/            # Data models
│   ├── Data/              # DB context
│   ├── DTOs/              # Data transfer objects
│   └── Program.cs         # Entry point
│
└── pnpm-workspace.yaml    # pnpm workspace config
```

## Quick Start

### Prerequisites

- Node.js 18+
- pnpm 9+
- .NET SDK 10.0+

### Installation

```bash
pnpm install
```

### Start Development Server

```bash
# Start backend (Terminal 1)
cd server-dotnet
dotnet run

# Start frontend (Terminal 2)
pnpm dev
```

- Frontend: <http://localhost:5173>
- Backend API: <http://localhost:5000>

### Build for Production

```bash
pnpm build
```

## API Endpoints

### Authentication

| Method | Path | Description |
|--------|------|-------------|
| POST | /api/auth/register | User registration |
| POST | /api/auth/login | User login |
| GET | /api/auth/me | Get current user |

### Notes

| Method | Path | Description |
|--------|------|-------------|
| GET | /api/notes | Get note list |
| GET | /api/notes/:id | Get note details |
| POST | /api/notes | Create note |
| PUT | /api/notes/:id | Update note |
| DELETE | /api/notes/:id | Delete note |
| POST | /api/notes/:id/share | Share note |
| POST | /api/notes/:id/unshare | Unshare note |
| GET | /api/notes/:id/versions | Get version history |
| POST | /api/notes/:id/restore/:versionId | Restore version |

### Folders

| Method | Path | Description |
|--------|------|-------------|
| GET | /api/folders | Get folder list |
| POST | /api/folders | Create folder |
| PUT | /api/folders/:id | Update folder |
| DELETE | /api/folders/:id | Delete folder |

### Sharing

| Method | Path | Description |
|--------|------|-------------|
| GET | /api/shared/:token | View note via share link |

## Configuration

### Backend Configuration (server-dotnet/appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=database.sqlite"
  },
  "Jwt": {
    "Secret": "your-secret-key"
  }
}
```

### Frontend Proxy Config (client/vite.config.js)

The frontend proxies to `http://localhost:5000` by default. Edit `vite.config.js` to change it.

## License

MIT License

---

[中文](./README.md)
