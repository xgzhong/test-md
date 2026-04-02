# Markdown 笔记

[English](./README.en.md)

[![Vue](https://img.shields.io/badge/Vue-3.3.11-green)](https://vuejs.org)
[![TypeScript](https://img.shields.io/badge/TypeScript-6.0-blue)](https://www.typescriptlang.org)
[![Vite](https://img.shields.io/badge/Vite-5.0-brightgreen)](https://vitejs.dev)
[![Element Plus](https://img.shields.io/badge/Element%20Plus-2.4.4-409eff)](https://element-plus.org)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-10.0-512bd4)](https://dotnet.microsoft.com)
[![MySQL](https://img.shields.io/badge/MySQL-5.7+-orange)](https://www.mysql.com)
[![MIT](https://img.shields.io/badge/License-MIT-yellow)](LICENSE)

一个基于 Web 的 Markdown 笔记应用，支持笔记分类、历史版本管理和分享功能。

## 功能特性

### 用户认证
- **注册/登录** - JWT 令牌认证
- **安全存储** - Token 存储在 HttpOnly Cookie 中，防止 XSS 攻击

### 笔记管理
- **Markdown 编辑器** - 使用 Vditor 实现所见即所得编辑
- **版本历史** - 手动保存版本，支持版本回溯和删除
- **分享功能** - 生成分享链接，HTML 内容经 DOMPurify 净化防 XSS
- **工作日志** - 自动生成当月工作日志模板
- **分页浏览** - 笔记列表支持分页加载，避免大数据量卡顿
- **图片上传** - 粘贴外部图片链接时自动上传到 OSS
- **文件上传** - 支持上传多种文件类型，单文件最大 200MB
- **宽屏模式** - 编辑器支持宽屏模式切换，适合大屏显示

### 分类管理
- **树形结构** - 支持多级分类（层级结构）
- **拖拽排序** - 同级分类拖拽调整顺序
- **父子关系** - 拖拽改变分类归属（视觉指示线引导）
- **分类操作** - 置顶、编辑名称、删除分类

### 用户界面
- **侧边栏** - 可折叠、可拖拽调整宽度
- **面包屑导航** - 编辑页显示所属分类路径
- **分类详情页** - 查看分类下的子分类和笔记列表

## 技术栈

### 前端
- **框架**: Vue 3 + Composition API + TypeScript
- **构建**: Vite 5
- **UI**: Element Plus
- **状态管理**: Pinia
- **路由**: Vue Router 4
- **Markdown 编辑器**: Vditor
- **HTTP 客户端**: Axios
- **XSS 防护**: DOMPurify

### 后端
- **框架**: ASP.NET Core 10.0
- **ORM**: Entity Framework Core
- **数据库**: MySQL
- **认证**: JWT + BCrypt
- **ID 生成**: Yitter.IdGenerator (雪花ID)
- **速率限制**: 内置限流中间件
- **日志**: Serilog

## 项目结构

```
test-md/
├── client/                         # 前端项目
│   ├── src/
│   │   ├── api/                   # API 请求封装 (Axios)
│   │   ├── components/            # 可复用组件
│   │   │   ├── Sidebar.vue        # 侧边栏组件
│   │   │   └── FolderTreeItem.vue # 分类树组件
│   │   ├── composables/           # 组合式函数
│   │   │   ├── useSidebar.ts      # 侧边栏逻辑
│   │   │   └── useCommon.ts       # 通用工具函数
│   │   ├── stores/                # Pinia 状态管理
│   │   │   └── auth.ts            # 认证状态
│   │   ├── views/                 # 页面组件
│   │   │   ├── Login.vue          # 登录页面
│   │   │   ├── Register.vue      # 注册页面
│   │   │   ├── Home.vue           # 笔记列表主页
│   │   │   ├── NoteEditorVditor.vue # Markdown 编辑器
│   │   │   ├── FolderDetail.vue   # 分类详情页
│   │   │   ├── About.vue          # 关于页面
│   │   │   └── Shared.vue         # 分享笔记页面
│   │   ├── router/                # 路由配置
│   │   ├── App.vue                # 根组件
│   │   ├── main.js                # 入口文件
│   │   └── style.css              # 全局样式
│   ├── index.html
│   ├── package.json
│   ├── tsconfig.json              # TypeScript 配置
│   └── vite.config.ts            # Vite 配置
│
├── server-dotnet/                 # 后端项目 (.NET 10.0)
│   ├── Common/                    # 公共模块
│   │   ├── Paging/                # 分页组件
│   │   └── Result/                # 统一响应封装
│   ├── Controllers/               # API 控制器
│   │   ├── AuthController.cs      # 认证接口
│   │   ├── NotesController.cs     # 笔记接口
│   │   ├── FoldersController.cs   # 分类接口
│   │   ├── SharedController.cs   # 分享接口
│   │   └── OssController.cs      # OSS 上传接口
│   ├── Models/                    # 数据模型
│   │   ├── User.cs
│   │   ├── Note.cs
│   │   ├── Folder.cs
│   │   └── NoteVersion.cs
│   ├── Data/                      # 数据库上下文
│   │   └── AppDbContext.cs
│   ├── DTOs/                      # 数据传输对象
│   │   └── *.cs
│   ├── Middleware/                # 中间件
│   │   └── GlobalExceptionHandler.cs
│   ├── Converters/                # JSON 转换器
│   │   ├── LongToStringConverter.cs
│   │   └── DateTimeOffsetConverter.cs
│   ├── Constants/                  # 常量配置
│   │   └── AppConstants.cs
│   ├── appsettings.json           # 配置文件
│   └── Program.cs                 # 入口文件
│
├── README.md                       # 中文版 README
├── README.en.md                    # 英文版 README
└── LICENSE                        # MIT 许可证
```

## 快速开始

### 环境要求

| 依赖 | 版本要求 |
|------|----------|
| Node.js | 18+ |
| pnpm | 9+ |
| .NET SDK | 10.0+ |
| MySQL | 5.7+ |

### 安装步骤

```bash
# 克隆项目
git clone https://github.com/xgzhong/test-md.git
cd test-md

# 安装前端依赖
cd client
pnpm install

# 安装后端依赖
cd ../server-dotnet
dotnet restore
```

### 配置

1. **创建数据库** (确保 MySQL 服务运行):

```sql
CREATE DATABASE markdown_notes CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

2. **设置环境变量**:

| 变量名 | 说明 | 示例 |
|--------|------|------|
| `DB_CONNECTION_STRING` | 数据库连接字符串 | `Server=localhost;Database=markdown_notes;User=root;Password=xxx;` |
| `JWT_SECRET` | JWT 密钥 (生产环境至少 32 字符) | `your-super-secret-key-at-least-32-chars` |
| `CORS_ALLOWED_ORIGINS` | 允许的前端地址 (多个逗号分隔) | `http://localhost:5173,https://yourdomain.com` |

> **Linux/macOS** 使用 `export` 赋值，**Windows** 可使用 `set` 或在 `.env` 文件中配置。

3. **OSS 配置** (用于文件上传，需要阿里云 OSS):

| 变量名 | 说明 | 示例 |
|--------|------|------|
| `OSS:AccessKeyId` | 阿里云 AccessKey ID | `LTAI5txxx` |
| `OSS:AccessKeySecret` | 阿里云 AccessKey Secret | `xxx` |
| `OSS:Endpoint` | OSS  Endpoint | `https://oss-cn-shanghai.aliyuncs.com` |
| `OSS:BucketName` | OSS Bucket 名称 | `note-md` |

4. **后端配置** (`server-dotnet/appsettings.json`):

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

> **注意**: 生产环境务必通过环境变量配置敏感信息，切勿使用硬编码默认值。

### 启动服务

```bash
# 终端 1: 启动后端
cd server-dotnet
dotnet run
# 后端地址: http://localhost:5000

# 终端 2: 启动前端
cd client
pnpm dev
# 前端地址: http://localhost:5173
```

> 首次运行时会自动创建数据库表结构（EF Core Code-First）。

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

## 部署说明

### Docker 部署（推荐）

```bash
# 构建并启动所有服务
docker-compose up -d
```

### Nginx 反向代理配置

```nginx
server {
    listen 80;
    server_name yourdomain.com;

    # 前端静态文件
    location / {
        root /var/www/test-md/client/dist;
        try_files $uri $uri/ /index.html;
    }

    # API 反向代理
    location /api {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

## API 接口

### 认证接口

| 方法 | 路径 | 描述 |
|------|------|------|
| POST | /api/auth/register | 用户注册 |
| POST | /api/auth/login | 用户登录 |
| POST | /api/auth/logout | 用户登出 |
| GET | /api/auth/me | 获取当前用户信息 |

### 笔记接口

| 方法 | 路径 | 描述 |
|------|------|------|
| GET | /api/notes | 获取笔记列表 (支持 folderId, search, page, pageSize) |
| GET | /api/notes/page | 分页获取笔记 (返回 items + metaData) |
| GET | /api/notes/:id | 获取笔记详情 |
| POST | /api/notes | 创建笔记 |
| PUT | /api/notes/:id | 更新笔记 |
| DELETE | /api/notes/:id | 删除笔记 (软删除) |
| POST | /api/notes/:id/share | 分享笔记 |
| POST | /api/notes/:id/unshare | 取消分享 |
| GET | /api/notes/:id/versions | 获取版本历史 |
| POST | /api/notes/:id/restore/:versionId | 恢复版本 |
| DELETE | /api/notes/:id/versions/:versionId | 删除版本 |

**分页响应格式**:

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

### 分类接口

| 方法 | 路径 | 描述 |
|------|------|------|
| GET | /api/folders | 获取分类列表 (树形结构) |
| POST | /api/folders | 创建分类 |
| PUT | /api/folders/:id | 更新分类 (名称、父子关系) |
| DELETE | /api/folders/:id | 删除分类 |
| PUT | /api/folders/reorder | 分类排序 |
| PUT | /api/folders/:id/pin | 置顶/取消置顶分类 |

### 分享接口

| 方法 | 路径 | 描述 |
|------|------|------|
| GET | /api/shared/:token | 通过分享链接查看笔记 |

### OSS 上传

| 方法 | 路径 | 描述 |
|------|------|------|
| POST | /api/oss/presigned-url | 获取预签名 URL，浏览器直传 OSS |
| POST | /api/oss/link-to-img | 下载外部图片并上传到 OSS |
| POST | /api/oss/upload | 服务器中转上传（备用方案） |

**预签名 URL 流程**：
1. 客户端请求服务器获取预签名 URL
2. 服务器生成带 30 分钟过期时间的签名 URL
3. 客户端直接通过预签名 URL 上传到 OSS
4. 传输文件不需要服务器带宽

**支持的文件类型**：

| 类型 | 扩展名 |
|------|--------|
| 图片 | .jpg, .jpeg, .png, .gif, .webp, .bmp, .svg, .tif, .tiff |
| 文档 | .pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx |
| 文本 | .txt, .md, .sql, .bak, .cs, .js, .vue, .html, .htm, .css, .sass |
| 压缩 | .zip, .rar |

**上传文件命名**：
- 格式：`yyyy-MM-dd_原文件名_random.ext`
- 特殊字符自动过滤，仅保留字母、数字、中文、下划线、连字符
- 文件名长度限制 100 字符

**文件展示**：
- 图片文件：直接以 Markdown 图片格式 `![文件名](URL)` 展示
- 其他文件：统一以附件格式 `[📎 文件名.ext](URL)` 展示

## 数据库设计

### 表结构

所有表使用雪花ID作为主键（`BIGINT`），包含审计字段：

| 表名 | 说明 |
|------|------|
| users | 用户表 |
| folders | 分类表 |
| notes | 笔记表 |
| note_versions | 笔记版本表 |

### 字段规范

| 字段类型 | 说明 |
|---------|------|
| Id | 雪花ID (BIGINT)，由 Yitter.IdGenerator 生成 |
| Version | 数据版本号 (BIGINT)，用于乐观并发控制 |
| CreatedAt/UpdatedAt | 时间戳 (DateTimeOffset) |
| CreatedBy/UpdatedBy | 创建人/修改人 ID (BIGINT) |

### JSON 序列化

- `long` 类型 ID 以字符串形式返回 (如 `"id": "785339216482373"`)
- `DateTime` 以 `yyyy-MM-dd HH:mm:ss` 格式返回

## 安全特性

- **JWT Token** - 存储在 HttpOnly Cookie 中，有效防止 XSS 攻击获取 Token
- **密码加密** - 使用 BCrypt 算法加密存储
- **XSS 防护** - 分享内容通过 DOMPurify 进行 HTML 净化
- **速率限制** - 认证接口 5 次/分钟，笔记接口 60 次/秒，防止暴力破解和接口滥用
- **CORS** - 严格配置跨域访问策略
- **软删除** - 笔记删除采用软删除策略，可恢复
- **请求取消** - 路由切换时自动取消 pending 请求，防止竞态条件
- **搜索防注入** - LIKE 查询特殊字符转义
- **文件类型验证** - 上传前验证文件扩展名和 Content-Type
- **文件名安全** - 上传文件名去除特殊字符，防止路径遍历攻击

## 性能优化

- **预加载编辑器** - 鼠标悬停笔记卡片时预加载 Vditor 编辑器
- **N+1 查询优化** - 使用批量更新替代循环更新
- **分页查询** - 笔记列表支持分页加载
- **请求去重** - 笔记自动保存防抖处理
- **响应时效检测** - 忽略过时响应，防止数据错乱

## 常见问题

**Q: 笔记编辑器空白怎么办？**
A: 确保后端 API 正常运行，检查浏览器控制台是否有错误信息。

**Q: 拖拽分类不生效？**
A: 检查浏览器是否支持 HTML5 Drag and Drop API。

**Q: 文件上传失败，提示"不支持的文件类型"？**
A: 检查文件扩展名是否在支持列表中，参见上方「支持的文件类型」。

**Q: 上传大文件失败？**
A: 确保文件小于 200MB，同时检查 Nginx 等反向代理是否配置了 `client_max_body_size`。

**Q: 笔记内容无法保存？**
A: 检查网络连接，确认后端 API 可访问。也可以查看浏览器控制台的网络请求面板。

**Q: 如何配置 OSS 用于文件上传？**
A: 需要在阿里云创建 OSS Bucket，并在环境变量或 `appsettings.json` 中配置 AccessKey 和 Bucket 信息。

## 贡献

欢迎提交 Issue 和 Pull Request！

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

## 许可证

MIT License
