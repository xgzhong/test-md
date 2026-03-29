<template>
  <div class="main-layout">
    <!-- 侧边栏 -->
    <Sidebar
      :collapsed="sidebarCollapsed"
      :width="sidebarWidth"
      :folders="folders"
      :notes="sidebarNotes"
      :currentFolder="currentFolder"
      :totalNotes="totalNotes"
      :uncategorizedCount="uncategorizedCount"
      :draggedFolder="draggedFolder"
      :dragOverFolder="dragOverFolder"
      :isLevelChange="isLevelChange"
      :hoverSide="hoverSide"
      :hoverPosition="hoverPosition"
      @toggle="toggleSidebar"
      @select="handleSelectFolder"
      @openNote="openNote"
      @addFolder="showFolderDialog = true"
      @pin="togglePinFolder"
      @edit="editFolderName"
      @delete="confirmDeleteFolder"
      @addChildFolder="openAddChildFolder"
      @addNote="createNoteInFolder"
      @dragStart="onDragStart"
      @dragOver="onDragOver"
      @drag-leave="onDragLeave"
      @drop="onDrop"
      @update:width="sidebarWidth = $event"
      @titleClick="goHome"
    />

    <!-- 主内容区 -->
    <div class="main-wrapper">
      <!-- 头部栏 -->
      <header class="header-bar">
        <div class="header-left">
          <el-icon class="home-icon" @click="goHome"><Back /></el-icon>
          <el-breadcrumb separator="/">
            <el-breadcrumb-item>
              <span class="breadcrumb-link" @click="goHome">首页</span>
            </el-breadcrumb-item>
            <el-breadcrumb-item v-if="parentFolder">
              <span class="breadcrumb-link" @click="goToParent">{{ parentFolder.name }}</span>
            </el-breadcrumb-item>
            <el-breadcrumb-item>{{ currentFolderData?.name }}</el-breadcrumb-item>
          </el-breadcrumb>
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
        <!-- 操作卡片区域 -->
        <div class="action-cards">
          <div class="action-card" @click="createNoteInCurrentFolder">
            <div class="action-icon">📝</div>
            <div class="action-text">
              <h3>新建笔记</h3>
              <p>在该分类下创建新笔记</p>
            </div>
          </div>
          <div class="action-card" @click="showAddChildDialog = true">
            <div class="action-icon">📁</div>
            <div class="action-text">
              <h3>新建子分类</h3>
              <p>在该分类下创建子分类</p>
            </div>
          </div>
        </div>

        <div class="divider"></div>

        <!-- 子分类卡片列表 -->
        <div v-if="childFolders.length > 0" class="section-title">
          <span>子分类</span>
        </div>
        <div v-if="childFolders.length > 0" class="folders-grid">
          <div
            v-for="child in childFolders"
            :key="child.id"
            class="folder-card"
            @click="goToFolder(child.id)"
          >
            <el-icon class="folder-card-delete" @click.stop="confirmDeleteChild(child)"><Delete /></el-icon>
            <el-icon class="folder-card-icon"><Folder /></el-icon>
            <span class="folder-card-name">{{ child.name }}</span>
          </div>
        </div>

        <!-- 笔记卡片列表 -->
        <div v-if="notes.length === 0 && childFolders.length === 0" class="empty-state">
          <div class="icon">📄</div>
          <p>该分类下暂无笔记</p>
        </div>

        <div v-if="notes.length > 0" class="section-title" style="margin-top: 20px;">
          <span>笔记</span>
        </div>
        <div v-if="notes.length > 0" class="notes-grid">
          <div
            v-for="note in notes"
            :key="note.id"
            class="note-card"
            @click="editNote(note.id)"
          >
            <div class="note-card-header">
              <h3>{{ note.title }}</h3>
              <el-icon class="delete-icon" @click.stop="confirmDelete(note)"><Delete /></el-icon>
            </div>
            <p v-text="note.content ? escapeHtml(note.content) : '暂无内容'"></p>
            <div class="note-meta">
              <span>{{ formatDate(note.updatedAt) }}</span>
              <el-tag v-if="note.isShared" size="small" type="success" style="margin-left: 10px;">
                已分享
              </el-tag>
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
          <span class="footer-link" @click="goHome">{{ packageJson.name }}</span>
          <span style="margin: 0 10px;">|</span>
          <a href="https://github.com/xgzhong/test-md" target="_blank" class="github-link">GitHub</a>
        </p>
      </footer>
    </div>

    <!-- 新建子分类对话框 -->
    <el-dialog v-model="showAddChildDialog" title="新建子分类" width="400px">
      <el-form @submit.prevent="createChildFolder">
        <el-form-item>
          <el-input
            v-model="newChildFolderName"
            placeholder="分类名称"
            @keyup.enter="createChildFolder"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showAddChildDialog = false">取消</el-button>
        <el-button type="primary" @click="createChildFolder">确定</el-button>
      </template>
    </el-dialog>

    <!-- 修改分类对话框 -->
    <el-dialog v-model="showEditFolderDialog" title="修改分类" width="400px" @opened="editFolderInputRef?.focus()">
      <el-form @submit.prevent="updateFolderName">
        <el-form-item label="分类名称">
          <el-input
            ref="editFolderInputRef"
            v-model="editFolderForm.name"
            placeholder="分类名称"
            @keyup.enter="updateFolderName"
          />
        </el-form-item>
        <el-form-item label="父级分类">
          <el-select v-model="editFolderForm.parentId" placeholder="顶级分类（无父级）" clearable style="width: 100%">
            <el-option
              v-for="folder in folders"
              :key="folder.id"
              :label="'└ ' + folder.name"
              :value="folder.id"
              :disabled="String(folder.id) === String(editFolderForm.id)"
            />
          </el-select>
          <div style="margin-top: 5px; font-size: 12px; color: #909399;">
            清空选择可将分类移至顶级
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showEditFolderDialog = false">取消</el-button>
        <el-button type="primary" @click="updateFolderName">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Back, Delete, Folder, User, SwitchButton, InfoFilled } from '@element-plus/icons-vue'
