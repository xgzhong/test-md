<template>
  <div class="main-layout">
    <!-- 侧边栏 -->
    <div class="sidebar" :class="{ collapsed: sidebarCollapsed }">
      <div class="sidebar-header">
        <h2 v-if="!sidebarCollapsed">Markdown 笔记</h2>
      </div>
      <!-- 折叠按钮 -->
      <div class="sidebar-toggle" @click="toggleSidebar">
        <el-icon v-if="sidebarCollapsed"><Expand /></el-icon>
        <el-icon v-else><Fold /></el-icon>
      </div>
      <div class="sidebar-menu" v-show="!sidebarCollapsed">
        <div
          class="menu-item"
          :class="{ active: currentFolder === null }"
          @click="selectFolder(null)"
        >
          <span class="menu-item-text">全部笔记</span>
          <el-tag size="small">{{ totalNotes }}</el-tag>
        </div>
        <div
          class="menu-item"
          :class="{ active: currentFolder === 'uncategorized' }"
          @click="selectFolder('uncategorized')"
        >
          <span class="menu-item-text">未分类</span>
          <el-tag size="small">{{ uncategorizedCount }}</el-tag>
        </div>

        <div class="sidebar-section-header">
          <span class="sidebar-section-title">分类</span>
          <el-button class="add-folder-btn" size="small" text @click="showFolderDialog = true">
            <el-icon><Plus /></el-icon>
          </el-button>
        </div>

        <div class="folder-list">
          <div
            v-for="(folder, index) in folders"
            :key="folder.id"
            class="folder-item"
            :class="{ active: currentFolder === folder.id, pinned: folder.isPinned }"
            draggable="true"
            @click="selectFolder(folder.id)"
            @dragstart="onDragStart($event, index)"
            @dragover.prevent="onDragOver($event, index)"
            @drop="onDrop($event, index)"
          >
            <el-icon v-if="folder.isPinned" class="folder-pin-icon"><Star /></el-icon>
            <span v-else class="folder-index">{{ index + 1 }}.</span>
            <el-tooltip :content="folder.name" placement="top" :disabled="folder.name.length <= 10">
              <span class="folder-name">{{ folder.name }}</span>
            </el-tooltip>
            <el-tag size="small">{{ folder.noteCount }}</el-tag>
            <el-icon class="folder-pin-btn" @click.stop="togglePinFolder(folder)">
              <span v-if="folder.isPinned">&#x2605;</span>
              <span v-else>&#x2606;</span>
            </el-icon>
            <el-icon class="folder-edit-icon" @click.stop="editFolderName(folder)"><Edit /></el-icon>
            <el-icon class="folder-delete-icon" @click.stop="confirmDeleteFolder(folder)"><Delete /></el-icon>
          </div>
        </div>
      </div>
    </div>

    <!-- 主内容区 -->
    <div class="main-content">
      <div class="content-header">
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
        <div class="user-info">
          <span class="username">{{ currentUser?.username || currentUser?.email }}</span>
          <el-button type="danger" size="small" @click="logout">
            退出登录
          </el-button>
        </div>
      </div>

      <div class="content-body">
        <!-- 操作按钮区域 -->
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

        <!-- 分割线 -->
        <div class="divider"></div>

        <!-- 笔记列表 -->
        <div v-if="notes.length === 0" class="empty-state">
          <div class="icon">📄</div>
          <p>暂无笔记，点击上方卡片开始</p>
        </div>

        <div v-else class="notes-grid">
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
            <p>{{ note.content || '暂无内容' }}</p>
            <div class="note-meta">
              <span>{{ formatDate(note.updatedAt) }}</span>
              <el-tag v-if="note.isShared" size="small" type="success" style="margin-left: 10px;">
                已分享
              </el-tag>
              <span v-if="getFolderName(note.folderId)" class="note-folder-name">
                {{ getFolderName(note.folderId) }}
              </span>
            </div>
          </div>
        </div>

        <!-- 底部信息 -->
        <div class="footer">
          <p>
            <span>Markdown Notes App</span>
            <span style="margin: 0 10px;">|</span>
            <a href="https://github.com" target="_blank">开源地址</a>
            <span style="margin: 0 10px;">|</span>
            <span>作者：Your Name</span>
          </p>
        </div>
      </div>
    </div>

    <!-- 新建分类对话框 -->
    <el-dialog v-model="showFolderDialog" title="新建分类" width="400px" @opened="folderInputRef?.focus()">
      <el-form :model="folderForm" @submit.prevent="createFolder">
        <el-form-item>
          <el-input
            ref="folderInputRef"
            v-model="folderForm.name"
            placeholder="分类名称"
            @keyup.enter="createFolder"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showFolderDialog = false">取消</el-button>
        <el-button type="primary" @click="createFolder">确定</el-button>
      </template>
    </el-dialog>

    <!-- 修改分类名称对话框 -->
    <el-dialog v-model="showEditFolderDialog" title="修改分类名称" width="400px" @opened="editFolderInputRef?.focus()">
      <el-form :model="editFolderForm" @submit.prevent="updateFolderName">
        <el-form-item>
          <el-input
            ref="editFolderInputRef"
            v-model="editFolderForm.name"
            placeholder="分类名称"
            @keyup.enter="updateFolderName"
          />
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
import { ref, reactive, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Search, Document, Delete, Star, Fold, Expand, Edit } from '@element-plus/icons-vue'
import { notesAPI, foldersAPI } from '../api'

