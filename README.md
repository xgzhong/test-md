[English](./README-en.md)

# Markdown Notes App

一个基于 Web 的 Markdown 笔记应用，支持笔记分类、历史版本管理和分享功能。

## 功能特性

- **用户认证** - 注册、登录，JWT 令牌认证
- **笔记管理** - 创建、编辑、删除 Markdown 笔记
- **分类管理** - 树形文件夹分类整理
  - 支持多级分类（层级结构）
  - 拖拽调整分类顺序
  - 拖拽调整父子关系（通过视觉指示线）
  - 置顶、编辑名称、删除分类
- **历史版本** - 手动保存版本历史，支持版本回溯和删除
- **笔记分享** - 生成分享链接他人查看
- **Markdown 编辑器** - 使用 Vditor 实现所见即所得编辑
- **侧边栏** - 支持折叠和展开，可复用组件
- **工作日志** - 自动生成当月工作日志模板
- **雪花ID** - 使用 Yitter.IdGenerator 生成分布式 ID
- **数据版本** - 每条记录包含 Version 字段用于版本控制

## 技术栈

### 前端

- Vue 3 + Composition API
- Vite
- Element Plus
- Axios
- Vue Router
- Vditor (Markdown 编辑器)

### 后端

- ASP.NET Core 9.0
- Entity Framework Core
- MySQL
- JWT 认证
- Yitter.IdGenerator (雪花ID)
- BCrypt.Net-Next (密码加密)

## 项目结构

```
test-md/
├── client/                      # 前端项目 (Vue 3)
│   ├── src/
│   │   ├── api/                 # API 请求封装
│   │   ├── components/           # 可复用组件
│   │   │   ├── Sidebar.vue       # 侧边栏组件
│   │   │   └── FolderTreeItem.vue # 分类树组件
│   │   ├── views/               # 页面组件
│   │   │   ├── Login.vue        # 登录页面
│   │   │   ├── Register.vue     # 注册页面
│   │   │   ├── Home.vue         # 笔记列表主页
│   │   │   ├── NoteEditorVditor.vue # Markdown 编辑器
│   │   │   └── Shared.vue       # 分享笔记页面
│   │   ├── router/              # 路由配置
│   │   ├── main.js              # 入口文件
│   │   ├── App.vue              # 根组件
│   │   └── style.css            # 全局样式
│   ├── index.html
│   ├── package.json
│   └── vite.config.js           # Vite 配置
│
├── server-dotnet/               # 后端项目 (.NET 9.0)
│   ├── Controllers/             # API 控制器
│   │   ├── AuthController.cs    # 认证接口
│   │   ├── NotesController.cs   # 笔记接口
│   │   ├── FoldersController.cs # 分类接口
│   │   └── SharedController.cs  # 分享接口
│   ├── Models/                   # 数据模型
│   │   ├── User.cs
│   │   ├── Note.cs
│   │   ├── Folder.cs
│   │   └── NoteVersion.cs
│   ├── Data/                     # 数据库上下文
│   │   └── AppDbContext.cs
│   ├── DTOs/                    # 数据传输对象
│   │   └── AuthDtos.cs
│   ├── Converters/              # JSON 转换器
│   │   ├── LongToStringConverter.cs
│   │   └── DateTimeOffsetConverter.cs
│   ├── appsettings.json         # 配置文件
│   └── Program.cs               # 入口文件
│
├── README.md                     # 中文版 README
├── README-en.md                 # 英文版 README
└── LICENSE                      # MIT 许可证
```

## 快速开始

### 前置要求

- Node.js 18+
- pnpm 9+
- .NET SDK 9.0+
- MySQL 5.7+

### 安装

```bash
# 安装前端依赖
cd client
pnpm install

# 安装后端依赖
cd ../server-dotnet
dotnet restore
```

### 配置

1. **数据库配置** (确保 MySQL 服务运行):

