<template>
  <div class="main-layout">
    <!-- 侧边栏 -->
    <div class="sidebar">
      <div class="sidebar-header" style="cursor: pointer;" @click="goHome">
        <h2>← 返回笔记列表</h2>
      </div>
      <div class="sidebar-menu">
        <div
          class="menu-item"
          :class="{ active: currentFolder === null }"
          @click="selectFolder(null)"
        >
          <span>全部笔记</span>
        </div>
        <div
          class="menu-item"
          :class="{ active: currentFolder === 'uncategorized' }"
          @click="selectFolder('uncategorized')"
        >
          <span>未分类</span>
        </div>

        <div style="padding: 10px 15px; font-weight: bold; color: #666;">分类</div>

        <div
          v-for="folder in folders"
          :key="folder.id"
          class="folder-item"
          :class="{ active: currentFolder === folder.id }"
          @click="selectFolder(folder.id)"
        >
          <span>{{ folder.name }}</span>
        </div>
      </div>
    </div>

    <!-- 编辑器区域 -->
    <div class="main-content" style="background: white;">
      <div class="content-header">
        <el-input
          v-model="noteTitle"
          placeholder="笔记标题"
          style="font-size: 18px; font-weight: bold;"
          @blur="saveNote"
        />
        <div style="display: flex; gap: 10px;">
          <el-select
            v-model="noteFolderId"
            placeholder="移动到分类"
            clearable
            style="width: 150px;"
            @change="saveNote"
          >
            <el-option label="未分类" :value="null" />
            <el-option
              v-for="folder in folders"
              :key="folder.id"
              :label="folder.name"
              :value="folder.id"
            />
          </el-select>
          <el-button @click="showVersions = true">
            <el-icon><Clock /></el-icon>
            版本历史
          </el-button>
          <el-button @click="handleShare">
            <el-icon><Share /></el-icon>
            {{ note.isShared ? '已分享' : '分享' }}
          </el-button>
          <el-button type="danger" @click="deleteNote">
            <el-icon><Delete /></el-icon>
            删除
          </el-button>
        </div>
      </div>

      <div class="editor-container">
        <div class="editor-left">
          <textarea
            v-model="noteContent"
            class="editor-textarea"
            placeholder="在这里使用 Markdown 编写笔记..."
            @input="handleInput"
          ></textarea>
        </div>
        <div class="editor-right">
          <div class="preview-title">预览</div>
          <div class="preview-content" v-html="renderedContent"></div>
        </div>
      </div>
    </div>

    <!-- 版本历史面板 -->
    <div v-if="showVersions" class="versions-panel">
      <div class="versions-header">
        <h3>版本历史</h3>
        <el-button text @click="showVersions = false">
          <el-icon><Close /></el-icon>
        </el-button>
      </div>
      <div class="versions-list">
        <div
          v-for="version in versions"
          :key="version.id"
          class="version-item"
          @click="viewVersion(version)"
        >
          <div class="time">{{ formatDate(version.createdAt) }}</div>
          <div class="preview">{{ version.title }}</div>
        </div>
      </div>
    </div>

    <!-- 分享对话框 -->
    <el-dialog v-model="showShareDialog" title="分享笔记" width="500px">
      <div v-if="shareUrl">
        <p style="margin-bottom: 15px;">复制以下链接分享给他人：</p>
        <el-input v-model="shareUrl" readonly>
          <template #append>
            <el-button @click="copyShareUrl">复制</el-button>
          </template>
        </el-input>
      </div>
      <div v-else>
        <p>该笔记尚未分享</p>
      </div>
      <template #footer>
        <el-button @click="showShareDialog = false">关闭</el-button>
        <el-button v-if="note.isShared" type="danger" @click="handleUnshare">
          取消分享
        </el-button>
      </template>
    </el-dialog>

    <!-- 版本预览对话框 -->
    <el-dialog v-model="showVersionPreview" title="版本预览" width="700px">
      <div v-if="selectedVersion">
        <p style="color: #666; margin-bottom: 15px;">
          创建时间：{{ formatDate(selectedVersion.createdAt) }}
        </p>
        <div class="preview-content" v-html="renderVersionContent"></div>
      </div>
      <template #footer>
        <el-button @click="showVersionPreview = false">关闭</el-button>
        <el-button type="primary" @click="restoreVersion">
          恢复到此版本
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { marked } from 'marked'
import { notesAPI, foldersAPI } from '../api'