const router = useRouter()

const currentUser = ref(null)
const notes = ref([])
const folders = ref([])
const currentFolder = ref(null)
const searchText = ref('')
const uncategorizedCount = ref(0)
const totalCount = ref(0)
const showFolderDialog = ref(false)
const showEditFolderDialog = ref(false)
const sidebarCollapsed = ref(false)
const folderInputRef = ref(null)
const editFolderInputRef = ref(null)
const folderForm = reactive({
  name: ''
})
const editFolderForm = reactive({
  id: null,
  name: ''
})

const toggleSidebar = () => {
  sidebarCollapsed.value = !sidebarCollapsed.value
}

const totalNotes = computed(() => {
  return totalCount.value
})

const loadNotes = async () => {
  try {
    const params = {}
    if (currentFolder.value !== null && currentFolder.value !== 'uncategorized') {
      params.folderId = currentFolder.value
    } else if (currentFolder.value === 'uncategorized') {
      params.folderId = 'null'
    }
    if (searchText.value) {
      params.search = searchText.value
    }

    const res = await notesAPI.getAll(params)
    notes.value = res.notes
    totalCount.value = res.totalCount || 0
  } catch (error) {
    ElMessage.error(error.message)
  }
}

let draggedIndex = null

const onDragStart = (event, index) => {
  draggedIndex = index
  event.dataTransfer.effectAllowed = 'move'
}

const onDragOver = (event, index) => {
  event.dataTransfer.dropEffect = 'move'
}

const onDrop = async (event, index) => {
  if (draggedIndex === null || draggedIndex === index) return

  // 重新排序
  const item = folders.value.splice(draggedIndex, 1)[0]
  folders.value.splice(index, 0, item)

  // 保存新顺序到后端
  try {
    const folderIds = folders.value.map(f => f.id)
    await foldersAPI.reorder(folderIds)
  } catch (error) {
    ElMessage.error('保存排序失败')
    loadFolders() // 重新加载
  }

  draggedIndex = null
}

