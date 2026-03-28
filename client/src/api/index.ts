import axios from 'axios'
import router from '../router'

// ============== Types ==============
export interface User {
  id: number
  username: string
  email: string
}

export interface Note {
  id: number
  folderId: number | null
  folderName: string | null
  title: string
  content: string
  isShared: boolean
  shareToken: string | null
  version: number
  createdAt: string
  updatedAt: string
}

export interface Folder {
  id: number
  name: string
  parentId: number
  noteCount: number
  sortOrder: number
  isPinned: boolean
  children?: Folder[]
}

// API Response types
export interface NotesResponse {
  notes: Note[]
  totalCount?: number
}

export interface NoteResponse {
  note: Note
}

export interface FoldersResponse {
  folders: Folder[]
  uncategorizedCount?: number
}

export interface FolderResponse {
  folder: Folder
}

export interface AuthResponse {
  message: string
  token: string | null
  user: User | null
}

export interface ShareResponse {
  message: string
  shareUrl: string
}

export interface VersionsResponse {
  versions: NoteVersion[]
}

export interface NoteVersion {
  id: number
  noteId: number
  content: string
  version: number
  createdAt: string
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
  folderId?: number | null
}

export interface UpdateNoteRequest {
  title?: string
  content?: string
  folderId?: number | string | null
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
  response => response,
  error => {
    if (error.response) {
      const { status, data } = error.response
      const message = data?.error || '请求失败'

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

// Auth API
export const authAPI = {
  login: (data: LoginRequest): Promise<AuthResponse> =>
    api.post<AuthResponse>('/auth/login', data).then(res => res.data),

  register: (data: RegisterRequest): Promise<{ message: string }> =>
    api.post('/auth/register', data).then(res => res.data),

  logout: (): Promise<void> =>
    api.post('/auth/logout').then(() => undefined),

  getUser: (): Promise<User> =>
    api.get<User>('/auth/me').then(res => res.data)
}

// Notes API
export const notesAPI = {
  getNotes: (params?: Record<string, string | number | undefined>): Promise<NotesResponse> =>
    api.get<NotesResponse>('/notes', { params }).then(res => res.data),

  getNote: (id: number): Promise<NoteResponse> =>
    api.get<NoteResponse>(`/notes/${id}`).then(res => res.data),

  createNote: (data: CreateNoteRequest): Promise<NoteResponse> =>
    api.post<NoteResponse>('/notes', data).then(res => res.data),

  updateNote: (id: number, data: UpdateNoteRequest): Promise<NoteResponse> =>
    api.put<NoteResponse>(`/notes/${id}`, data).then(res => res.data),

  deleteNote: (id: number): Promise<void> =>
    api.delete(`/notes/${id}`).then(() => undefined),

  shareNote: (id: number): Promise<ShareResponse> =>
    api.post<ShareResponse>(`/notes/${id}/share`).then(res => res.data),

  unshareNote: (id: number): Promise<void> =>
    api.post(`/notes/${id}/unshare`).then(() => undefined),

  getVersions: (id: number): Promise<VersionsResponse> =>
    api.get<VersionsResponse>(`/notes/${id}/versions`).then(res => res.data),

  restoreVersion: (id: number, versionId: number): Promise<NoteResponse> =>
    api.post<NoteResponse>(`/notes/${id}/restore/${versionId}`).then(res => res.data),

  deleteVersion: (id: number, versionId: number): Promise<void> =>
    api.delete(`/notes/${id}/versions/${versionId}`).then(() => undefined)
}

// Folders API
export const foldersAPI = {
  getFolders: (): Promise<FoldersResponse> =>
    api.get<FoldersResponse>('/folders').then(res => res.data),

  createFolder: (data: CreateFolderRequest): Promise<FolderResponse> =>
    api.post<FolderResponse>('/folders', data).then(res => res.data),

  updateFolder: (id: number, data: UpdateFolderRequest): Promise<FolderResponse> =>
    api.put<FolderResponse>(`/folders/${id}`, data).then(res => res.data),

  deleteFolder: (id: number): Promise<void> =>
    api.delete(`/folders/${id}`).then(() => undefined),

  reorderFolders: (data: ReorderFoldersRequest): Promise<void> =>
    api.put('/folders/reorder', data.folderIds).then(() => undefined),

  pinFolder: (id: number): Promise<void> =>
    api.put(`/folders/${id}/pin`).then(() => undefined)
}

// Shared API
export const sharedAPI = {
  getSharedNote: (token: string): Promise<{ note: Note & { author: string } }> =>
    api.get(`/shared/${token}`).then(res => res.data)
}

export default api
