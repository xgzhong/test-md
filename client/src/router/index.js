import { createRouter, createWebHistory } from 'vue-router'
import Login from '../views/Login.vue'
import Register from '../views/Register.vue'
import Home from '../views/Home.vue'
import NoteEditorVditor from '../views/NoteEditorVditor.vue'
import Shared from '../views/Shared.vue'
import FolderDetail from '../views/FolderDetail.vue'
import { authAPI } from '../api'

const routes = [
  {
    path: '/',
    redirect: '/home'
  },
  {
    path: '/login',
    name: 'Login',
    component: Login
  },
  {
    path: '/register',
    name: 'Register',
    component: Register
  },
  {
    path: '/home',
    name: 'Home',
    component: Home,
    meta: { requiresAuth: true }
  },
  {
    path: '/folder/:id',
    name: 'FolderDetail',
    component: FolderDetail,
    meta: { requiresAuth: true }
  },
  {
    path: '/note/:id',
    name: 'NoteEditor',
    component: NoteEditorVditor,
    meta: { requiresAuth: true }
  },
  {
    path: '/shared/:token',
    name: 'Shared',
    component: Shared
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 路由守卫 - 使用 API 验证 token 有效性
router.beforeEach(async (to, from, next) => {
  // 检查 localStorage 标记作为快速判断
  const isLoggedIn = localStorage.getItem('isLoggedIn') === 'true'

  if (to.meta.requiresAuth) {
    if (!isLoggedIn) {
      next('/login')
      return
    }

    // 验证 token 有效性
    try {
      await authAPI.getUser()
      next()
    } catch (error) {
      // 只处理 401 错误（认证失败），其他错误（网络问题等）继续放行
      if (error.response?.status === 401) {
        localStorage.removeItem('isLoggedIn')
        localStorage.removeItem('user')
        next('/login')
      } else {
        // 网络错误或其他错误，允许访问，可能只是临时连接问题
        next()
      }
    }
  } else if ((to.path === '/login' || to.path === '/register') && isLoggedIn) {
    next('/home')
  } else {
    next()
  }
})

export default router
