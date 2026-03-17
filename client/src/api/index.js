import axios from 'axios'

const api = axios.create({
  baseURL: '/api',
  timeout: 10000
})

// 请求拦截器 - 添加 token
api.interceptors.request.use(
  config => {
    const token = localStorage.getItem('token')
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
      const message = error.response.data?.error || '请求失败'
      if (error.response.status === 401) {
        localStorage.removeItem('token')
        localStorage.removeItem('user')
        window.location.href = '/login'
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
  restore: (id, versionId) => api.post(`/notes/${id}/restore/${versionId}`)
}

// 分类
export const foldersAPI = {
  getAll: () => api.get('/folders'),
  create: (data) => api.post('/folders', data),
  update: (id, data) => api.put(`/folders/${id}`, data),
  delete: (id) => api.delete(`/folders/${id}`)
}

// 分享
export const sharedAPI = {
  getByToken: (token) => api.get(`/shared/${token}`)
}

export default api
