# Markdown Notes App

一个基于 Web 的 Markdown 笔记应用，支持笔记分类、历史版本管理和分享功能。

A web-based Markdown note-taking application with category management, version history, and sharing features.

---

## 功能特性 / Features

- **用户认证** - 注册、登录、JWT 令牌认证
- **User Authentication** - Register, login, JWT token authentication

- **笔记管理** - 创建、编辑、删除 Markdown 笔记
- **Note Management** - Create, edit, delete Markdown notes

- **分类管理** - 文件夹分类整理
- **Category Management** - Organize notes into folders

- **历史版本** - 自动保存笔记历史，支持版本回溯
- **Version History** - Auto-save note history with restore capability

- **笔记分享** - 生成分享链接他人查看
- **Note Sharing** - Generate shareable links for others to view

- **Markdown 渲染** - 实时预览 Markdown 内容
- **Markdown Rendering** - Real-time Markdown preview

---

## 技术栈 / Tech Stack

### 前端 / Frontend

- Vue 3 + Composition API
- Vite
- Element Plus
- Axios
- Vue Router
- marked

### 后端 / Backend

- ASP.NET Core 10.0
- Entity Framework Core
- SQLite
- JWT Authentication

---

## 项目结构 / Project Structure

```
test-md/
├── client/                 # 前端项目 (Vue 3) / Frontend (Vue 3)
│   ├── src/
│   │   ├── api/           # API 请求 / API requests
│   │   ├── views/         # 页面组件 / Page components
│   │   ├── router/        # 路由配置 / Router config
│   │   └── main.js        # 入口文件 / Entry point
│   └── package.json
│
├── server-dotnet/          # 后端项目 (.NET 10.0) / Backend (.NET 10.0)
│   ├── Controllers/       # API 控制器 / API controllers
│   ├── Models/            # 数据模型 / Data models
│   ├── Data/              # 数据库上下文 / DB context
│   ├── DTOs/              # 数据传输对象 / DTOs
│   └── Program.cs         # 入口文件 / Entry point
│
└── pnpm-workspace.yaml    # pnpm 工作区配置 / pnpm workspace config
```

---

## 快速开始 / Quick Start

### 前置要求 / Prerequisites

- Node.js 18+
- pnpm 9+
- .NET SDK 10.0+

### 安装 / Installation

```bash
pnpm install
```

### 启动开发服务器 / Start Development Server

```bash
# 启动后端 (终端 1) / Start backend (Terminal 1)
cd server-dotnet
dotnet run

# 启动前端 (终端 2) / Start frontend (Terminal 2)
pnpm dev
```

- 前端访问 / Frontend: <http://localhost:5173>
- 后端 API / Backend API: <http://localhost:5000>

### 构建生产版本 / Build for Production

```bash
pnpm build
```

---

## API 接口 / API Endpoints

### 认证 / Authentication

| 方法 / Method | 路径 / Path | 描述 / Description |
|---------------|-------------|-------------------|
| POST | /api/auth/register | 用户注册 / User registration |
| POST | /api/auth/login | 用户登录 / User login |
| GET | /api/auth/me | 获取当前用户 / Get current user |

### 笔记 / Notes

| 方法 / Method | 路径 / Path | 描述 / Description |
|---------------|-------------|-------------------|
| GET | /api/notes | 获取笔记列表 / Get note list |
| GET | /api/notes/:id | 获取笔记详情 / Get note details |
| POST | /api/notes | 创建笔记 / Create note |
| PUT | /api/notes/:id | 更新笔记 / Update note |
| DELETE | /api/notes/:id | 删除笔记 / Delete note |
| POST | /api/notes/:id/share | 分享笔记 / Share note |
| POST | /api/notes/:id/unshare | 取消分享 / Unshare note |
| GET | /api/notes/:id/versions | 获取历史版本 / Get version history |
| POST | /api/notes/:id/restore/:versionId | 恢复版本 / Restore version |

### 分类 / Folders

| 方法 / Method | 路径 / Path | 描述 / Description |
|---------------|-------------|-------------------|
| GET | /api/folders | 获取分类列表 / Get folder list |
| POST | /api/folders | 创建分类 / Create folder |
| PUT | /api/folders/:id | 更新分类 / Update folder |
| DELETE | /api/folders/:id | 删除分类 / Delete folder |

### 分享 / Sharing

| 方法 / Method | 路径 / Path | 描述 / Description |
|---------------|-------------|-------------------|
| GET | /api/shared/:token | 通过分享链接查看笔记 / View note via share link |

---

## 配置 / Configuration

### 后端配置 / Backend Configuration (`server-dotnet/appsettings.json`)

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

### 前端代理配置 / Frontend Proxy Config (`client/vite.config.js`)

前端默认代理到 `http://localhost:5000`，如需修改请编辑 `vite.config.js`。

The frontend proxies to `http://localhost:5000` by default. Edit `vite.config.js` to change it.

---

## 许可证 / License

MIT License
