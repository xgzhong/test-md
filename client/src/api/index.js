import axios from 'axios'
import router from '../router'

const api = axios.create({
  baseURL: '/api',
  timeout: 10000,
  withCredentials: true  // 允许发送 Cookie
})

// Helper to read token from cookie
const getTokenFromCookie = () => {
  const cookies = document.cookie.split('; ')
  const tokenCookie = cookies.find(c => c.trim().startsWith('auth_token='))
  if (tokenCookie) {
    return tokenCookie.split('=')[1]
  }
  return null
}

// Helper to clear auth cookie
const clearAuthCookie = () => {
  document.cookie = 'auth_token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;'
}

// 请求拦截器 - 添加 token
api.interceptors.request.use(
  config => {
    // Read from cookie instead of localStorage
    const token = getTokenFromCookie()
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  error => {
    return Promise.reject(error)
  }
)

// 响应拦截器
api.interceptors.response.use(
  response => response.data,
  error => {
    if (error.response) {
      const { status, data } = error.response
      const message = data?.error || '请求失败'

      if (status === 401) {
        // Clear auth cookie on unauthorized
        clearAuthCookie()
        localStorage.removeItem('user')
        localStorage.removeItem('isLoggedIn')
        router.push('/login')
      } else if (status === 429) {
        // Rate limiting
        return Promise.reject(new Error('请求过于频繁，请稍后再试'))
      }

      return Promise.reject(new Error(message))
    }
    return Promise.reject(error)
  }
)

// 认证
export const authAPI = {
  register: (data) => api.post('/auth/register', data),
  login: (data) => api.post('/auth/login', data),
  logout: () => api.post('/auth/logout'),
  getUser: () => api.get('/auth/me')
}

// 笔记
export const notesAPI = {
  getAll: (params) => api.get('/notes', { params }),
  getById: (id) => api.get(`/notes/${id}`),
  create: (data) => api.post('/notes', data),
  update: (id, data) => api.put(`/notes/${id}`, data),
  delete: (id) => api.delete(`/notes/${id}`),
  share: (id) => api.post(`/notes/${id}/share`),
  unshare: (id) => api.post(`/notes/${id}/unshare`),
  getVersions: (id) => api.get(`/notes/${id}/versions`),
  deleteVersion: (noteId, versionId) => api.delete(`/notes/${noteId}/versions/${versionId}`),
  restore: (id, versionId) => api.post(`/notes/${id}/restore/${versionId}`)
}

// 分类
export const foldersAPI = {
  getAll: () => api.get('/folders'),
  create: (data) => api.post('/folders', data),
  update: (id, data) => api.put(`/folders/${id}`, data),
  delete: (id) => api.delete(`/folders/${id}`),
  reorder: (folderIds) => api.put('/folders/reorder', folderIds),
  togglePin: (id) => api.put(`/folders/${id}/pin`)
}

// 分享
export const sharedAPI = {
  getByToken: (token) => api.get(`/shared/${token}`)
}

export default api
