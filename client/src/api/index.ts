import axios from 'axios'
import router from '../router'

// ============== Types ==============
// Note: Backend returns all long/Long? types as strings (via LongToStringConverter)
export interface User {
  id: string
  username: string
  email: string
}

export interface Note {
  id: string
  folderId: string | null
  folderName: string | null
  title: string
  content: string
  isShared: boolean
  shareToken: string | null
  version: string
  createdAt: string
  updatedAt: string
}

export interface Folder {
  id: string
  name: string
  parentId: string
  noteCount: number
  sortOrder: number
  isPinned: boolean
  children?: Folder[]
}

export interface NoteVersion {
  id: string
  noteId: string
  content: string
  version: number
  createdAt: string
}

// API Response types - backend returns unwrapped data via Result<T>.value
export interface NotesResponse {
  notes: Note[]
  totalCount?: number
}

export interface ShareResponse {
  message: string
  shareUrl: string
}

export interface VersionsResponse {
  versions: NoteVersion[]
}

// Request types
export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  username: string
  email: string
  password: string
}

export interface CreateNoteRequest {
  title?: string
  content?: string
  folderId?: string | number | null
}

export interface UpdateNoteRequest {
  title?: string
  content?: string
  folderId?: string | number | null
  saveVersion?: boolean
}

export interface CreateFolderRequest {
  name: string
  parentId?: string | number | null
}

export interface UpdateFolderRequest {
  name?: string
  parentId?: string | number | null
}

export interface ReorderFoldersRequest {
  folderIds: (string | number)[]
}

// ============== Axios Instance ==============
const api = axios.create({
  baseURL: '/api',
  timeout: 10000,
  withCredentials: true
})

const getTokenFromCookie = (): string | null => {
  const cookies = document.cookie.split('; ')
  const tokenCookie = cookies.find(c => c.trim().startsWith('auth_token='))
  if (tokenCookie) {
    return tokenCookie.split('=')[1]
  }
  return null
}

const clearAuthCookie = (): void => {
  document.cookie = 'auth_token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;'
}

// Request interceptor
api.interceptors.request.use(
  config => {
    const token = getTokenFromCookie()
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  error => Promise.reject(error)
)

// Response interceptor
api.interceptors.response.use(
  response => {
    // 统一解包 Result<T> 格式: { value, isSuccess, errors, status, message }
    const res = response.data
    if (res && typeof res === 'object' && 'isSuccess' in res) {
      if (res.isSuccess) {
        // 成功：解包 value 给前端使用
        response.data = res.value
      } else {
        // 失败：抛出错误
        const message = res.message || res.errors?.join(', ') || '请求失败'
        return Promise.reject(new Error(message))
      }
    }
    return response
  },
  error => {
    if (error.response) {
      const { status, data } = error.response
      const message = data?.message || data?.error?.message || '请求失败'

      if (status === 401) {
        clearAuthCookie()
        localStorage.removeItem('user')
        localStorage.removeItem('isLoggedIn')
        router.push('/login')
      } else if (status === 429) {
        return Promise.reject(new Error('请求过于频繁，请稍后再试'))
      }

      return Promise.reject(new Error(message))
    }
    return Promise.reject(error)
  }
)

// ============== API Methods ==============

// Auth API - login/register return { message, token?, user? } directly in value
export const authAPI = {
  login: (data: LoginRequest): Promise<{ message: string; token: string | null; user: User | null }> =>
    api.post('/auth/login', data).then(res => res.data),

  register: (data: RegisterRequest): Promise<{ message: string }> =>
    api.post('/auth/register', data).then(res => res.data),

  logout: (): Promise<void> =>
    api.post('/auth/logout').then(() => undefined),

  getUser: (): Promise<User> =>
    api.get('/auth/me').then(res => res.data)
}

// Notes API - returns Note directly, not wrapped
export const notesAPI = {
  getNotes: (params?: Record<string, string | number | undefined>): Promise<NotesResponse> =>
    api.get('/notes', { params }).then(res => res.data),

  getNote: (id: string): Promise<Note> =>
    api.get(`/notes/${id}`).then(res => res.data),

  createNote: (data: CreateNoteRequest): Promise<Note> =>
    api.post('/notes', data).then(res => res.data),

  updateNote: (id: string, data: UpdateNoteRequest): Promise<Note> =>
    api.put(`/notes/${id}`, data).then(res => res.data),

  deleteNote: (id: string): Promise<void> =>
    api.delete(`/notes/${id}`).then(() => undefined),

  shareNote: (id: string): Promise<ShareResponse> =>
    api.post(`/notes/${id}/share`).then(res => res.data),

  unshareNote: (id: string): Promise<void> =>
    api.post(`/notes/${id}/unshare`).then(() => undefined),

  getVersions: (id: string): Promise<VersionsResponse> =>
    api.get(`/notes/${id}/versions`).then(res => res.data),

  restoreVersion: (id: string, versionId: string): Promise<Note> =>
    api.post(`/notes/${id}/restore/${versionId}`).then(res => res.data),

  deleteVersion: (id: string, versionId: string): Promise<void> =>
    api.delete(`/notes/${id}/versions/${versionId}`).then(() => undefined)
}

// Folders API - returns Folder directly
export const foldersAPI = {
  getFolders: (): Promise<{ folders: Folder[]; uncategorizedCount: number }> =>
    api.get('/folders').then(res => res.data),

  createFolder: (data: CreateFolderRequest): Promise<Folder> =>
    api.post('/folders', data).then(res => res.data),

  updateFolder: (id: string, data: UpdateFolderRequest): Promise<Folder> =>
    api.put(`/folders/${id}`, data).then(res => res.data),

  deleteFolder: (id: string): Promise<void> =>
    api.delete(`/folders/${id}`).then(() => undefined),

  reorderFolders: (data: ReorderFoldersRequest): Promise<void> =>
    api.put('/folders/reorder', data.folderIds).then(() => undefined),

  pinFolder: (id: string): Promise<Folder> =>
    api.put(`/folders/${id}/pin`).then(res => res.data)
}

// Shared API
export const sharedAPI = {
  getSharedNote: (token: string): Promise<{ id: number; title: string; content: string; author: string; createdAt: string; updatedAt: string }> =>
    api.get(`/shared/${token}`).then(res => res.data)
}

export default api
