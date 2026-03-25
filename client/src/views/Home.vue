<template>
  <div class="main-layout">
    <!-- 侧边栏 -->
    <Sidebar
      :collapsed="sidebarCollapsed"
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
      @select="selectFolder"
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
    />

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
    <el-dialog v-model="showFolderDialog" title="新建分类" width="400px" @opened="folderInputRef?.focus()" @close="resetFolderForm">
      <el-form :model="folderForm" @submit.prevent="createFolder">
        <el-form-item label="上级分类">
          <el-select v-model="folderForm.parentId" placeholder="顶级分类" clearable style="width: 100%">
            <el-option
              v-for="folder in folders"
              :key="folder.id"
              :label="'└ ' + folder.name"
              :value="folder.id"
            />
          </el-select>
        </el-form-item>
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
    <el-dialog v-model="showEditFolderDialog" title="修改分类" width="400px" @opened="editFolderInputRef?.focus()">
      <el-form :model="editFolderForm" @submit.prevent="updateFolderName">
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
              :disabled="folder.id === editFolderForm.id"
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
import { ref, reactive, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Search, Document, Delete, Star, Fold, Expand, Edit } from '@element-plus/icons-vue'
import { notesAPI, foldersAPI } from '../api'
import Sidebar from '../components/Sidebar.vue'

const router = useRouter()

const currentUser = ref(null)
const notes = ref([])
const sidebarNotes = ref([])
const folders = ref([])
const currentFolder = ref(null)
const searchText = ref('')
const uncategorizedCount = ref(0)

// 拖拽状态
const draggedFolder = ref(null)
const dragOverFolder = ref(null)
const hoverSide = ref('child') // 'sibling' 或 'child'
const hoverPosition = ref('below') // 'above' 或 'below'，用于指示线位置

// 检测是否在改变层级
const isLevelChange = computed(() => {
  if (!draggedFolder.value || !dragOverFolder.value) return false
  const draggedPid = draggedFolder.value.parentId ? String(draggedFolder.value.parentId) : '0'
  return draggedPid !== String(dragOverFolder.value.id)
})
const totalCount = ref(0)
const showFolderDialog = ref(false)
const showEditFolderDialog = ref(false)
const sidebarCollapsed = ref(false)
const folderInputRef = ref(null)
const editFolderInputRef = ref(null)
const folderForm = reactive({
  name: '',
  parentId: null
})

const resetFolderForm = () => {
  folderForm.name = ''
  folderForm.parentId = null
}