const loadFolders = async () => {
  try {
    const res = await foldersAPI.getAll()
    folders.value = res.folders
    uncategorizedCount.value = res.uncategorizedCount
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const selectFolder = (folderId) => {
  currentFolder.value = folderId
  loadNotes()
}

const handleSearch = () => {
  loadNotes()
}

const createNote = async () => {
  try {
    const res = await notesAPI.create({
      title: '无标题笔记',
      content: ''
    })
    router.push(`/note/${res.note.id}`)
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const createWorkLog = async () => {
  const now = new Date()
  const year = now.getFullYear()
  const month = now.getMonth() + 1

  // 获取当月天数
  const daysInMonth = new Date(year, month, 0).getDate()

  // 星期几的中文数组
  const weekDays = ['周日', '周一', '周二', '周三', '周四', '周五', '周六']

  // 生成工作日志内容
  let content = `# ${year}年${String(month).padStart(2, '0')}月\n`
  content += '每日工作日志\n\n'

  for (let day = 1; day <= daysInMonth; day++) {
    const date = new Date(year, month - 1, day)
    const weekDay = weekDays[date.getDay()]
    const dateStr = `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`

    content += `${dateStr} ${weekDay}\n`
    content += '- 计划1：\n'
    content += '- 记录1：\n'
    content += '- 记录2：\n\n'
  }

  try {
    const res = await notesAPI.create({
      title: `${year}年${String(month).padStart(2, '0')}月工作日志`,
      content: content
    })
    ElMessage.success('工作日志生成成功')
    router.push(`/note/${res.note.id}`)
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const editNote = (id) => {
  router.push(`/note/${id}`)
}

const confirmDelete = (note) => {
  ElMessageBox.confirm(`确定要删除笔记"${note.title}"吗？`, '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(async () => {
    try {
      await notesAPI.delete(note.id)
      ElMessage.success('删除成功')
      loadNotes()
      loadFolders()
    } catch (error) {
      ElMessage.error(error.message)
    }
  }).catch(() => {})
}

const confirmDeleteFolder = (folder) => {
  ElMessageBox.confirm(`确定要删除分类"${folder.name}"吗？删除后该分类下的笔记将变为未分类。`, '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(async () => {
    try {
      await foldersAPI.delete(folder.id)
      ElMessage.success('删除成功')
      loadFolders()
      if (currentFolder.value === folder.id) {
        selectFolder(null)
      }
    } catch (error) {
      ElMessage.error(error.message)
    }
  }).catch(() => {})
}

const togglePinFolder = async (folder) => {
  try {
    await foldersAPI.togglePin(folder.id)
    loadFolders()
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const createFolder = async () => {
  if (!folderForm.name.trim()) {
    ElMessage.warning('请输入分类名称')
    return
  }

  try {
    await foldersAPI.create({ name: folderForm.name })
    ElMessage.success('分类创建成功')
    folderForm.name = ''
    showFolderDialog.value = false
    loadFolders()
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const editFolderName = (folder) => {
  editFolderForm.id = folder.id
  editFolderForm.name = folder.name
  showEditFolderDialog.value = true
}

const updateFolderName = async () => {
  if (!editFolderForm.name.trim()) {
    ElMessage.warning('请输入分类名称')
    return
  }

  try {
    await foldersAPI.update(editFolderForm.id, { name: editFolderForm.name })
    ElMessage.success('分类名称修改成功')
    showEditFolderDialog.value = false
    loadFolders()
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const logout = () => {
  ElMessageBox.confirm('确定要退出登录吗？', '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(() => {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    router.push('/login')
  }).catch(() => {})
}

const formatDate = (dateStr) => {
  const date = new Date(dateStr + 'Z')
  return date.toLocaleDateString('zh-CN', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

const getFolderName = (folderId) => {
  if (!folderId) return ''
  const folder = folders.value.find(f => f.id === folderId)
  if (!folder) return ''
  return folder.name.length > 8 ? folder.name.substring(0, 8) + '...' : folder.name
}

onMounted(() => {
  // 加载用户信息
  const userStr = localStorage.getItem('user')
  if (userStr) {
    currentUser.value = JSON.parse(userStr)
  }
  loadNotes()
  loadFolders()
})
</script>
