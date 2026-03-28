import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authAPI, type User, type LoginRequest, type RegisterRequest } from '../api'
import router from '../router'

export const useAuthStore = defineStore('auth', () => {
  // State
  const user = ref<User | null>(null)
  const isLoggedIn = ref(false)
  const isLoading = ref(false)

  // Getters
  const isAuthenticated = computed(() => isLoggedIn.value && user.value !== null)
  const userId = computed(() => user.value?.id)
  const username = computed(() => user.value?.username)

  // Actions
  function initAuth() {
    const storedUser = localStorage.getItem('user')
    const storedIsLoggedIn = localStorage.getItem('isLoggedIn')

    if (storedUser && storedIsLoggedIn === 'true') {
      try {
        user.value = JSON.parse(storedUser)
        isLoggedIn.value = true
      } catch {
        localStorage.removeItem('user')
        localStorage.removeItem('isLoggedIn')
      }
    }
  }

  async function login(email: string, password: string): Promise<void> {
    isLoading.value = true
    try {
      const data: LoginRequest = { email, password }
      const res = await authAPI.login(data)
      user.value = res.user as User
      isLoggedIn.value = true
      if (res.user) {
        localStorage.setItem('user', JSON.stringify(res.user))
      }
      localStorage.setItem('isLoggedIn', 'true')
      router.push('/home')
    } catch (error: any) {
      throw new Error(error.message || '登录失败')
    } finally {
      isLoading.value = false
    }
  }

  async function register(username: string, email: string, password: string): Promise<void> {
    isLoading.value = true
    try {
      const data: RegisterRequest = { username, email, password }
      await authAPI.register(data)
      router.push('/login')
    } catch (error: any) {
      throw new Error(error.message || '注册失败')
    } finally {
      isLoading.value = false
    }
  }

  async function logout(): Promise<void> {
    try {
      await authAPI.logout()
    } finally {
      user.value = null
      isLoggedIn.value = false
      localStorage.removeItem('user')
      localStorage.removeItem('isLoggedIn')
      router.push('/login')
    }
  }

  async function fetchUser(): Promise<void> {
    try {
      const res = await authAPI.getUser()
      user.value = res
      isLoggedIn.value = true
      localStorage.setItem('user', JSON.stringify(res))
      localStorage.setItem('isLoggedIn', 'true')
    } catch {
      user.value = null
      isLoggedIn.value = false
      localStorage.removeItem('user')
      localStorage.removeItem('isLoggedIn')
    }
  }

  return {
    // State
    user,
    isLoggedIn,
    isLoading,
    // Getters
    isAuthenticated,
    userId,
    username,
    // Actions
    initAuth,
    login,
    register,
    logout,
    fetchUser
  }
})