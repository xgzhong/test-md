import { createRouter, createWebHistory, type Router } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const routes = [
  {
    path: '/',
    redirect: '/home'
  },
  {
    path: '/login',
    name: 'Login',
    component: () => import('../views/Login.vue')
  },
  {
    path: '/register',
    name: 'Register',
    component: () => import('../views/Register.vue')
  },
  {
    path: '/home',
    name: 'Home',
    component: () => import('../views/Home.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/folder/:id',
    name: 'FolderDetail',
    component: () => import('../views/FolderDetail.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/note/:id',
    name: 'NoteEditor',
    component: () => import('../views/NoteEditorVditor.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/shared/:token',
    name: 'Shared',
    component: () => import('../views/Shared.vue')
  },
  {
    path: '/about',
    name: 'About',
    component: () => import('../views/About.vue'),
    meta: { requiresAuth: true }
  }
]

const router: Router = createRouter({
  history: createWebHistory(),
  routes
})

// Navigation guard for auth protection
router.beforeEach((to, _from, next) => {
  const authStore = useAuthStore()
  authStore.initAuth()

  if (to.meta.requiresAuth && !authStore.isLoggedIn) {
    next({ name: 'Login', query: { redirect: to.fullPath } })
  } else if ((to.name === 'Login' || to.name === 'Register') && authStore.isLoggedIn) {
    next({ name: 'Home' })
  } else {
    next()
  }
})

export default router