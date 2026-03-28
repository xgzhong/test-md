import { createRouter, createWebHistory, type Router } from 'vue-router'

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

export default router