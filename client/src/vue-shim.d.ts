declare module '*.vue' {
  import type { DefineComponent } from 'vue'
  const component: DefineComponent<{}, {}, any>
  export default component
}

declare module 'dompurify' {
  const DOMPurify: any
  export default DOMPurify
}

// Router module declaration
declare module '../router/index' {
  import type { Router } from 'vue-router'
  const router: Router
  export default router
}
