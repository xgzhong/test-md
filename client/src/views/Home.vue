<template>
  <div class="main-layout">
    <!-- 侧边栏 -->
    <Sidebar
      :title="packageJson.name"
      :collapsed="sidebarCollapsed"
      :width="sidebarWidth"
      :currentFolder="currentFolder"
      :totalNotes="totalNotesForSidebar"
      :uncategorizedCount="sidebar.uncategorizedCount.value"
      :dragOverFolder="sidebar.dragOverFolder.value"
      :draggedFolder="sidebar.draggedFolder.value"
      :hoverSide="sidebar.hoverSide.value"
      :hoverPosition="sidebar.hoverPosition.value"
      @toggle="toggleSidebar"
      @select="selectFolder"
      @openNote="openNote"
      @addFolder="showFolderDialog = true"
      @pin="togglePinFolder"
      @edit="editFolderName"
      @delete="confirmDeleteFolder"
      @addChildFolder="openAddChildFolder"
      @addNote="createNoteInFolder"
      @update:width="sidebarWidth = $event"
      @titleClick="router.push('/home')"
      @loaded="onSidebarLoaded"
    />

    <!-- 主内容区 -->
    <div class="main-wrapper">
      <!-- 头部栏 -->
      <header class="header-bar">
        <div class="header-left">
          <el-input
            v-model="searchText"
            placeholder="搜索笔记..."
            style="width: 250px;"
            clearable
            @input="handleSearch"
          >
            <template #prefix>
              <el-icon><Search /></el-icon>
            </template>
          </el-input>
        </div>
        <div class="header-right">
          <router-link to="/about" class="about-link">
            <el-icon><InfoFilled /></el-icon>
            <span>关于</span>
          </router-link>
          <el-divider direction="vertical" />
          <el-icon class="user-icon"><User /></el-icon>
          <span class="username">{{ authStore.user?.username || authStore.user?.email }}</span>
          <el-divider direction="vertical" />
          <el-button type="info" size="small" text @click="logout">
            <el-icon><SwitchButton /></el-icon>
            <span>退出</span>
          </el-button>
        </div>
      </header>

      <!-- 内容区 -->
      <main class="content-area">
        <div class="action-cards">
          <div class="action-card" @click="createNote">
            <div class="action-icon">📝</div>
            <div class="action-text">
              <h3>新建笔记</h3>
              <p>创建一个新的 Markdown 笔记</p>
            </div>
          </div>
          <div class="action-card" @click="createWorkLog">
            <div class="action-icon">📋</div>
            <div class="action-text">
              <h3>生成工作日志</h3>
              <p>自动生成当月工作日志模板</p>
            </div>
          </div>
        </div>

        <div class="divider"></div>

        <div v-if="notes.length === 0" class="empty-state">
          <div class="icon">📄</div>
          <p>暂无笔记，点击上方卡片开始</p>
        </div>

        <div v-else class="notes-grid">
          <div v-for="note in notes" :key="note.id" class="note-card" @click="editNote(note.id)" @mouseenter="preloadEditor">
            <div class="note-card-header">
              <h3>{{ note.title }}</h3>
              <el-icon class="delete-icon" @click.stop="confirmDelete(note)"><Delete /></el-icon>
            </div>
            <p v-text="note.content ? escapeHtml(note.content) : '暂无内容'"></p>
            <div class="note-meta">
              <span>{{ formatDate(note.updatedAt) }}</span>
              <el-tag v-if="note.isShared" size="small" type="success" style="margin-left: 10px;">已分享</el-tag>
              <span v-if="note.folderName" class="note-folder-name">{{ note.folderName }}</span>
            </div>
          </div>
        </div>
      </main>

      <!-- 分页 -->
      <div v-if="totalPages > 1" class="pagination-container">
        <el-pagination
          v-model:current-page="currentPage"
          :page-size="pageSize"
          :total="totalCount"
          background
          layout="prev, pager, next"
          @current-change="handlePageChange"
        />
      </div>

      <!-- 底部区 -->
      <footer class="footer-bar">
        <p>
          <span class="footer-link" @click="router.push('/home')">{{ packageJson.name }}</span>
          <span style="margin: 0 10px;">|</span>
          <a href="https://github.com/xgzhong/test-md" target="_blank" class="github-link">GitHub</a>
        </p>
      </footer>
    </div>

    <!-- 修改分类对话框 -->
    <el-dialog v-model="showEditFolderDialog" title="修改分类" width="400px" @opened="editFolderInputRef?.focus()">
      <el-form @submit.prevent="updateFolderName">
        <el-form-item label="分类名称">
          <el-input ref="editFolderInputRef" v-model="editFolderForm.name" placeholder="分类名称" @keyup.enter="updateFolderName" />
        </el-form-item>
        <el-form-item label="父级分类">
          <el-select v-model="editFolderForm.parentId" placeholder="顶级分类（无父级）" clearable style="width: 100%">
            <el-option v-for="folder in sidebar.rootFolders.value" :key="folder.id" :label="'└ ' + folder.name" :value="folder.id" :disabled="String(folder.id) === String(editFolderForm.id)" />
          </el-select>
          <div style="margin-top: 5px; font-size: 12px; color: #909399;">清空选择可将分类移至顶级</div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showEditFolderDialog = false">取消</el-button>
        <el-button type="primary" @click="updateFolderName">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Search, Delete, User, SwitchButton, InfoFilled } from '@element-plus/icons-vue'
