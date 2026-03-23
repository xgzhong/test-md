<template>
  <div class="main-layout" @click="handleOutsideClick">
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

        <div class="sidebar-section-header">
          <span class="sidebar-section-title">分类</span>
        </div>

        <div class="folder-list">
          <div
            v-for="folder in folders"
            :key="folder.id"
            class="folder-item"
            :class="{ active: currentFolder === folder.id }"
            @click="selectFolder(folder.id)"
          >
            <span class="folder-name">{{ folder.name }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- 编辑器区域 -->
    <div class="main-content" style="background: white;">
      <div class="content-header">
        <div style="display: flex; align-items: center; gap: 10px;">
          <span v-if="showSavedTip" style="color: #67c23a; font-size: 12px; width: 40px;">已保存</span>
          <span v-else style="width: 40px;"></span>
          <el-input
            v-model="noteTitle"
            placeholder="笔记标题"
            style="font-size: 18px; font-weight: bold; width: 350px;"
            @input="handleTitleInput"
          />
          <el-button type="primary" @click="manualSave">保存版本</el-button>
        </div>
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
          <div class="version-content">
            <div class="time">{{ formatDate(version.createdAt) }}</div>
            <div class="preview">{{ version.title }}</div>
          </div>
          <el-icon class="version-delete-icon" @click.stop="confirmDeleteVersion(version)"><Delete /></el-icon>
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
import { ref, reactive, onMounted, computed, watch, onBeforeUnmount } from 'vue'
import { useRouter, useRoute, onBeforeRouteLeave } from 'vue-router'
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
const isNewNote = ref(false)
const version = ref(0)
const folders = ref([])
const currentFolder = ref(null)
const versions = ref([])
const showVersions = ref(false)
const showShareDialog = ref(false)
const showVersionPreview = ref(false)
const shareUrl = ref('')
const selectedVersion = ref(null)
const hasUnsavedChanges = ref(false)
const originalContent = ref('')
const originalTitle = ref('')
const showSavedTip = ref(false)
let savedTipTimer = null

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
    version.value = res.note.version || 0

    // 记录原始内容，用于检测是否有未保存的更改
    originalTitle.value = res.note.title || ''
    originalContent.value = res.note.content || ''

    // 用数据版本判断是否是新建的笔记（0表示新建未保存，>=1表示已保存过）
    isNewNote.value = version.value === 0

    if (res.note.folderId) {
      currentFolder.value = res.note.folderId
    }

    hasUnsavedChanges.value = false
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

  // 如果是新建的空笔记且没有任何内容，不保存
  if (version.value === 0 && !noteTitle.value && !noteContent.value) {
    return
  }

  try {
    const res = await notesAPI.update(noteId.value, {
      title: noteTitle.value || '无标题笔记',
      content: noteContent.value,
      folderId: noteFolderId.value
    })
    // 保存成功后更新原始值和数据版本
    originalTitle.value = noteTitle.value || '无标题笔记'
    originalContent.value = noteContent.value
    version.value = res.note?.version || version.value + 1
    hasUnsavedChanges.value = false
    // 标记不再是新建笔记
    isNewNote.value = false
    // 显示"已保存"提示
    showSavedTip.value = true
    if (savedTipTimer) clearTimeout(savedTipTimer)
    savedTipTimer = setTimeout(() => {
      showSavedTip.value = false
    }, 2000)
  } catch (error) {
    ElMessage.error(error.message)
  }
}

// 手动保存，记录版本历史
const manualSave = async () => {
  if (!noteId.value) return

  try {
    const res = await notesAPI.update(noteId.value, {
      title: noteTitle.value || '无标题笔记',
      content: noteContent.value,
      folderId: noteFolderId.value,
      saveVersion: true
    })
    // 保存成功后更新原始值和数据版本
    originalTitle.value = noteTitle.value || '无标题笔记'
    originalContent.value = noteContent.value
    version.value = res.note?.version || version.value + 1
    hasUnsavedChanges.value = false
    // 标记不再是新建笔记
    isNewNote.value = false
    // 显示"已保存"提示
    showSavedTip.value = true
    if (savedTipTimer) clearTimeout(savedTipTimer)
    savedTipTimer = setTimeout(() => {
      showSavedTip.value = false
    }, 2000)
    ElMessage.success('保存成功')
  } catch (error) {
    ElMessage.error(error.message)
  }
}

const handleInput = () => {
  // 检测是否有未保存的更改
  hasUnsavedChanges.value = noteTitle.value !== originalTitle.value || noteContent.value !== originalContent.value

  // 防抖保存
  if (saveTimer) clearTimeout(saveTimer)
  saveTimer = setTimeout(() => {
    saveNote()
  }, 1000)
}

