<template>
  <div class="main-layout" @click="handleOutsideClick" @keydown="handleKeydown">
    <!-- 侧边栏 -->
    <Sidebar
      :collapsed="false"
      :width="sidebarWidth"
      :folders="folders"
      :notes="notes"
      :currentFolder="currentFolder"
      :showAddFolder="false"
      :showPin="false"
      :showEdit="false"
      :showDelete="false"
      @select="handleSelectFolder"
      @openNote="handleOpenNote"
      @update:width="sidebarWidth = $event"
      @titleClick="handleGoHome"
    />

    <!-- 编辑器区域 -->
    <div class="main-content editor-main-content">
      <div class="content-header">
        <div class="header-left">
          <el-icon class="home-icon" @click="handleGoHome"><Back /></el-icon>
          <el-breadcrumb separator="/" v-if="breadcrumbPath.length > 0">
            <el-breadcrumb-item v-for="(item, index) in breadcrumbPath" :key="item.id">
              <span
                class="breadcrumb-item"
                :class="{ 'breadcrumb-clickable': index < breadcrumbPath.length - 1 }"
                @click="index < breadcrumbPath.length - 1 && handleBreadcrumbClick(item)"
              >
                {{ item.name }}
              </span>
            </el-breadcrumb-item>
          </el-breadcrumb>
          <span v-if="ui.showSavedTip" class="saved-tip">已保存</span>
          <span v-else class="saved-tip-placeholder"></span>
          <el-input
            v-model="note.title"
            placeholder="笔记标题"
            class="title-input"
            @input="handleTitleInput"
          />
          <el-button type="primary" @click="manualSave">保存版本</el-button>
        </div>
        <div class="header-right">
          <el-select
            v-model="noteFolderId"
            placeholder="移动到分类"
            clearable
            filterable
            class="folder-select"
            @change="handleFolderChange"
          >
            <el-option label="未分类" value="" />
            <el-option
              v-for="folder in folders"
              :key="folder.id"
              :label="folder.name"
              :value="folder.id"
            />
          </el-select>
          <el-button @click="ui.showVersions = true">
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
        <div id="vditor" class="vditor-editor"></div>
      </div>

      <div class="editor-footer">
        <span class="word-count">字数：{{ wordCount }}</span>
        <span class="char-count">字符：{{ charCount }}</span>
      </div>
    </div>

    <!-- 版本历史面板 -->
    <div v-if="ui.showVersions" class="versions-panel">
      <div class="versions-header">
        <h3>版本历史</h3>
        <el-button text @click="ui.showVersions = false">
          <el-icon><Close /></el-icon>
        </el-button>
      </div>
      <div class="versions-list">
        <div
          v-for="v in noteVersions"
          :key="v.id"
          class="version-item"
          @click="viewVersion(v)"
        >
          <div class="version-content">
            <div class="time">{{ formatDate(v.createdAt) }}</div>
            <div class="preview">{{ v.title }}</div>
          </div>
          <el-icon class="version-delete-icon" @click.stop="confirmDeleteVersion(v)"><Delete /></el-icon>
        </div>
      </div>
    </div>

    <!-- 分享对话框 -->
    <el-dialog v-model="ui.showShareDialog" title="分享笔记" width="500px">
      <div v-if="ui.shareUrl">
        <p class="share-tip">复制以下链接分享给他人：</p>
        <el-input v-model="ui.shareUrl" readonly>
          <template #append>
            <el-button @click="copyShareUrl">复制</el-button>
          </template>
        </el-input>
      </div>
      <div v-else>
        <p>该笔记尚未分享</p>
      </div>
      <template #footer>
        <el-button @click="ui.showShareDialog = false">关闭</el-button>
        <el-button v-if="note.isShared" type="danger" @click="handleUnshare">
          取消分享
        </el-button>
      </template>
    </el-dialog>

    <!-- 版本预览对话框 -->
    <el-dialog v-model="ui.showVersionPreview" title="版本预览" width="700px">
      <div v-if="ui.selectedVersion">
        <p class="v-time">
          创建时间：{{ formatDate(ui.selectedVersion.createdAt) }}
        </p>
        <div class="preview-content" v-html="renderVersionContent"></div>
      </div>
      <template #footer>
        <el-button @click="ui.showVersionPreview = false">关闭</el-button>
        <el-button type="primary" @click="restoreVersion">
          恢复到此版本
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, onBeforeUnmount, watch, nextTick } from 'vue'
import { useRouter, useRoute, onBeforeRouteLeave } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Back } from '@element-plus/icons-vue'
import { marked } from 'marked'
import DOMPurify from 'dompurify'
import { notesAPI, foldersAPI } from '../api'
import Sidebar from '../components/Sidebar.vue'
import Vditor from 'vditor'
import 'vditor/dist/index.css'