import { useAuthStore } from '../stores/auth'
import Sidebar from '../components/Sidebar.vue'
import { useSidebar } from '../composables/useSidebar'
import { formatDate, escapeHtml } from '../composables/useCommon'
import { notesAPI, type Note, type Folder, type PagedMetaData } from '../api'
import packageJson from '../../package.json'

const router = useRouter()
const authStore = useAuthStore()
const sidebar = useSidebar()

const searchText = ref('')
const currentFolder = ref<number | string | null>(null)
const sidebarCollapsed = ref(false)
const sidebarWidth = ref(380)
const editFolderInputRef = ref<HTMLInputElement | null>(null)
const notes = ref<Note[]>([])

// 分页状态
const currentPage = ref(1)
const pageSize = ref(20)
const totalCount = ref(0)
const pageMeta = ref<PagedMetaData | null>(null)

const showFolderDialog = ref(false)
const showEditFolderDialog = ref(false)
const editFolderForm = ref({ id: null as string | null, name: '', parentId: null as string | null })
let searchTimer: ReturnType<typeof setTimeout> | null = null
let lastSearchTime = 0  // 用于忽略过时的响应

const totalPages = computed(() => pageMeta.value?.totalPages ?? 0)
const totalNotesForSidebar = computed(() => typeof totalCount.value === 'number' ? totalCount.value : Number(totalCount.value) || 0)

const onSidebarLoaded = async () => {
  await loadNotes()
}

const loadNotes = async () => {
  const thisSearchTime = Date.now()
  lastSearchTime = thisSearchTime

  const params: Record<string, string | number | undefined> = {
    page: currentPage.value,
    pageSize: pageSize.value
  }
  if (currentFolder.value !== null && currentFolder.value !== 'uncategorized') {
    params.folderId = currentFolder.value
  } else if (currentFolder.value === 'uncategorized') {
    params.folderId = 'null'
  }
  if (searchText.value) {
    params.search = searchText.value
  }
  try {
    const res = await notesAPI.getPageNotes(params)
    // Ignore stale responses
    if (thisSearchTime !== lastSearchTime) return
    notes.value = res.items || []
    const tc = res.metaData?.totalCount
    totalCount.value = typeof tc === 'number' ? tc : (typeof tc === 'string' ? parseInt(tc, 10) : 0)
    pageMeta.value = res.metaData || null
  } catch (error: unknown) {
    if (thisSearchTime !== lastSearchTime) return
    ElMessage.error(error instanceof Error ? error.message : '加载笔记失败')
  }
}

