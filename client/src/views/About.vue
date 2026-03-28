<template>
  <div class="main-layout">
    <!-- 侧边栏 -->
    <Sidebar
      :collapsed="sidebarCollapsed"
      :width="sidebarWidth"
      :currentFolder="currentFolder"
      :totalNotes="0"
      :uncategorizedCount="0"
      :dragOverFolder="null"
      :draggedFolder="null"
      :hoverSide="'child'"
      :hoverPosition="'below'"
      @toggle="toggleSidebar"
      @select="selectFolder"
      @openNote="openNote"
      @update:width="sidebarWidth = $event"
      @titleClick="router.push('/home')"
    />

    <!-- 主内容区 -->
    <div class="main-wrapper">
      <!-- 头部栏 -->
      <header class="header-bar">
        <div class="header-left">
          <h2 class="page-title">关于</h2>
        </div>
        <div class="header-right">
          <router-link to="/home" class="home-link">
            <el-icon><HomeFilled /></el-icon>
            <span>首页</span>
          </router-link>
        </div>
      </header>

      <!-- 内容区 -->
      <main class="content-area">
        <div class="about-card">
          <h1>{{ packageJson.name }}</h1>
          <p class="version">版本 {{ packageJson.version }}</p>
          <p class="description">{{ packageJson.description }}</p>

          <div class="section">
            <h2>技术栈</h2>
            <ul>
              <li><strong>前端框架：</strong>Vue 3 (Composition API)</li>
              <li><strong>UI 组件库：</strong>Element Plus</li>
              <li><strong>状态管理：</strong>Pinia</li>
              <li><strong>路由：</strong>Vue Router 4</li>
              <li><strong>Markdown 编辑器：</strong>Vditor</li>
              <li><strong>HTTP 客户端：</strong>Axios</li>
              <li><strong>构建工具：</strong>Vite 5</li>
            </ul>
          </div>

          <div class="section">
            <h2>生产依赖</h2>
            <el-table :data="dependencies" stripe style="width: 100%">
              <el-table-column prop="name" label="包名" width="200" />
              <el-table-column prop="version" label="版本号" width="150" />
              <el-table-column prop="description" label="说明" />
            </el-table>
          </div>

          <div class="section">
            <h2>开发依赖</h2>
            <el-table :data="devDependencies" stripe style="width: 100%">
              <el-table-column prop="name" label="包名" width="200" />
              <el-table-column prop="version" label="版本号" width="150" />
              <el-table-column prop="description" label="说明" />
            </el-table>
          </div>

          <div class="section">
            <h2>项目地址</h2>
            <p>
              <a href="https://github.com/xgzhong/test-md" target="_blank" class="github-link">
                GitHub 仓库
              </a>
            </p>
          </div>
        </div>
      </main>

      <!-- 底部区 -->
      <footer class="footer-bar">
        <p>
          <span class="footer-link" @click="router.push('/home')">Markdown Notes</span>
          <span style="margin: 0 10px;">|</span>
          <a href="https://github.com/xgzhong/test-md" target="_blank" class="github-link">GitHub</a>
        </p>
      </footer>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { HomeFilled } from '@element-plus/icons-vue'
import Sidebar from '../components/Sidebar.vue'
import packageJson from '../../package.json'

const router = useRouter()

const sidebarCollapsed = ref(false)
const sidebarWidth = ref(380)
const currentFolder = ref<number | string | null>(null)

const toggleSidebar = () => {
  sidebarCollapsed.value = !sidebarCollapsed.value
}

const selectFolder = (folderId: number | string | null) => {
  if (folderId === null) {
    router.push('/home')
  } else {
    router.push(`/folder/${folderId}`)
  }
}

const openNote = (id: number) => {
  router.push(`/note/${id}`)
}

