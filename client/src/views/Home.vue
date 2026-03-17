<template>
  <div class="main-layout">
    <!-- 侧边栏 -->
    <div class="sidebar">
      <div class="sidebar-header">
        <h2>Markdown 笔记</h2>
      </div>
      <div class="sidebar-menu">
        <div
          class="menu-item"
          :class="{ active: currentFolder === null }"
          @click="selectFolder(null)"
        >
          <span>全部笔记</span>
          <el-tag size="small">{{ totalNotes }}</el-tag>
        </div>
        <div
          class="menu-item"
          :class="{ active: currentFolder === 'uncategorized' }"
          @click="selectFolder('uncategorized')"
        >
          <span>未分类</span>
          <el-tag size="small">{{ uncategorizedCount }}</el-tag>
        </div>

        <div style="padding: 10px 15px; display: flex; justify-content: space-between; align-items: center;">
          <span style="font-weight: bold; color: #666;">分类</span>
          <el-button size="small" text @click="showFolderDialog = true">
            <el-icon><Plus /></el-icon>
          </el-button>
        </div>

        <div
          v-for="folder in folders"
          :key="folder.id"
          class="folder-item"
          :class="{ active: currentFolder === folder.id }"
          @click="selectFolder(folder.id)"
        >
          <span>{{ folder.name }}</span>
          <el-tag size="small">{{ folder.noteCount }}</el-tag>
        </div>
      </div>

      <div style="padding: 15px; border-top: 1px solid #e0e0e0;">
        <el-button style="width: 100%;" type="primary" @click="logout">
          退出登录
        </el-button>
      </div>
    </div>

    <!-- 主内容区 -->
    <div class="main-content">
      <div class="content-header">
        <div style="display: flex; align-items: center; gap: 15px;">
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
        <el-button type="primary" @click="createNote">
          <el-icon><Plus /></el-icon>
          新建笔记
        </el-button>
      </div>

      <div class="content-body">
        <div v-if="notes.length === 0" class="empty-state">
          <div class="icon">📝</div>
          <p>暂无笔记，点击"新建笔记"开始</p>
        </div>

        <div v-else class="notes-grid">
          <div
            v-for="note in notes"
            :key="note.id"
            class="note-card"
            @click="editNote(note.id)"
          >
            <h3>{{ note.title }}</h3>
            <p>{{ note.content || '暂无内容' }}</p>
            <div class="note-meta">
              <span>{{ formatDate(note.updatedAt) }}</span>
              <el-tag v-if="note.isShared" size="small" type="success" style="margin-left: 10px;">
                已分享
              </el-tag>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 新建分类对话框 -->
    <el-dialog v-model="showFolderDialog" title="新建分类" width="400px">
      <el-form :model="folderForm" @submit.prevent="createFolder">
        <el-form-item>
          <el-input
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
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { notesAPI, foldersAPI } from '../api'

const router = useRouter()

const notes = ref([])
const folders = ref([])
const currentFolder = ref(null)
const searchText = ref('')
const uncategorizedCount = ref(0)
const showFolderDialog = ref(false)
const folderForm = reactive({
  name: ''
})

const totalNotes = computed(() => {
  return notes.value.length + uncategorizedCount.value
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
  } catch (error) {
    ElMessage.error(error.message)
  }
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

const editNote = (id) => {
  router.push(`/note/${id}`)
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
  const date = new Date(dateStr)
  return date.toLocaleDateString('zh-CN', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

onMounted(() => {
  loadNotes()
  loadFolders()
})
</script>
