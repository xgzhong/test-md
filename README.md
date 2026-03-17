# Markdown Notes App

一个基于 Web 的 Markdown 笔记应用，支持笔记分类、历史版本管理和分享功能。

## 功能特性

- **用户认证** - 注册、登录、JWT 令牌认证
- **笔记管理** - 创建、编辑、删除 Markdown 笔记
- **分类管理** - 文件夹分类整理
- **历史版本** - 自动保存笔记历史，支持版本回溯
- **笔记分享** - 生成分享链接他人查看
- **Markdown 渲染** - 实时预览 Markdown 内容

## 技术栈

### 前端

- Vue 3 + Composition API
- Vite
- Element Plus
- Axios
- Vue Router
- marked

### 后端

- ASP.NET Core 10.0
- Entity Framework Core
- SQLite
- JWT 认证

## 项目结构

```
test-md/
├── client/                 # 前端项目 (Vue 3)
│   ├── src/
│   │   ├── api/           # API 请求
│   │   ├── views/         # 页面组件
│   │   ├── router/        # 路由配置
│   │   └── main.js        # 入口文件
│   └── package.json
│
├── server-dotnet/          # 后端项目 (.NET 10.0)
│   ├── Controllers/       # API 控制器
│   ├── Models/            # 数据模型
│   ├── Data/              # 数据库上下文
│   ├── DTOs/              # 数据传输对象
│   └── Program.cs         # 入口文件
│
└── pnpm-workspace.yaml    # pnpm 工作区配置
```

## 快速开始

### 前置要求

- Node.js 18+
- pnpm 9+
- .NET SDK 10.0+

### 安装

```bash
pnpm install
```

### 启动开发服务器

```bash
# 启动后端 (终端 1)
cd server-dotnet
dotnet run

# 启动前端 (终端 2)
pnpm dev
```

- 前端访问: <http://localhost:5173>
- 后端 API: <http://localhost:5000>

### 构建生产版本

```bash
pnpm build
```

## API 接口

### 认证

| 方法 | 路径 | 描述 |
|------|------|------|
| POST | /api/auth/register | 用户注册 |
| POST | /api/auth/login | 用户登录 |
| GET | /api/auth/me | 获取当前用户 |

### 笔记

| 方法 | 路径 | 描述 |
|------|------|------|
| GET | /api/notes | 获取笔记列表 |
| GET | /api/notes/:id | 获取笔记详情 |
| POST | /api/notes | 创建笔记 |
| PUT | /api/notes/:id | 更新笔记 |
| DELETE | /api/notes/:id | 删除笔记 |
| POST | /api/notes/:id/share | 分享笔记 |
| POST | /api/notes/:id/unshare | 取消分享 |
| GET | /api/notes/:id/versions | 获取历史版本 |
| POST | /api/notes/:id/restore/:versionId | 恢复版本 |

### 分类

| 方法 | 路径 | 描述 |
|------|------|------|
| GET | /api/folders | 获取分类列表 |
| POST | /api/folders | 创建分类 |
| PUT | /api/folders/:id | 更新分类 |
| DELETE | /api/folders/:id | 删除分类 |

### 分享

| 方法 | 路径 | 描述 |
|------|------|------|
| GET | /api/shared/:token | 通过分享链接查看笔记 |

## 配置

### 后端配置 (server-dotnet/appsettings.json)

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

### 前端代理配置 (client/vite.config.js)

前端默认代理到 `http://localhost:5000`，如需修改请编辑 `vite.config.js`。

## 许可证

MIT License

---

## Markdown Notes App

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