const editFolderForm = reactive({
  id: null,
  name: '',
  parentId: null
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

const onDragStart = (event, folder) => {
  draggedFolder.value = folder
  event.dataTransfer.effectAllowed = 'move'
}

const onDragOver = (event, folder) => {
  dragOverFolder.value = folder
  event.dataTransfer.dropEffect = 'move'
  // 查找 folder-item 元素
  const target = event.target.closest('.folder-item')
  if (!target) return
  const rect = target.getBoundingClientRect()
  const y = event.clientY - rect.top
  const relativeY = y / rect.height
  // 鼠标在元素上半部分 = 并列 (sibling)，下半部分 = 子级 (child)
  hoverSide.value = relativeY < 0.35 ? 'sibling' : 'child'
  // 记录鼠标在元素的上半部分还是下半部分
  hoverPosition.value = relativeY < 0.5 ? 'above' : 'below'
}

const onDragLeave = () => {
  dragOverFolder.value = null
}

// 扁平化文件夹列表（用于拖拽排序）
const flattenFolders = (folderList) => {
  const result = []
  const flatten = (items, parentId = '0') => {
    for (const item of items) {
      // 保持 parentId 为字符串，0 表示根级别
      const pid = item.parentId ? String(item.parentId) : '0'
      result.push({ ...item, parentId: pid })
      if (item.children && item.children.length > 0) {
        flatten(item.children, String(item.id))
      }
    }
  }
  flatten(folderList)
  return result
}

const onDrop = async (event, targetFolder) => {
  // 保存状态用于处理
  const dragged = draggedFolder.value

  // 清空拖拽状态
  draggedFolder.value = null
  dragOverFolder.value = null

  if (!dragged || !targetFolder) return
  if (dragged.id === targetFolder.id) return

  // 扁平化当前文件夹列表
  const flatFolders = flattenFolders(folders.value)
  const draggedItem = flatFolders.find(f => f.id === dragged.id)
  const targetItem = flatFolders.find(f => f.id === targetFolder.id)

  if (!draggedItem || !targetItem) return

  // 判断是否在同一层级：比较父级ID，0/null/undefined 都视为根级别
  const isRootId = (id) => {
    const strId = String(id)
    return strId === '0' || strId === '' || strId === 'null' || strId === 'undefined' || id === null || id === undefined
  }

  const getParentIdStr = (id) => {
    if (id === null || id === undefined) return '0'
    const strId = String(id)
    return isRootId(strId) ? '0' : strId
  }

  const isSameParentId = (id1, id2) => {
    return getParentIdStr(id1) === getParentIdStr(id2)
  }

  // 判断是否拖拽到了目标文件夹的子级（不允许）
  const isDescendant = (parent, childId) => {
    const parentIdStr = String(parent.id)
    const children = flatFolders.filter(f => String(f.parentId) === parentIdStr)
    for (const child of children) {
      if (String(child.id) === String(childId)) return true
      if (isDescendant(child, childId)) return true
    }
    return false
  }

  if (isDescendant(draggedItem, targetFolder.id)) {
    ElMessage.warning('不能将分类拖拽到其子分类下')
    return
  }

  // 判断是否在同一层级
  const isSameLevel = isSameParentId(draggedItem.parentId, targetFolder.parentId)

  // 如果 hoverSide 是 child（拖到分类中间），即使同一层级也要成为子级
  const shouldBeChild = hoverSide.value === 'child'

  if (isSameLevel && !shouldBeChild) {
    // 同一层级内排序
    const parentIdStr = getParentIdStr(draggedItem.parentId)
    const siblings = flatFolders.filter(f => getParentIdStr(f.parentId) === parentIdStr)
    const draggedIndex = siblings.findIndex(f => String(f.id) === String(dragged.id))
    const targetIndex = siblings.findIndex(f => String(f.id) === String(targetFolder.id))

    if (draggedIndex === -1 || targetIndex === -1) return

    // 调整顺序
    const item = siblings.splice(draggedIndex, 1)[0]
    siblings.splice(targetIndex, 0, item)

    try {
      const folderIds = siblings.map(f => f.id)
      await foldersAPI.reorder(folderIds)
      await loadFolders()
    } catch (error) {
      ElMessage.error('保存排序失败')
      loadFolders()
    }
    return
  }

  // 不同层级：根据 hoverSide 决定是成为子级还是并列
  let newParentId
  if (hoverSide.value === 'child') {
    // 成为目标分类的子级
    newParentId = targetFolder.id
  } else {
    // 和目标分类并列（使用目标分类的父级）
    newParentId = getParentIdStr(targetFolder.parentId)
  }

  try {
    await foldersAPI.update(dragged.id, { parentId: newParentId })
    ElMessage.success('分类移动成功')
    loadFolders()
  } catch (error) {
    console.error('移动分类失败:', error)
    ElMessage.error(error.message || '移动分类失败')
    loadFolders()
  }
}

const loadFolders = async () => {
  try {
    const res = await foldersAPI.getAll()
    folders.value = res.folders
    uncategorizedCount.value = res.uncategorizedCount
    // 加载所有笔记用于侧边栏展示
    loadAllNotesForSidebar()
  } catch (error) {
    ElMessage.error(error.message)
  }
}

// 加载所有笔记用于侧边栏分类下显示
const loadAllNotesForSidebar = async () => {
  try {
    const res = await notesAPI.getAll({})
    sidebarNotes.value = res.notes || []
  } catch (error) {
    console.error('加载笔记失败:', error)
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

const openNote = (id) => {
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

// 打开新建子级分类对话框
const openAddChildFolder = (parentFolder) => {
  folderForm.name = ''
  folderForm.parentId = String(parentFolder.id)
  showFolderDialog.value = true
}

// 在指定分类下新建笔记
const createNoteInFolder = async (folder) => {
  try {
    const res = await notesAPI.create({
      title: '无标题笔记',
      content: '',
      folderId: folder.id
    })
    ElMessage.success('笔记创建成功')
    // 跳转到笔记编辑页面
    router.push(`/note/${res.note.id}`)
  } catch (error) {
    ElMessage.error(error.message)
  }
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
    // parentId 为 null 时发送 '0' 表示顶级分类
    const parentIdValue = folderForm.parentId === null ? '0' : folderForm.parentId
    await foldersAPI.create({ name: folderForm.name, parentId: parentIdValue })
    ElMessage.success('分类创建成功')
    resetFolderForm()
    showFolderDialog.value = false
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
    await foldersAPI.update(editFolderForm.id, {
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