const router = useRouter()
const route = useRoute()

const noteId = ref(null)
const note = ref({})
const noteTitle = ref('')
const noteContent = ref('')
const noteFolderId = ref(null)
const folders = ref([])
const currentFolder = ref(null)
const versions = ref([])
const showVersions = ref(false)
const showShareDialog = ref(false)
const showVersionPreview = ref(false)
const shareUrl = ref('')
const selectedVersion = ref(null)

let saveTimer = null

const renderedContent = computed(() => {
  return marked(noteContent.value || '')
})

const renderVersionContent = computed(() => {
  if (!selectedVersion.value) return ''
  return marked(selectedVersion.value.content || '')
})

const loadNote = async () => {
  try {
    const res = await notesAPI.getById(noteId.value)
    note.value = res.note
    noteTitle.value = res.note.title
    noteContent.value = res.note.content
    noteFolderId.value = res.note.folderId

    if (res.note.folderId) {
      currentFolder.value = res.note.folderId
    }
  } catch (error) {
    ElMessage.error(error.message)
    router.push('/home')
  }
}

const loadFolders = async () => {
  try {
    const res = await foldersAPI.getAll()
    folders.value = res.folders
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const loadVersions = async () => {
  try {
    const res = await notesAPI.getVersions(noteId.value)
    versions.value = res.versions
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const saveNote = async () => {
  if (!noteId.value) return

  try {
    await notesAPI.update(noteId.value, {
      title: noteTitle.value,
      content: noteContent.value,
      folderId: noteFolderId.value
    })
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const handleInput = () => {
  // 防抖保存
  if (saveTimer) clearTimeout(saveTimer)
  saveTimer = setTimeout(() => {
    saveNote()
  }, 1000)
}

const handleShare = async () => {
  try {
    const res = await notesAPI.share(noteId.value)
    shareUrl.value = window.location.origin + res.shareUrl
    showShareDialog.value = true
    loadNote()
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const copyShareUrl = () => {
  navigator.clipboard.writeText(shareUrl.value)
  ElMessage.success('复制成功')
}

const handleUnshare = async () => {
  try {
    await notesAPI.unshare(noteId.value)
    ElMessage.success('取消分享成功')
    showShareDialog.value = false
    loadNote()
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const deleteNote = () => {
  ElMessageBox.confirm('确定要删除这条笔记吗？', '警告', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(async () => {
    try {
      await notesAPI.delete(noteId.value)
      ElMessage.success('删除成功')
      router.push('/home')
    } catch (error) {
      ElMessage.error(error.message)
    }
  }).catch(() => {})
}

const viewVersion = (version) => {
  selectedVersion.value = version
  showVersionPreview.value = true
}

const restoreVersion = async () => {
  try {
    await notesAPI.restore(noteId.value, selectedVersion.value.id)
    ElMessage.success('恢复成功')
    showVersionPreview.value = false
    loadNote()
    loadVersions()
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const selectFolder = (folderId) => {
  currentFolder.value = folderId
  router.push('/home')
}

const goHome = () => {
  router.push('/home')
}

const formatDate = (dateStr) => {
  const date = new Date(dateStr)
  return date.toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

onMounted(() => {
  noteId.value = route.params.id
  if (noteId.value) {
    loadNote()
    loadFolders()
    loadVersions()
  }
})

watch(() => route.params.id, (newId) => {
  if (newId) {
    noteId.value = newId
    loadNote()
    loadVersions()
  }
})
</script>