const handleTitleInput = () => {
  // 检测是否有未保存的更改
  hasUnsavedChanges.value = noteTitle.value !== originalTitle.value || noteContent.value !== originalContent.value
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

const confirmDeleteVersion = (version) => {
  ElMessageBox.confirm(`确定要删除该版本吗？`, '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(async () => {
    try {
      await notesAPI.deleteVersion(noteId.value, version.id)
      ElMessage.success('删除成功')
      loadVersions()
    } catch (error) {
      ElMessage.error(error.message)
    }
  }).catch(() => {})
}

// 统一的离开确认逻辑
const confirmBeforeLeave = async () => {
  // 如果是新建的笔记（包括空笔记和工作日志）且没有编辑，直接删除
  if (version.value === 0) {
    const hasChanges = noteTitle.value !== originalTitle.value || noteContent.value !== originalContent.value
    if (!hasChanges) {
      try {
        await notesAPI.delete(noteId.value)
      } catch (error) {
        // 忽略删除错误
      }
      return true
    }
  }

  // 如果有未保存的更改，提示用户
  const currentHasChanges = noteTitle.value !== originalTitle.value || noteContent.value !== originalContent.value
  if (currentHasChanges || hasUnsavedChanges.value) {
    return new Promise((resolve) => {
      ElMessageBox.confirm('您有未保存的更改，确定要离开吗？', '提示', {
        confirmButtonText: '保存并离开',
        cancelButtonText: '不保存',
        distinguishCancelAndClose: true,
        type: 'warning'
      }).then(async () => {
        // 保存并离开
        await saveNote()
        resolve(true)
      }).catch(async (action) => {
        if (action === 'cancel') {
          // 不保存，直接离开，如果是新建的笔记则删除
          if (version.value === 0) {
            try {
              await notesAPI.delete(noteId.value)
            } catch (error) {
              // 忽略删除错误
            }
          }
          resolve(true)
        }
        // close 被点击时留在页面
        resolve(false)
      })
    })
  }

  return true
}

const selectFolder = (folderId) => {
  // 检查是否有未保存的更改
  const currentHasChanges = noteTitle.value !== originalTitle.value || noteContent.value !== originalContent.value || hasUnsavedChanges.value

  if (!currentHasChanges) {
    // 没有更改，直接跳转
    // 如果是新建的笔记且没有内容则删除
    if (version.value === 0 && !noteTitle.value && !noteContent.value) {
      notesAPI.delete(noteId.value).catch(() => {})
    }
    router.push('/home')
    return
  }

  // 有更改，弹出确认框
  ElMessageBox.confirm('您有未保存的更改，确定要离开吗？', '提示', {
    confirmButtonText: '保存并离开',
    cancelButtonText: '不保存',
    distinguishCancelAndClose: true,
    type: 'warning'
  }).then(async () => {
    // 保存并离开
    await saveNote()
    router.push('/home')
  }).catch((action) => {
    if (action === 'cancel') {
      // 不保存，直接离开，如果是新建的笔记则删除
      if (version.value === 0) {
        notesAPI.delete(noteId.value).catch(() => {})
      }
      router.push('/home')
    }
    // close 或其他 action 留在当前页面
  })
}

const goHome = () => {
  // 检查是否有未保存的更改
  const currentHasChanges = noteTitle.value !== originalTitle.value || noteContent.value !== originalContent.value || hasUnsavedChanges.value

  if (!currentHasChanges) {
    // 没有更改，直接跳转
    // 如果是新建的笔记且没有内容则删除
    if (version.value === 0 && !noteTitle.value && !noteContent.value) {
      notesAPI.delete(noteId.value).catch(() => {})
    }
    router.push('/home')
    return
  }

  // 有更改，弹出确认框
  ElMessageBox.confirm('您有未保存的更改，确定要离开吗？', '提示', {
    confirmButtonText: '保存并离开',
    cancelButtonText: '不保存',
    distinguishCancelAndClose: true,
    type: 'warning'
  }).then(async () => {
    // 保存并离开
    await saveNote()
    router.push('/home')
  }).catch((action) => {
    if (action === 'cancel') {
      // 不保存，直接离开，如果是新建的笔记则删除
      if (version.value === 0) {
        notesAPI.delete(noteId.value).catch(() => {})
      }
      router.push('/home')
    }
    // close 或其他 action 留在当前页面
  })
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

// 离开页面前的确认（用于浏览器后退按钮）
onBeforeRouteLeave(async () => {
  return await confirmBeforeLeave()
})

const handleOutsideClick = (event) => {
  if (!showVersions.value) return
  const versionsPanel = document.querySelector('.versions-panel')
  const contentHeader = document.querySelector('.content-header')
  if (versionsPanel && !versionsPanel.contains(event.target) && contentHeader && !contentHeader.contains(event.target)) {
    showVersions.value = false
  }
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