import { notesAPI, foldersAPI } from '../api'
import { useAuthStore } from '../stores/auth'
import Sidebar from '../components/Sidebar.vue'
import { flattenFolders, getParentIdStr, isSameParentId, isDescendant, formatDate, escapeHtml } from '../composables/useCommon'
import packageJson from '../../package.json'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const sidebarCollapsed = ref(false)
const sidebarWidth = ref(380)
const folders = ref([])
const sidebarNotes = ref([])
const currentFolder = ref(null)
const totalNotes = ref(0)
const uncategorizedCount = ref(0)
const notes = ref([])
const currentFolderData = ref(null)
const parentFolder = ref(null)

// 分页状态
const currentPage = ref(1)
const pageSize = ref(15)
const totalCount = ref(0)
const pageMeta = ref(null)
const totalPages = computed(() => pageMeta.value?.totalPages ?? 0)

// 拖拽状态
const draggedFolder = ref(null)
const dragOverFolder = ref(null)
const hoverSide = ref('child')
const hoverPosition = ref('below')

const isLevelChange = computed(() => {
  if (!draggedFolder.value || !dragOverFolder.value) return false
  const draggedPid = draggedFolder.value.parentId ? String(draggedFolder.value.parentId) : '0'
  return draggedPid !== String(dragOverFolder.value.id)
})

const showAddChildDialog = ref(false)
const newChildFolderName = ref('')
const showEditFolderDialog = ref(false)
const editFolderInputRef = ref(null)
const editFolderForm = reactive({
  id: null,
  name: '',
  parentId: null
})

// 获取当前分类的子分类
const childFolders = computed(() => {
  if (!currentFolderData.value || !folders.value) return []
  const findChildren = (folderList, parentId) => {
    const children = []
    for (const folder of folderList) {
      if (String(folder.parentId) === String(parentId)) {
        children.push(folder)
      }
      if (folder.children && folder.children.length > 0) {
        children.push(...findChildren(folder.children, parentId))
      }
    }
    return children
  }
  return findChildren(folders.value, route.params.id)
})