```sql
CREATE DATABASE markdown_notes CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

2. **环境变量配置** - 复制 `.env.example` 为 `.env` 并配置：

```bash
cd server-dotnet
cp .env.example .env
```

主要配置项：
- `DB_CONNECTION_STRING` - 数据库连接字符串
- `JWT_SECRET` - JWT 密钥（生产环境请使用足够长度的随机字符串）
- `ASPNETCORE_ENVIRONMENT` - 运行环境 (Development/Production)

3. **后端配置** (`server-dotnet/appsettings.json`)：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=markdown_notes;User=root;Password=your_password;"
  },
  "Jwt": {
    "Secret": "your-secret-key-change-in-production"
  },
  "SnowflakeId": {
    "BaseTime": "2026-01-01 00:00:00"
  }
}
```

### 启动开发服务器

```bash
# 启动后端 (终端 1)
cd server-dotnet
dotnet run
# 后端运行在 http://localhost:5000

# 启动前端 (终端 2)
cd client
pnpm dev
# 前端运行在 http://localhost:5173
```

### 初始化数据库

首次运行后，端点 `/api/init` 可用于初始化数据库表结构（如果使用 EF Core migrations）。

### 构建生产版本

```bash
# 前端构建
cd client
pnpm build
# 构建产物在 client/dist 目录

# 后端发布
cd server-dotnet
dotnet publish -c Release
```

## API 接口

### 认证

| 方法 | 路径 | 描述 |
|------|------|------|
| POST | /api/auth/register | 用户注册 |
| POST | /api/auth/login | 用户登录 |
| GET | /api/auth/me | 获取当前用户信息 |

### 笔记

| 方法 | 路径 | 描述 |
|------|------|------|
| GET | /api/notes | 获取笔记列表 (支持 folderId, search 参数) |
| GET | /api/notes/:id | 获取笔记详情 |
| POST | /api/notes | 创建笔记 |
| PUT | /api/notes/:id | 更新笔记 |
| DELETE | /api/notes/:id | 删除笔记 (逻辑删除) |
| POST | /api/notes/:id/share | 分享笔记 |
| POST | /api/notes/:id/unshare | 取消分享 |
| GET | /api/notes/:id/versions | 获取版本历史 |
| POST | /api/notes/:id/restore/:versionId | 恢复版本 |
| DELETE | /api/notes/:id/versions/:versionId | 删除版本 |

### 分类

| 方法 | 路径 | 描述 |
|------|------|------|
| GET | /api/folders | 获取分类列表 (树形结构) |
| POST | /api/folders | 创建分类 |
| PUT | /api/folders/:id | 更新分类 (名称、父子关系) |
| DELETE | /api/folders/:id | 删除分类 |
| PUT | /api/folders/reorder | 分类排序 |
| PUT | /api/folders/:id/pin | 置顶/取消置顶分类 |

### 分享

| 方法 | 路径 | 描述 |
|------|------|------|
| GET | /api/shared/:token | 通过分享链接查看笔记 |

## 数据库设计

### 表结构

所有表使用雪花ID作为主键（`BIGINT`），包含审计字段：

- **users** - 用户表
- **folders** - 分类表
- **notes** - 笔记表
- **note_versions** - 笔记版本表

### 字段规范

| 字段类型 | 说明 |
|---------|------|
| Id | 雪花ID (BIGINT)，使用 Yitter.IdGenerator 生成 |
| Version | 数据版本号 (BIGINT)，雪花ID格式 |
| CreatedAt/UpdatedAt | 时间戳 (DateTimeOffset) |
| CreatedBy/UpdatedBy | 创建人/修改人 ID (BIGINT) |

### JSON 序列化

API 返回格式：
- `long` 类型 ID 以字符串形式返回（如 `"id": "785339216482373"`）
- `DateTime` 以 `yyyy-MM-dd HH:mm:ss` 格式返回

## 截图

> 截图将在后续版本中添加

主要界面：
- **笔记列表** - 展示所有笔记，支持搜索和分类筛选
- **分类管理** - 树形结构展示，支持拖拽排序和层级调整
- **Markdown 编辑器** - Vditor 所见即所得编辑
- **笔记分享** - 一键生成分享链接

## 贡献

欢迎提交 Issue 和 Pull Request！

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

## 许可证

MIT License