// 从 package.json 提取依赖信息
const dependencies = computed(() => {
  const deps = packageJson.dependencies || {}
  return Object.entries(deps).map(([name, version]) => ({
    name,
    version: (version as string).replace(/[\^~]/g, ''),
    description: getPackageDescription(name)
  }))
})

const devDependencies = computed(() => {
  const deps = packageJson.devDependencies || {}
  return Object.entries(deps).map(([name, version]) => ({
    name,
    version: (version as string).replace(/[\^~]/g, ''),
    description: getPackageDescription(name)
  }))
})

// 包名说明映射
const packageDescriptions: Record<string, string> = {
  'vue': '渐进式 JavaScript 框架',
  'vue-router': 'Vue.js 的官方路由',
  'pinia': 'Vue 的状态管理',
  'element-plus': 'Vue 3 的组件库',
  '@element-plus/icons-vue': 'Element Plus 图标库',
  'vditor': 'Markdown 编辑器',
  'marked': 'Markdown 解析器',
  'axios': 'HTTP 请求库',
  'dompurify': 'HTML 净化库，防止 XSS',
  '@vitejs/plugin-vue': 'Vite 的 Vue 插件',
  'typescript': 'TypeScript 类型系统',
  'vite': '下一代前端构建工具',
  'vue-tsc': 'Vue 的 TypeScript 检查工具',
  '@types/dompurify': 'DOMPurify 的类型定义',
  '@types/node': 'Node.js 的类型定义'
}

const getPackageDescription = (name: string): string => {
  return packageDescriptions[name] || ''
}
</script>

<style scoped>
.main-layout {
  display: flex;
  height: 100vh;
  overflow: hidden;
}

.main-wrapper {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
  background: #f5f7fa;
}

.header-bar {
  height: 60px;
  padding: 0 20px;
  background: white;
  border-bottom: 1px solid #e4e7ed;
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-shrink: 0;
}

.header-left {
  display: flex;
  align-items: center;
}

.page-title {
  margin: 0;
  font-size: 18px;
  color: #333;
}

.header-right {
  display: flex;
  align-items: center;
}

.home-link {
  display: flex;
  align-items: center;
  gap: 5px;
  color: #409eff;
  text-decoration: none;
  font-size: 14px;
}

.home-link:hover {
  color: #66b1ff;
}

.content-area {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
}

.about-card {
  width: 100%;
  max-width: 800px;
  margin: 0 auto;
  background: white;
  border-radius: 8px;
  padding: 40px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
}

.about-card h1 {
  text-align: center;
  color: #333;
  margin-bottom: 10px;
  font-size: 24px;
}

.version {
  text-align: center;
  color: #909399;
  font-size: 14px;
  margin-bottom: 15px;
}

.description {
  text-align: center;
  color: #606266;
  line-height: 1.8;
  font-size: 14px;
  margin-bottom: 30px;
  padding: 15px;
  background: #f5f7fa;
  border-radius: 6px;
}

.section {
  margin-bottom: 25px;
}

.section:last-child {
  margin-bottom: 0;
}

.section h2 {
  color: #409eff;
  font-size: 16px;
  margin-bottom: 12px;
  padding-bottom: 8px;
  border-bottom: 2px solid #409eff;
}

.section ul {
  list-style: none;
  padding: 0;
  margin: 0;
}

.section li {
  padding: 6px 0;
  color: #606266;
  font-size: 14px;
  border-bottom: 1px solid #f0f0f0;
}

.section li:last-child {
  border-bottom: none;
}

.section li strong {
  color: #333;
  font-weight: 600;
}

.github-link {
  color: #409eff;
  text-decoration: none;
  font-size: 14px;
}

.github-link:hover {
  text-decoration: underline;
}

.footer-bar {
  height: 50px;
  padding: 0 20px;
  background: white;
  border-top: 1px solid #e4e7ed;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.footer-link {
  cursor: pointer;
  color: #409eff;
}

.footer-link:hover {
  color: #66b1ff;
}
</style>
