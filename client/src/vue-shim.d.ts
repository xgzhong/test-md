declare module '*.vue' {
  import type { DefineComponent } from 'vue'
  const component: DefineComponent<{}, {}, any>
  export default component
}

declare module 'dompurify' {
  const DOMPurify: any
  export default DOMPurify
}

// API module declaration
declare module '../api' {
  export interface ApiResponse<T = any> {
    data?: T
    error?: string
  }

  export interface LoginRequest {
    email: string
    password: string
  }

  export interface RegisterRequest {
    username: string
    email: string
    password: string
  }

  export const authAPI: {
    login: (data: LoginRequest) => Promise<any>
    register: (data: RegisterRequest) => Promise<any>
    logout: () => Promise<void>
    getUser: () => Promise<any>
  }

  export const notesAPI: {
    getNotes: (params?: Record<string, any>) => Promise<{ notes: any[] }>
    getNote: (id: number) => Promise<{ note: any }>
    createNote: (data: any) => Promise<{ note: any }>
    updateNote: (id: number, data: any) => Promise<{ note: any }>
    deleteNote: (id: number) => Promise<void>
    shareNote: (id: number) => Promise<any>
    unshareNote: (id: number) => Promise<void>
    getVersions: (id: number) => Promise<any>
    restoreVersion: (id: number, versionId: number) => Promise<any>
    deleteVersion: (id: number, versionId: number) => Promise<void>
  }

  export const foldersAPI: {
    getFolders: () => Promise<{ folders: any[]; uncategorizedCount?: number }>
    createFolder: (data: any) => Promise<{ folder: any }>
    updateFolder: (id: number, data: any) => Promise<{ folder: any }>
    deleteFolder: (id: number) => Promise<void>
    reorderFolders: (data: any) => Promise<void>
    pinFolder: (id: number) => Promise<void>
  }

  export const sharedAPI: {
    getSharedNote: (token: string) => Promise<any>
  }
}

// Router module declaration
declare module '../router/index' {
  import type { Router } from 'vue-router'
  const router: Router
  export default router
}