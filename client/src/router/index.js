import { createRouter, createWebHistory } from 'vue-router'
import Login from '../views/Login.vue'
import Register from '../views/Register.vue'
import Home from '../views/Home.vue'
import NoteEditorVditor from '../views/NoteEditorVditor.vue'
import Shared from '../views/Shared.vue'
import FolderDetail from '../views/FolderDetail.vue'

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

// 路由守卫
router.beforeEach((to, from, next) => {
  // Check auth status via API or localStorage flag
  const isLoggedIn = localStorage.getItem('isLoggedIn') === 'true'

  if (to.meta.requiresAuth && !isLoggedIn) {
    next('/login')
  } else if ((to.path === '/login' || to.path === '/register') && isLoggedIn) {
    next('/home')
  } else {
    next()
  }
})

export default router
