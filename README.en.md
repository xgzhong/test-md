# Markdown Notes

[中文](./README.md)

[![Vue](https://img.shields.io/badge/Vue-3.3.11-green)](https://vuejs.org)
[![TypeScript](https://img.shields.io/badge/TypeScript-6.0-blue)](https://www.typescriptlang.org)
[![Vite](https://img.shields.io/badge/Vite-5.0-brightgreen)](https://vitejs.dev)
[![Element Plus](https://img.shields.io/badge/Element%20Plus-2.4.4-409eff)](https://element-plus.org)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-10.0-512bd4)](https://dotnet.microsoft.com)
[![MySQL](https://img.shields.io/badge/MySQL-5.7+-orange)](https://www.mysql.com)
[![MIT](https://img.shields.io/badge/License-MIT-yellow)](LICENSE)

A web-based Markdown note-taking application with note categorization, version history management, and sharing capabilities.

## Features

### User Authentication

- **Register/Login** - JWT token authentication
- **Secure Storage** - Tokens stored in HttpOnly Cookie to prevent XSS attacks
- **Change Password** - Click username to change login password

### Note Management

- **Markdown Editor** - WYSIWYG editing with Vditor
- **Version History** - Manual save with restore and delete support
- **Sharing** - Generate share links with secure access (token not exposed in URL)
- **Work Log** - Auto-generate monthly work log templates
- **Pagination** - Paginated note list for handling large datasets
- **Image Upload** - Paste external image links to automatically upload to OSS
- **File Upload** - Support various file types up to 200MB per file
- **Wide Mode** - Editor supports wide mode toggle for large displays

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
- **Logging**: Serilog

## Project Structure

```
test-md/
├── client/                         # Frontend project
│   ├── src/
│   │   ├── api/                   # API client (Axios)
│   │   ├── components/            # Reusable components
│   │   │   ├── Sidebar.vue        # Sidebar component
│   │   │   ├── FolderTreeItem.vue # Folder tree component
│   │   │   └── ChangePasswordDialog.vue # Change password dialog
│   │   ├── composables/           # Composable functions
│   │   │   ├── useSidebar.ts      # Sidebar logic
│   │   │   └── useCommon.ts       # Common utilities
│   │   ├── stores/                # Pinia stores
│   │   │   └── auth.ts            # Auth state
│   │   ├── views/                 # Page components
│   │   │   ├── Login.vue          # Login page
│   │   │   ├── Register.vue       # Registration page
│   │   │   ├── Home.vue           # Notes list home
│   │   │   ├── NoteEditorVditor.vue # Markdown editor
│   │   │   ├── FolderDetail.vue   # Folder detail page
│   │   │   ├── About.vue          # About page
│   │   │   └── Shared.vue          # Shared note page
│   │   ├── router/                # Router configuration
│   │   ├── App.vue                # Root component
│   │   ├── main.js                # Entry file
│   │   └── style.css              # Global styles
│   ├── index.html
│   ├── package.json
│   ├── tsconfig.json               # TypeScript config
│   └── vite.config.ts             # Vite config
│
├── server-dotnet/                  # Backend project (.NET 10.0)
│   ├── Common/                     # Shared modules
│   │   ├── Paging/                # Pagination components
│   │   └── Result/                # Unified response wrapper
│   ├── Controllers/               # API Controllers
│   │   ├── AuthController.cs      # Authentication
│   │   ├── NotesController.cs      # Notes API
│   │   ├── FoldersController.cs   # Folders API
│   │   ├── SharedController.cs    # Sharing API
│   │   └── OssController.cs      # OSS Upload API
│   ├── Models/                     # Data Models
│   │   ├── User.cs
│   │   ├── Note.cs
│   │   ├── Folder.cs
│   │   └── NoteVersion.cs
│   ├── Data/                       # Database Context
│   │   └── AppDbContext.cs
│   ├── DTOs/                       # Data Transfer Objects
│   │   └── *.cs
│   ├── Middleware/                 # Middleware
│   │   └── GlobalExceptionHandler.cs
│   ├── Converters/                 # JSON Converters
│   │   ├── LongToStringConverter.cs
│   │   └── DateTimeOffsetConverter.cs
│   ├── Constants/                  # Application Constants
│   │   └── AppConstants.cs
│   ├── appsettings.json            # Configuration
│   └── Program.cs                  # Entry point
│
├── README.md                       # Chinese README
├── README.en.md                    # English README
└── LICENSE                         # MIT License
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

> On **Linux/macOS** use `export`, on **Windows** use `set` or configure in a `.env` file.

3. **OSS Configuration** (for file uploads, requires Aliyun OSS):

| Variable | Description | Example |
|----------|-------------|---------|
| `OSS:AccessKeyId` | Aliyun AccessKey ID | `LTAI5txxx` |
| `OSS:AccessKeySecret` | Aliyun AccessKey Secret | `xxx` |
| `OSS:Endpoint` | OSS Endpoint | `https://oss-cn-shanghai.aliyuncs.com` |
| `OSS:BucketName` | OSS Bucket name | `note-md` |

4. **Backend Configuration** (`server-dotnet/appsettings.json`):

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
  },
  "OSS": {
    "AccessKeyId": "${OSS_ACCESS_KEY_ID}",
    "AccessKeySecret": "${OSS_ACCESS_KEY_SECRET}",
    "Endpoint": "https://oss-cn-shanghai.aliyuncs.com",
    "BucketName": "note-md"
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

> Database tables are automatically created on first run (EF Core Code-First).

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

## Deployment

### Docker (Recommended)

```bash
# Build and start all services
docker-compose up -d
```

### Nginx Reverse Proxy

```nginx
server {
    listen 80;
    server_name yourdomain.com;

    # Frontend static files
    location / {
        root /var/www/test-md/client/dist;
        try_files $uri $uri/ /index.html;
    }

    # API reverse proxy
    location /api {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

## API Endpoints

### Authentication

| Method | Path | Description |
|--------|------|-------------|
| POST | /api/auth/register | User registration |
| POST | /api/auth/login | User login |
| POST | /api/auth/logout | User logout |
| GET | /api/auth/me | Get current user info |
| POST | /api/auth/change-password | Change password |

### Notes

| Method | Path | Description |
|--------|------|-------------|
| GET | /api/notes | Get notes list (supports folderId, search, page, pageSize) |
| GET | /api/notes/page | Paginated notes (returns items + metaData) |
| GET | /api/notes/:id | Get note details |
| POST | /api/notes | Create note |
| PUT | /api/notes/:id | Update note |
| DELETE | /api/notes/:id | Delete note (soft delete) |
| POST | /api/notes/:id/share | Share note |
| POST | /api/notes/:id/unshare | Unshare note |
| GET | /api/notes/:id/versions | Get version history |
| POST | /api/notes/:id/restore/:versionId | Restore version |
| DELETE | /api/notes/:id/versions/:versionId | Delete version |

**Paginated Response Format**:

```json
{
  "items": [...],
  "metaData": {
    "currentPage": 1,
    "pageSize": 20,
    "totalCount": 100,
    "totalPages": 5
  }
}
```

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
| POST | /api/shared/access | Secure share access (exchange token for session cookie) |
| GET | /api/shared/view/:id | View shared note via session cookie |
| POST | /api/shared/logout | Clear share session |

> **Security Note**: Share token is only used on first access, subsequent access via HttpOnly Cookie session to avoid token exposure in URL.

### OSS Upload

| Method | Path | Description |
|--------|------|-------------|
| POST | /api/oss/presigned-url | Get presigned URL for direct browser-to-OSS upload |
| POST | /api/oss/link-to-img | Download external image and upload to OSS |
| POST | /api/oss/upload | Server-side upload (fallback) |

**Presigned URL Flow**:
1. Client requests presigned URL from server
2. Server generates signed URL with 30-minute expiration
3. Client uploads directly to OSS using the presigned URL
4. No server bandwidth needed for file transfer

**Supported File Types**:

| Type | Extensions |
|------|------------|
| Images | .jpg, .jpeg, .png, .gif, .webp, .bmp, .svg, .tif, .tiff |
| Documents | .pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx |
| Text | .txt, .md, .sql, .bak, .cs, .js, .vue, .html, .htm, .css, .sass |
| Archives | .zip, .rar |

**File Naming**:
- Format: `yyyy-MM-dd_original_filename_random.ext`
- Special characters filtered, keeping only letters, digits, Chinese characters, underscores, hyphens
- Filename length limited to 100 characters

**File Display**:
- Image files: Displayed as Markdown image `![filename](URL)`
- Other files: Displayed as attachment `[📎 filename.ext](URL)`

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
- **Secure Sharing** - Share token used only on first access, then via HttpOnly Cookie session (token not exposed in URL)
- **XSS Protection** - Shared content sanitized with DOMPurify
- **Rate Limiting** - Auth: 5 req/min, Shared: 30 req/min, Notes: 60 req/sec
- **CORS** - Strict cross-origin policy configuration
- **Soft Delete** - Notes are soft-deleted and can be recovered
- **Request Cancellation** - Auto-cancel pending requests on route change to prevent race conditions
- **Search Injection Prevention** - LIKE query special character escaping
- **File Type Validation** - File extension and Content-Type validated before upload
- **Filename Security** - Upload filenames sanitized to prevent path traversal attacks

## Performance Optimization

- **Editor Preloading** - Preload Vditor editor on note card hover
- **N+1 Query Optimization** - Use bulk update instead of loop update
- **Pagination** - Notes list supports paginated loading
- **On-demand Note Loading** - Load notes only when expanding folders in sidebar
- **Request Deduplication** - Note auto-save with debounce
- **Stale Response Detection** - Ignore outdated responses to prevent data inconsistency
- **Singleton State Management** - useSidebar composable uses singleton pattern for consistent state across components

## FAQ

**Q: Editor is blank, what should I do?**
A: Ensure the backend API is running. Check browser console for errors.

**Q: Drag and drop categories not working?**
A: Make sure your browser supports HTML5 Drag and Drop API.

**Q: File upload fails with "unsupported file type"?**
A: Check if the file extension is in the supported list. See "Supported File Types" above.

**Q: Large file upload fails?**
A: Ensure the file is under 200MB. Also check if Nginx or other reverse proxy is configured with `client_max_body_size`.

**Q: Notes not saving?**
A: Check network connection and backend API accessibility. You can also check the browser's network tab.

**Q: How to configure OSS for file uploads?**
A: Create an OSS Bucket on Aliyun and configure AccessKey and Bucket information in environment variables or `appsettings.json`.

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

MIT License