const selectFolder = (folderId: number | string | null) => {
  if (folderId === null) {
    currentFolder.value = null
  } else if (folderId === 'uncategorized') {
    currentFolder.value = 'uncategorized'
  } else {
    router.push(`/folder/${folderId}`)
    return
  }
  searchText.value = ''  // 切换文件夹时清空搜索
  currentPage.value = 1 // 切换文件夹时重置页码
  loadNotes()
}

const handleSearch = () => {
  if (searchTimer) clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    currentPage.value = 1 // 搜索时重置页码
    loadNotes()
  }, 300)
}

const handlePageChange = () => {
  loadNotes()
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

const createNote = async () => {
  const note = await sidebar.createNote('无标题笔记', '')
  if (note?.id) {
    router.push(`/note/${note.id}?new=true`)
  }
}

const createNoteInFolder = async (folder: Folder) => {
  const note = await sidebar.createNoteInFolder(folder, '无标题笔记', '')
  if (note?.id) {
    router.push(`/note/${note.id}?new=true`)
  }
}

const createWorkLog = async () => {
  const now = new Date()
  const year = now.getFullYear()
  const month = now.getMonth() + 1
  const daysInMonth = new Date(year, month, 0).getDate()
  const weekDays = ['周日', '周一', '周二', '周三', '周四', '周五', '周六']

  let content = `# ${year}年${String(month).padStart(2, '0')}月\n每日工作日志\n\n`
  for (let day = 1; day <= daysInMonth; day++) {
    const date = new Date(year, month - 1, day)
    const weekDay = weekDays[date.getDay()]
    const dateStr = `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`
    content += `${dateStr} ${weekDay}\n`
    content += '- 计划1：\n- 记录1：\n- 记录2：\n\n'
  }

  const note = await sidebar.createNote(`${year}年${String(month).padStart(2, '0')}月工作日志`, content)
  if (note?.id) {
    ElMessage.success('工作日志生成成功')
    router.push(`/note/${note.id}`)
  }
}

const editNote = (id: string | number) => router.push(`/note/${id}`)
const openNote = (id: string | number) => router.push(`/note/${id}`)

const confirmDelete = async (note: Note) => {
  try {
    await ElMessageBox.confirm(`确定要删除笔记"${note.title}"吗？`, '提示', { type: 'warning' })
    await sidebar.deleteNote(note.id)
    await loadNotes()
  } catch (e) {
    // User cancelled - no action needed
  }
}

const confirmDeleteFolder = async (folder: Folder) => {
  try {
    await ElMessageBox.confirm(`确定要删除分类"${folder.name}"吗？删除后该分类下的笔记将变为未分类。`, '提示', { type: 'warning' })
    await sidebar.deleteFolder(folder.id)
    if (String(currentFolder.value) === String(folder.id)) {
      selectFolder(null)
    }
  } catch (e) {
    // User cancelled - no action needed
  }
}

const openAddChildFolder = (parentFolder: any) => {
  showFolderDialog.value = true
  // 通过 sidebar 的 createFolder 方法处理
}

const togglePinFolder = async (folder: Folder) => {
  await sidebar.togglePinFolder(folder.id)
}

const editFolderName = (folder: Folder) => {
  editFolderForm.value.id = folder.id
  editFolderForm.value.name = folder.name
  editFolderForm.value.parentId = folder.parentId === '0' ? null : folder.parentId
  showEditFolderDialog.value = true
}

const updateFolderName = async () => {
  if (!editFolderForm.value.name.trim()) {
    ElMessage.warning('请输入分类名称')
    return
  }
  await sidebar.updateFolder(editFolderForm.value.id!, editFolderForm.value.name, editFolderForm.value.parentId)
  showEditFolderDialog.value = false
}

const toggleSidebar = () => {
  sidebarCollapsed.value = !sidebarCollapsed.value
}

const logout = async () => {
  try {
    await ElMessageBox.confirm('确定要退出登录吗？', '提示', { type: 'warning' })
    await authStore.logout()
  } catch (e) {
    // User cancelled - no action needed
  }
}

// 预加载笔记编辑器组件（只预加载一次）
let editorPreloaded = false
const preloadEditor = () => {
  if (editorPreloaded) return
  editorPreloaded = true
  const preload = () => import('../views/NoteEditorVditor.vue')
  if ('requestIdleCallback' in window) {
    requestIdleCallback(preload, { timeout: 3000 })
  } else {
    setTimeout(preload, 1000)
  }
}

onMounted(() => {
  authStore.initAuth()
  // 延迟执行，等首页数据加载完成后再预加载
  setTimeout(preloadEditor, 500)
})
</script>

<style scoped>
/* 主布局 */
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

/* 头部栏 */
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

.header-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.header-right .user-icon {
  font-size: 18px;
  color: #409eff;
}

.header-right .username {
  color: #606266;
  font-size: 14px;
}

.header-right .el-divider {
  margin: 0 4px;
}

.about-link {
  color: #409eff;
  text-decoration: none;
  font-size: 14px;
  display: flex;
  align-items: center;
  gap: 4px;
}

.about-link:hover {
  color: #66b1ff;
}

/* 内容区 */
.content-area {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
}

/* 操作卡片 */
.action-cards {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 20px;
  margin-bottom: 20px;
}

.action-card {
  display: flex;
  align-items: center;
  gap: 15px;
  padding: 20px;
  background: white;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.3s;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
  border: 2px solid transparent;
}

.action-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);
  border-color: #409eff;
}