const router = useRouter()
const route = useRoute()

// Constants
const AUTO_SAVE_DEBOUNCE_MS = 3000
const AUTO_SAVE_MIN_CONTENT_DELTA = 100

// 侧边栏宽度
const sidebarWidth = ref(380)

// 笔记数据
const note = reactive({
  id: null,
  data: {},
  title: '',
  content: '',
  folderId: undefined,
  isNew: false,
  version: 0
})

// 标记是否正在执行程序化导航（避免路由守卫重复确认）
let isProgrammaticNavigation = false

// UI 状态
const ui = reactive({
  showVersions: false,
  showShareDialog: false,
  showVersionPreview: false,
  showSavedTip: false,
  hasUnsavedChanges: false,
  shareUrl: '',
  selectedVersion: null
})

// 原始数据（用于比较变化）
const original = reactive({
  title: '',
  content: ''
})

// 分类列表
const folders = ref([])
const notes = ref([])
const noteVersions = ref([])
const currentFolder = ref(null)

let vditor = null
let saveTimer = null
let savedTipTimer = null
let inputTimer = null

const wordCount = computed(() => {
  const text = note.content.replace(/[#*`\[\]()]/g, '').trim()
  if (!text) return 0
  return text.split(/\s+/).filter(word => word.length > 0).length
})

const charCount = computed(() => {
  return note.content.length
})

const noteFolderId = computed({
  get: () => note.folderId ?? '',
  set: (val) => { note.folderId = val === '' ? null : val }
})

const renderedContent = computed(() => {
  return marked(note.content || '')
})

// 构建面包屑路径 - 递归搜索树形结构
const breadcrumbPath = computed(() => {
  if (!note.folderId || !folders.value || folders.value.length === 0) return []
  const path = []

  // 在树形结构中递归查找文件夹
  const findFolder = (folderList, folderId) => {
    const fid = String(folderId)
    for (const folder of folderList) {
      if (String(folder.id) === fid) {
        return folder
      }
      if (folder.children && folder.children.length > 0) {
        const found = findFolder(folder.children, folderId)
        if (found) return found
      }
    }
    return null
  }

  const findFolderPath = (folderId, currentPath) => {
    const folder = findFolder(folders.value, folderId)
    if (folder) {
      if (folder.parentId) {
        findFolderPath(folder.parentId, currentPath)
      }
      currentPath.push({ id: folder.id, name: folder.name })
    }
  }
  findFolderPath(note.folderId, path)
  return path
})

// 点击面包屑项切换分类
const handleBreadcrumbClick = (folder) => {
  router.push('/home')
  // 触发切换到对应分类
  nextTick(() => {
    // Emit event to switch folder after navigation
  })
}

const renderVersionContent = computed(() => {
  if (!ui.selectedVersion) return ''
  const html = marked(ui.selectedVersion.content || '')
  return DOMPurify.sanitize(html)
})

// 重试机制
const withRetry = async (fn, retries = 3, delay = 1000) => {
  for (let i = 0; i < retries; i++) {
    try {
      return await fn()
    } catch (error) {
      if (i === retries - 1) throw error
      await new Promise(resolve => setTimeout(resolve, delay * (i + 1)))
    }
  }
}

const initVditor = (content, onReady) => {
  if (vditor) {
    vditor.destroy()
    vditor = null
  }

  vditor = new Vditor('vditor', {
    value: content || '',
    mode: 'wysiwyg',
    placeholder: '在这里使用 Markdown 编写笔记...',
    lang: 'zh_CN',
    after: () => {
      if (onReady) onReady()
    },
    toolbar: [
      'headings',
      'bold',
      'italic',
      'strike',
      '|',
      'line',
      'quote',
      'list',
      'ordered-list',
      'check',
      '|',
      'code',
      'inline-code',
      'link',
      'table',
      '|',
      'upload',
      '|',
      'undo',
      'redo',
      '|',
      'wysiwyg',
      'preview',
      'fullscreen'
    ],
    customWysiwygToolbar: () => [],
    toolbarConfig: {
      pin: true
    },
    upload: {
      accept: 'image/*',
      handler: async (files) => {
        const file = files[0]
        if (!file) return

        try {
          const reader = new FileReader()
          reader.onload = (e) => {
            const base64 = e.target.result
            vditor.insertValue(`![${file.name}](${base64})`)
          }
          reader.readAsDataURL(file)
        } catch (error) {
          ElMessage.error('图片上传失败')
        }
      }
    },
    input: (value) => {
      note.content = value
      handleInput()
    },
    blur: () => {
      handleBlur()
    },
    ready: () => {}
  })
}

const updateVditor = (content) => {
  if (vditor) {
    vditor.setValue(content || '')
  }
}

const loadNote = async () => {
  try {
    const res = await withRetry(() => notesAPI.getById(note.id))
    note.data = res.note
    note.title = res.note.title
    note.content = res.note.content
    note.folderId = res.note.folderId
    note.version = parseInt(res.note.version) || 0
    note.isShared = res.note.isShared || false

    original.title = res.note.title || ''
    original.content = res.note.content || ''

    note.isNew = note.version === 0

    if (res.note.folderId) {
      currentFolder.value = res.note.folderId
    }

    ui.hasUnsavedChanges = false

    if (vditor) {
      updateVditor(res.note.content || '')
    }
  } catch (error) {
    ElMessage.error(error.message)
    router.push('/home')
  }
}

const loadFolders = async () => {
  try {
    const res = await withRetry(() => foldersAPI.getAll())
    folders.value = res.folders
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const loadNotes = async () => {
  try {
    const res = await notesAPI.getAll({})
    notes.value = res.notes || []
  } catch (error) {
    console.error('加载笔记失败:', error)
  }
}

const loadVersions = async () => {
  try {
    const res = await withRetry(() => notesAPI.getVersions(note.id))
    noteVersions.value = res.versions
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const saveNote = async () => {
  if (!note.id) return

  if (note.version === 0 && !note.title && !note.content) {
    return
  }

  try {
    const res = await withRetry(() => notesAPI.update(note.id, {
      title: note.title || '无标题笔记',
      content: note.content,
      folderId: note.folderId
    }))

    original.title = note.title || '无标题笔记'
    original.content = note.content
    note.version = res.note?.v ? parseInt(res.note.version) : note.version + 1
    ui.hasUnsavedChanges = false
    note.isNew = false

    ui.showSavedTip = true
    if (savedTipTimer) clearTimeout(savedTipTimer)
    savedTipTimer = setTimeout(() => {
      ui.showSavedTip = false
    }, 2000)
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const manualSave = async () => {
  if (!note.id) return

  try {
    const res = await withRetry(() => notesAPI.update(note.id, {
      title: note.title || '无标题笔记',
      content: note.content,
      folderId: note.folderId,
      saveVersion: true
    }))

    original.title = note.title || '无标题笔记'
    original.content = note.content
    note.version = res.note?.v ? parseInt(res.note.version) : note.version + 1
    ui.hasUnsavedChanges = false
    note.isNew = false

    ui.showSavedTip = true
    if (savedTipTimer) clearTimeout(savedTipTimer)
    savedTipTimer = setTimeout(() => {
      ui.showSavedTip = false
    }, 2000)
    ElMessage.success('保存成功')
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const handleInput = () => {
  ui.hasUnsavedChanges = note.title !== original.title || note.content !== original.content

  if (inputTimer) clearTimeout(inputTimer)
  inputTimer = setTimeout(() => {
    const contentDelta = Math.abs(note.content.length - original.content.length)
    const hasSignificantChange = contentDelta > AUTO_SAVE_MIN_CONTENT_DELTA || note.title !== original.title
    if (hasSignificantChange) {
      saveNote()
    }
  }, AUTO_SAVE_DEBOUNCE_MS)
}

// Save on blur (when user switches tabs/windows)
const handleBlur = () => {
  if (ui.hasUnsavedChanges) {
    saveNote()
  }
}

const handleTitleInput = () => {
  ui.hasUnsavedChanges = note.title !== original.title || note.content !== original.content
}

const handleFolderChange = () => {
  saveNote()
}

const handleShare = async () => {
  try {
    const res = await withRetry(() => notesAPI.share(note.id))
    ui.shareUrl = window.location.origin + res.shareUrl
    ui.showShareDialog = true
    loadNote()
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const copyShareUrl = () => {
  navigator.clipboard.writeText(ui.shareUrl)
  ElMessage.success('复制成功')
}

const handleUnshare = async () => {
  try {
    await withRetry(() => notesAPI.unshare(note.id))
    ElMessage.success('取消分享成功')
    ui.showShareDialog = false
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
      await withRetry(() => notesAPI.delete(note.id))
      ElMessage.success('删除成功')
      router.push('/home')
    } catch (error) {
      ElMessage.error(error.message)
    }
  }).catch(() => {})
}

const viewVersion = (v) => {
  ui.selectedVersion = v
  ui.showVersionPreview = true
}

const restoreVersion = async () => {
  try {
    await withRetry(() => notesAPI.restore(note.id, ui.selectedVersion.id))
    ElMessage.success('恢复成功')
    ui.showVersionPreview = false
    loadNote()
    loadVersions()
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const confirmDeleteVersion = (v) => {
  ElMessageBox.confirm('确定要删除该版本吗？', '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(async () => {
    try {
      await withRetry(() => notesAPI.deleteVersion(note.id, v.id))
      ElMessage.success('删除成功')
      loadVersions()
    } catch (error) {
      ElMessage.error(error.message)
    }
  }).catch(() => {})
}

// 统一的离开确认逻辑
const confirmBeforeLeave = async () => {
  if (note.version === 0) {
    const hasChanges = note.title !== original.title || note.content !== original.content
    if (!hasChanges) {
      try {
        await withRetry(() => notesAPI.delete(note.id))
      } catch (error) {
        // 忽略删除错误
      }
      return true
    }
  }

  const currentHasChanges = note.title !== original.title || note.content !== original.content
  if (currentHasChanges || ui.hasUnsavedChanges) {
    return new Promise((resolve) => {
      ElMessageBox.confirm('您有未保存的更改，确定要离开吗？', '提示', {
        confirmButtonText: '保存并离开',
        cancelButtonText: '不保存',
        distinguishCancelAndClose: true,
        type: 'warning'
      }).then(async () => {
        await saveNote()
        resolve(true)
      }).catch(async (action) => {
        if (action === 'cancel') {
          if (note.version === 0) {
            try {
              await withRetry(() => notesAPI.delete(note.id))
            } catch (error) {
              // 忽略删除错误
            }
          }
          resolve(true)
        }
        resolve(false)
      })
    })
  }

  return true
}

// 统一的跳转逻辑
const navigateToHome = async (shouldSave = false, shouldDelete = false) => {
  if (shouldSave) {
    await saveNote()
  } else if (shouldDelete && note.version === 0) {
    try {
      await withRetry(() => notesAPI.delete(note.id))
    } catch (error) {
      // 忽略删除错误
    }
  }
  isProgrammaticNavigation = true
  router.push('/home')
  // 重置标志
  setTimeout(() => { isProgrammaticNavigation = false }, 100)
}

// 处理选择分类
const handleSelectFolder = async () => {
  const currentHasChanges = note.title !== original.title ||
                           note.content !== original.content ||
                           ui.hasUnsavedChanges

  if (!currentHasChanges) {
    if (note.version === 0 && !note.title && !note.content) {
      await navigateToHome(false, true)
    } else {
      await navigateToHome(false, false)
    }
    return
  }

  ElMessageBox.confirm('您有未保存的更改，确定要离开吗？', '提示', {
    confirmButtonText: '保存并离开',
    cancelButtonText: '不保存',
    distinguishCancelAndClose: true,
    type: 'warning'
  }).then(async () => {
    await navigateToHome(true, false)
  }).catch((action) => {
    if (action === 'cancel') {
      navigateToHome(false, note.version === 0)
    }
  })
}

// 处理打开笔记
const handleOpenNote = (noteId) => {
  if (noteId) {
    router.push(`/note/${noteId}`)
  }
}

// 处理返回按钮
const handleGoHome = async () => {
  const currentHasChanges = note.title !== original.title ||
                           note.content !== original.content ||
                           ui.hasUnsavedChanges

  if (!currentHasChanges) {
    if (note.version === 0 && !note.title && !note.content) {
      await navigateToHome(false, true)
    } else {
      await navigateToHome(false, false)
    }
    return
  }

  ElMessageBox.confirm('您有未保存的更改，确定要离开吗？', '提示', {
    confirmButtonText: '保存并离开',
    cancelButtonText: '不保存',
    distinguishCancelAndClose: true,
    type: 'warning'
  }).then(async () => {
    await navigateToHome(true, false)
  }).catch((action) => {
    if (action === 'cancel') {
      navigateToHome(false, note.version === 0)
    }
  })
}

// 处理键盘快捷键
const handleKeydown = (event) => {
  // Escape 键返回
  if (event.key === 'Escape') {
    handleGoHome()
  }
}

const formatDate = (dateStr) => {
  const date = new Date(dateStr + 'Z')
  return date.toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

onBeforeRouteLeave(async () => {
  // 如果是程序化导航，不触发确认对话框
  if (isProgrammaticNavigation) {
    return true
  }
  return await confirmBeforeLeave()
})

const handleOutsideClick = (event) => {
  if (!ui.showVersions) return
  const noteVersionsPanel = document.querySelector('.versions-panel')
  const contentHeader = document.querySelector('.content-header')
  if (noteVersionsPanel && !noteVersionsPanel.contains(event.target) && contentHeader && !contentHeader.contains(event.target)) {
    ui.showVersions = false
  }
}

onMounted(() => {
  note.id = route.params.id
  if (note.id) {
    initVditor('', () => {
      loadNote()
    })
    loadFolders()
    loadNotes()
    loadVersions()
  }
})

watch(() => route.params.id, (newId) => {
  if (newId) {
    note.id = newId
    loadNote()
    loadVersions()
  }
})

onBeforeUnmount(() => {
  if (vditor) {
    vditor.destroy()
    vditor = null
  }
  if (saveTimer) clearTimeout(saveTimer)
  if (inputTimer) clearTimeout(inputTimer)
  if (savedTipTimer) clearTimeout(savedTipTimer)
})
</script>

<style scoped>
.editor-main-content {
  background: white;
}

.header-clickable {
  cursor: pointer;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
}

.saved-tip {
  color: #67c23a;
  font-size: 12px;
  width: 40px;
}

.home-icon {
  cursor: pointer;
  font-size: 20px;
  color: #409eff;
  margin-right: 5px;
}

.home-icon:hover {
  color: #66b1ff;
}

.saved-tip-placeholder {
  width: 30px;
}

.breadcrumb-item {
  font-size: 14px;
  color: #606266;
}

.breadcrumb-clickable {
  cursor: pointer;
  color: #409eff;
}

.breadcrumb-clickable:hover {
  color: #66b1ff;
}

.title-input {
  font-size: 18px;
  font-weight: bold;
  width: 350px;
}

.header-right {
  display: flex;
  gap: 10px;
}

.folder-select {
  width: 150px;
}

.editor-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  padding: 10px;
  overflow: auto;
}

.vditor-editor {
  flex: 1;
  border: 1px solid #e4e7ed;
  border-radius: 4px;
  overflow: hidden;
}

.editor-footer {
  padding: 8px 15px;
  border-top: 1px solid #e4e7ed;
  display: flex;
  gap: 20px;
  font-size: 12px;
  color: #666;
}

.word-count,
.char-count {
  font-size: 12px;
  color: #999;
}

.share-tip {
  margin-bottom: 15px;
}

.v-time {
  color: #666;
  margin-bottom: 15px;
}

:deep(.vditor) {
  border: none;
  display: flex;
  flex-direction: column;
  height: 100%;
}

:deep(.vditor-toolbar) {
  border-bottom: 1px solid #e4e7ed;
}

:deep(.vditor-content) {
  flex: 1;
  background: white;
  overflow: auto;
}

:deep(.vditor-reset) {
  padding: 20px;
  font-size: 15px;
  line-height: 1.6;
}
</style>