const loadFolders = async () => {
  try {
    const res = await foldersAPI.getFolders()
    folders.value = res.folders
    uncategorizedCount.value = res.uncategorizedCount
    loadAllNotesForSidebar()
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const loadAllNotesForSidebar = async () => {
  try {
    const res = await notesAPI.getNotes({})
    sidebarNotes.value = res.notes || []
  } catch (error) {
    console.error('加载笔记失败:', error)
  }
}

const loadFolderDetail = async () => {
  try {
    const folderId = route.params.id
    currentFolder.value = folderId

    // 加载文件夹详情
    const findFolder = (folderList, fid) => {
      for (const folder of folderList) {
        if (String(folder.id) === String(fid)) {
          return folder
        }
        if (folder.children && folder.children.length > 0) {
          const found = findFolder(folder.children, fid)
          if (found) return found
        }
      }
      return null
    }
    currentFolderData.value = findFolder(folders.value, folderId)

    // 构建父分类信息
    if (currentFolderData.value?.parentId) {
      parentFolder.value = findFolder(folders.value, currentFolderData.value.parentId)
    } else {
      parentFolder.value = null
    }

    // 加载该分类下的笔记（分页）
    const res = await notesAPI.getPageNotes({
      page: currentPage.value,
      pageSize: pageSize.value,
      folderId: folderId
    })
    notes.value = res.items || []
    const tc = res.metaData?.totalCount
    totalCount.value = typeof tc === 'number' ? tc : (typeof tc === 'string' ? parseInt(tc, 10) : 0)
    pageMeta.value = res.metaData || null
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const goHome = () => {
  router.push('/home')
}

const goToParent = () => {
  if (parentFolder.value) {
    router.push(`/folder/${parentFolder.value.id}`)
  } else {
    goHome()
  }
}

const goToFolder = (folderId) => {
  router.push(`/folder/${folderId}`)
}

const handlePageChange = () => {
  loadFolderDetail()
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

const logout = async () => {
  try {
    await ElMessageBox.confirm('确定要退出登录吗？', '提示', { type: 'warning' })
    await authStore.logout()
  } catch (e) {
    // User cancelled - no action needed
  }
}

const handleSelectFolder = (folderId) => {
  if (folderId === null) {
    router.push('/home')
  } else if (folderId === 'uncategorized') {
    router.push('/home?uncategorized=true')
  } else {
    router.push(`/folder/${folderId}`)
  }
}

const openNote = (noteId) => {
  router.push(`/note/${noteId}`)
}

const createNoteInCurrentFolder = async () => {
  try {
    const note = await notesAPI.createNote({
      title: '无标题笔记',
      content: '',
      folderId: route.params.id
    })
    if (note?.id) {
      router.push(`/note/${note.id}`)
    }
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const createChildFolder = async () => {
  if (!newChildFolderName.value.trim()) {
    ElMessage.warning('请输入分类名称')
    return
  }
  try {
    await foldersAPI.createFolder({
      name: newChildFolderName.value,
      parentId: route.params.id
    })
    ElMessage.success('子分类创建成功')
    newChildFolderName.value = ''
    showAddChildDialog.value = false
    loadFolders()
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const confirmDelete = (note) => {
  ElMessageBox.confirm(`确定要删除笔记"${note.title}"吗？`, '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(async () => {
    try {
      await notesAPI.deleteNote(note.id)
      ElMessage.success('删除成功')
      loadFolderDetail()
      loadFolders()
    } catch (error) {
      ElMessage.error(error.message)
    }
  }).catch(() => {})
}

const editNote = (id) => {
  router.push(`/note/${id}`)
}

// 拖拽相关
const toggleSidebar = () => {
  sidebarCollapsed.value = !sidebarCollapsed.value
}

const onDragStart = (event, folder) => {
  draggedFolder.value = folder
  event.dataTransfer.effectAllowed = 'move'
}

const onDragOver = (event, folder) => {
  dragOverFolder.value = folder
  event.dataTransfer.dropEffect = 'move'
  const target = event.target.closest('.folder-item')
  if (!target) return
  const rect = target.getBoundingClientRect()
  const y = event.clientY - rect.top
  const relativeY = y / rect.height
  hoverSide.value = relativeY < 0.35 ? 'sibling' : 'child'
  hoverPosition.value = relativeY < 0.5 ? 'above' : 'below'
}

const onDragLeave = () => {
  dragOverFolder.value = null
}

const onDrop = async (event, targetFolder) => {
  const dragged = draggedFolder.value
  draggedFolder.value = null
  dragOverFolder.value = null

  if (!dragged || !targetFolder) return
  if (String(dragged.id) === String(targetFolder.id)) return

  const flatFolders = flattenFolders(folders.value)
  const draggedItem = flatFolders.find(f => String(f.id) === String(dragged.id))
  const targetItem = flatFolders.find(f => String(f.id) === String(targetFolder.id))

  if (!draggedItem || !targetItem) return

  if (isDescendant(draggedItem, targetFolder.id, flatFolders)) {
    ElMessage.warning('不能将分类拖拽到其子分类下')
    return
  }

  const isSameLevel = isSameParentId(draggedItem.parentId, targetFolder.parentId)
  const shouldBeChild = hoverSide.value === 'child'

  if (isSameLevel && !shouldBeChild) {
    const parentIdStr = getParentIdStr(draggedItem.parentId)
    const siblings = flatFolders.filter(f => getParentIdStr(f.parentId) === parentIdStr)
    const draggedIndex = siblings.findIndex(f => String(f.id) === String(dragged.id))
    const targetIndex = siblings.findIndex(f => String(f.id) === String(targetFolder.id))

    if (draggedIndex === -1 || targetIndex === -1) return

    const item = siblings.splice(draggedIndex, 1)[0]
    siblings.splice(targetIndex, 0, item)

    try {
      const folderIds = siblings.map(f => f.id)
      await foldersAPI.reorderFolders({ folderIds })
      loadFolders()
    } catch (error) {
      ElMessage.error('保存排序失败')
      loadFolders()
    }
    return
  }

  let newParentId
  if (hoverSide.value === 'child') {
    newParentId = targetFolder.id
  } else {
    newParentId = getParentIdStr(targetFolder.parentId)
  }

  try {
    await foldersAPI.updateFolder(dragged.id, { parentId: newParentId })
    ElMessage.success('分类移动成功')
    loadFolders()
  } catch (error) {
    console.error('移动分类失败:', error)
    ElMessage.error(error.message || '移动分类失败')
    loadFolders()
  }
}

const togglePinFolder = async (folder) => {
  try {
    await foldersAPI.pinFolder(folder.id)
    loadFolders()
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const editFolderName = (folder) => {
  editFolderForm.id = folder.id
  editFolderForm.name = folder.name
  // parentId 为 '0' 或 0 时表示顶级分类，显示为空
  const pid = folder.parentId ? String(folder.parentId) : '0'
  editFolderForm.parentId = pid === '0' ? null : folder.parentId
  showEditFolderDialog.value = true
}

const updateFolderName = async () => {
  if (!editFolderForm.name.trim()) {
    ElMessage.warning('请输入分类名称')
    return
  }

  try {
    // 如果 parentId 为 null/undefined（用户清空了选择），发送 '0' 表示移除父级
    const parentIdValue = (editFolderForm.parentId === null || editFolderForm.parentId === undefined) ? '0' : editFolderForm.parentId
    await foldersAPI.updateFolder(editFolderForm.id, {
      name: editFolderForm.name,
      parentId: parentIdValue
    })
    ElMessage.success('分类修改成功')
    showEditFolderDialog.value = false
    loadFolders()
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const confirmDeleteFolder = (folder) => {
  ElMessageBox.confirm(`确定要删除分类"${folder.name}"吗？删除后该分类下的笔记将变为未分类。`, '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(async () => {
    try {
      await foldersAPI.deleteFolder(folder.id)
      ElMessage.success('删除成功')
      loadFolders()
      if (String(currentFolder.value) === String(folder.id)) {
        goHome()
      }
    } catch (error) {
      ElMessage.error(error.message)
    }
  }).catch(() => {})
}

const confirmDeleteChild = (child) => {
  ElMessageBox.confirm(`确定要删除子分类"${child.name}"吗？删除后该分类下的笔记将变为未分类。`, '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(async () => {
    try {
      await foldersAPI.deleteFolder(child.id)
      ElMessage.success('删除成功')
      loadFolders()
      loadFolderDetail()
    } catch (error) {
      ElMessage.error(error.message)
    }
  }).catch(() => {})
}

const openAddChildFolder = (parentFolder) => {
  newChildFolderName.value = ''
  showAddChildDialog.value = true
}

const createNoteInFolder = async (folder) => {
  try {
    const note = await notesAPI.createNote({
      title: '无标题笔记',
      content: '',
      folderId: folder.id
    })
    if (note?.id) {
      router.push(`/note/${note.id}`)
    }
  } catch (error) {
    ElMessage.error(error.message)
  }
}

onMounted(() => {
  loadFolders().then(() => {
    loadFolderDetail()
  })
})

// 监听路由变化，重新加载分类详情
watch(() => route.params.id, (newId, oldId) => {
  if (newId && newId !== oldId) {
    currentPage.value = 1 // 切换分类时重置页码
    loadFolderDetail()
  }
})
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
  gap: 10px;
}

.header-right {
  display: flex;
  gap: 10px;
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

.home-icon {
  cursor: pointer;
  font-size: 20px;
  color: #409eff;
}

.home-icon:hover {
  color: #66b1ff;
}

.breadcrumb-link {
  color: #409eff;
  cursor: pointer;
}

.breadcrumb-link:hover {
  color: #66b1ff;
}

.content-area {
  flex: 1;
  padding: 20px;
  overflow-y: auto;
}

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

.empty-state {
  text-align: center;
  padding: 60px 20px;
  color: #909399;
}

.empty-state .icon {
  font-size: 64px;
  margin-bottom: 20px;
}

.empty-state p {
  margin-bottom: 20px;
  font-size: 16px;
}

.action-cards {
  display: flex;
  gap: 16px;
  margin-bottom: 0;
}

.action-card {
  display: flex;
  align-items: center;
  gap: 15px;
  padding: 20px;
  background: white;
  border: 1px solid #e4e7ed;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
  min-width: 200px;
}

.action-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  border-color: #409eff;
}

.action-icon {
  font-size: 32px;
}

.action-text h3 {
  margin: 0 0 5px 0;
  font-size: 16px;
  color: #303133;
}

.action-text p {
  margin: 0;
  font-size: 13px;
  color: #909399;
}

.divider {
  height: 1px;
  background: #e4e7ed;
  margin: 20px 0;
}

.section-title {
  font-size: 16px;
  font-weight: bold;
  color: #303133;
  margin-bottom: 16px;
}

.folders-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 16px;
}

.folder-card {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 20px;
  background: white;
  border: 1px solid #e4e7ed;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
  position: relative;
}

.folder-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  border-color: #409eff;
}

.folder-card:hover .folder-card-delete {
  opacity: 1;
}

.folder-card-delete {
  position: absolute;
  top: 8px;
  right: 8px;
  color: #f56c6c;
  opacity: 0;
  transition: opacity 0.2s;
  cursor: pointer;
  font-size: 18px;
  padding: 6px;
  border-radius: 4px;
}

.folder-card-delete:hover {
  color: #e64040;
}

.folder-card-icon {
  font-size: 24px;
  color: #409eff;
}

.folder-card-name {
  font-size: 14px;
  color: #303133;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.notes-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 16px;
}

.note-card {
  background: white;
  border: 1px solid #e4e7ed;
  border-radius: 8px;
  padding: 16px;
  cursor: pointer;
  transition: all 0.2s;
  min-height: 120px;
}

.note-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  border-color: #409eff;
}

.note-card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 10px;
}

.note-card-header h3 {
  margin: 0;
  font-size: 16px;
  color: #303133;
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.delete-icon {
  color: #c0c4cc;
  cursor: pointer;
  opacity: 0;
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

.note-card:hover .delete-icon {
  opacity: 1;
}

.delete-icon:hover {
  color: #e64040;
}

.note-card p {
  margin: 0;
  color: #606266;
  font-size: 14px;
  line-height: 1.5;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
}

.note-meta {
  margin-top: 12px;
  font-size: 12px;
  color: #909399;
  display: flex;
  align-items: center;
}

.note-folder-name {
  margin-left: auto;
  color: #409eff;
}
</style>