.action-card .action-icon {
  font-size: 36px;
}

.action-card .action-text h3 {
  margin: 0 0 5px 0;
  font-size: 16px;
  color: #333;
}

.action-card .action-text p {
  margin: 0;
  font-size: 13px;
  color: #909399;
}

/* 分割线 */
.divider {
  height: 1px;
  background: #e4e7ed;
  margin: 20px 0;
}

/* 空状态 */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 60px 20px;
  color: #909399;
}

.empty-state .icon {
  font-size: 64px;
  margin-bottom: 20px;
}

.empty-state p {
  font-size: 16px;
  margin: 0;
}

/* 笔记网格 */
.notes-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 20px;
}

/* 笔记卡片 */
.note-card {
  background: white;
  border-radius: 8px;
  padding: 16px;
  cursor: pointer;
  transition: all 0.3s;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
}

.note-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);
}

.note-card h3 {
  margin: 0 0 10px 0;
  font-size: 16px;
  color: #333;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.note-card p {
  color: #666;
  font-size: 14px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  margin: 0 0 10px 0;
}

.note-card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
}

.delete-icon {
  color: #c0c4cc;
  cursor: pointer;
  width: 32px;
  height: 18px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
  transition: all 0.3s;
}

.delete-icon:hover {
  color: #f56c6c;
  background-color: rgba(245, 108, 108, 0.1);
}

.note-meta {
  font-size: 12px;
  color: #909399;
  display: flex;
  align-items: center;
}

.note-folder-name {
  color: #606266;
  font-size: 12px;
  margin-left: 10px;
  max-width: 80px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* 分页 */
.pagination-container {
  height: 50px;
  padding: 0 20px;
  background: #f5f7fa;
  border-top: 1px solid #e4e7ed;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

/* 底部区 */
.footer-bar {
  height: 50px;
  padding: 0 20px;
  background: #f5f7fa;
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

.github-link {
  color: #333;
  text-decoration: none;
}

.github-link:hover {
  color: #409eff;
}
</style>