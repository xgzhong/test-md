<template>
  <div class="sidebar" :class="{ collapsed }" :style="{ width: collapsed ? '50px' : sidebarWidth + 'px' }">
    <div class="sidebar-header" @click="$emit('titleClick')">
      <slot name="header">
        <h2 v-if="!collapsed" style="cursor: pointer;">{{ title }}</h2>
      </slot>
    </div>

    <!-- 折叠按钮 -->
    <div class="sidebar-toggle" @click="$emit('toggle')">
      <el-icon v-if="collapsed"><Expand /></el-icon>
      <el-icon v-else><Fold /></el-icon>
    </div>

    <!-- 拖动调整宽度手柄 -->
    <div v-if="!collapsed" class="sidebar-resize-handle" @mousedown="startResize"></div>

    <div class="sidebar-menu" v-show="!collapsed">
      <!-- 全部笔记 -->
      <div class="menu-item" :class="{ active: currentFolder === null }" @click="$emit('select', null)">
        <span class="menu-item-text">全部笔记</span>
        <el-tag v-if="props.totalNotes !== undefined" size="small">{{ props.totalNotes }}</el-tag>
      </div>

      <!-- 未分类 -->
      <div class="menu-item" :class="{ active: currentFolder === 'uncategorized' }" @click="$emit('select', 'uncategorized')">
        <span class="menu-item-text">未分类</span>
        <el-tag v-if="props.uncategorizedCount !== undefined" size="small">{{ props.uncategorizedCount }}</el-tag>
      </div>

      <!-- 分类标题 -->
      <div class="sidebar-section-header">
        <div class="section-title-group">
          <el-icon class="section-title-icon"><Folder /></el-icon>
          <span class="sidebar-section-title">分类</span>
        </div>
        <el-button v-if="showAddFolder" class="add-folder-btn" text size="small" @click="handleAddFolder">
          <el-icon><Plus /></el-icon>
          <span>新增分类</span>
        </el-button>
      </div>

      <!-- 分类列表（树形结构） -->
      <div class="folder-list" @dragover.prevent @drop="onDropToRoot">
        <template v-for="folder in sidebar.rootFolders.value" :key="folder.id">
          <FolderTreeItem
            :folder="folder"
            :notesByFolderId="notesByFolderId"
            :currentFolder="currentFolder"
            :level="0"
            :expandedKeys="sidebar.expandedKeys.value"
            :showMore="true"
            :dragOverFolder="sidebar.dragOverFolder.value"
            :draggedFolder="sidebar.draggedFolder.value"
            :hoverSide="sidebar.hoverSide.value"
            :hoverPosition="sidebar.hoverPosition.value"
            @select="handleSelect"
            @openNote="(id) => $emit('openNote', id)"
            @toggleExpand="(id) => sidebar.toggleExpand(id)"
            @pin="(folder) => handlePin(folder)"
            @edit="(folder) => $emit('edit', folder)"
            @delete="(folder) => $emit('delete', folder)"
            @addChildFolder="(folder) => $emit('addChildFolder', folder)"
            @addNote="(folder) => $emit('addNote', folder)"
            @dragStart="(e, f) => sidebar.onDragStart(e, f)"
            @dragOver="(e, f) => sidebar.onDragOver(e, f)"
            @dragleave="sidebar.onDragLeave"
            @drop="(e, f) => sidebar.onDrop(e, f)"
          />
        </template>
      </div>
    </div>

    <!-- 新建分类对话框 -->
    <el-dialog v-model="showFolderDialog" title="新建分类" width="400px" @opened="folderInputRef?.focus()" @close="resetFolderForm">
      <el-form @submit.prevent="submitCreateFolder">
        <el-form-item label="上级分类">
          <el-select v-model="folderForm.parentId" placeholder="顶级分类" clearable style="width: 100%">
            <el-option v-for="folder in sidebar.rootFolders.value" :key="folder.id" :label="'└ ' + folder.name" :value="folder.id" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-input ref="folderInputRef" v-model="folderForm.name" placeholder="分类名称" @keyup.enter="submitCreateFolder" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showFolderDialog = false">取消</el-button>
        <el-button type="primary" @click="submitCreateFolder">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import FolderTreeItem from './FolderTreeItem.vue'
import { useSidebar } from '../composables/useSidebar'
import type { Folder, Note } from '../api'

const props = defineProps({
  title: { type: String, default: 'Markdown 笔记' },
  collapsed: { type: Boolean, default: false },
  width: { type: Number, default: 380 },
  folders: { type: Array, default: () => [] },
  notes: { type: Array as () => Note[], default: () => [] as Note[] },
  currentFolder: { type: [Number, String, null], default: null },
  totalNotes: { type: Number, default: undefined },
  uncategorizedCount: { type: Number, default: undefined },
  showAddFolder: { type: Boolean, default: true },
  autoLoad: { type: Boolean, default: true } // 是否自动加载数据
})

const emit = defineEmits([
  'toggle',
  'select',
  'addFolder',
  'pin',
  'edit',
  'delete',
  'openNote',
  'drop',
  'update:width',
  'titleClick',
  'addChildFolder',
  'addNote',
  'loaded'
])

// 使用 sidebar composable
const sidebar = useSidebar()

// 同步外部传入的 folders 和 notes
watch(() => props.folders, (val) => {
  if (val && val.length > 0) {
    sidebar.folders.value = val as Folder[]
  }
}, { immediate: true })

watch(() => props.notes, (val) => {
  if (val) {
    sidebar.notes.value = val as Note[]
  }
}, { immediate: true })

// Pre-compute notes grouped by folderId for O(1) lookup
const notesByFolderId = computed(() => {
  const map = new Map<string | number, typeof props.notes>()
  for (const note of props.notes) {
    const fid = note.folderId === null || note.folderId === undefined ? 'uncategorized' : String(note.folderId)
    if (!map.has(fid)) map.set(fid, [])
    map.get(fid)!.push(note)
  }
  return map
})

watch(() => props.uncategorizedCount, (val) => {
  if (val !== undefined) {
    sidebar.uncategorizedCount.value = val
  }
}, { immediate: true })

// 自动加载数据（仅在没有外部传入 folders/notes 时）
onMounted(async () => {
  if (props.autoLoad && (!props.folders || props.folders.length === 0)) {
    await sidebar.loadAll()
    emit('loaded', { folders: sidebar.folders.value, notes: sidebar.notes.value })
  }
})

// 侧边栏宽度拖动调整
const MIN_WIDTH = 200
const MAX_WIDTH = 600
const sidebarWidth = ref(props.width)
let isResizing = false
let startX = 0
let startWidth = 0

const startResize = (e: MouseEvent) => {
  isResizing = true
  startX = e.clientX
  startWidth = sidebarWidth.value
  document.addEventListener('mousemove', onResize)
  document.addEventListener('mouseup', stopResize)
  document.body.style.cursor = 'col-resize'
  document.body.style.userSelect = 'none'
}

const onResize = (e: MouseEvent) => {
  if (!isResizing) return
  const delta = e.clientX - startX
  const newWidth = Math.min(MAX_WIDTH, Math.max(MIN_WIDTH, startWidth + delta))
  sidebarWidth.value = newWidth
}

const stopResize = () => {
  if (isResizing) {
    isResizing = false
    document.removeEventListener('mousemove', onResize)
    document.removeEventListener('mouseup', stopResize)
    document.body.style.cursor = ''
    document.body.style.userSelect = ''
    emit('update:width', sidebarWidth.value)
  }
}

// 新建分类
const showFolderDialog = ref(false)
const folderInputRef = ref<HTMLInputElement | null>(null)
const folderForm = ref({ name: '', parentId: null as number | null })

const handleAddFolder = () => {
  folderForm.value = { name: '', parentId: null }
  showFolderDialog.value = true
}

const resetFolderForm = () => {
  folderForm.value = { name: '', parentId: null }
}

const submitCreateFolder = async () => {
  if (!folderForm.value.name.trim()) {
    ElMessage.warning('请输入分类名称')
    return
  }
  await sidebar.createFolder(folderForm.value.name, folderForm.value.parentId)
  showFolderDialog.value = false
  resetFolderForm()
}

// 处理选择
const handleSelect = (folderId: number | string) => {
  emit('select', folderId)
}

// 处理置顶
const handlePin = async (folder: Folder) => {
  await sidebar.togglePinFolder(folder.id)
}

// 拖拽到根区域
const onDropToRoot = (event: DragEvent) => {
  sidebar.onDrop(event, null)
}

// 暴露方法供外部调用
defineExpose({
  // 数据加载
  loadAll: () => sidebar.loadAll(),
  loadFolders: () => sidebar.loadFolders(),
  loadNotes: () => sidebar.loadNotes(),

  // 分类操作
  createFolder: (name: string, parentId?: number | null) => sidebar.createFolder(name, parentId),
  updateFolder: (id: number, name: string, parentId?: number | null) => sidebar.updateFolder(id, name, parentId),
  deleteFolder: (id: number) => sidebar.deleteFolder(id),
  togglePinFolder: (id: number) => sidebar.togglePinFolder(id),

  // 笔记操作
  createNote: (title?: string, content?: string, folderId?: number | null) => sidebar.createNote(title, content, folderId),
  deleteNote: (id: number) => sidebar.deleteNote(id),

  // 状态
  folders: sidebar.folders,
  notes: sidebar.notes,
  uncategorizedCount: sidebar.uncategorizedCount
})
</script>

<style scoped>
.sidebar {
  height: 100%;
  background: #f5f7fa;
  border-right: 1px solid #e4e7ed;
  display: flex;
  flex-direction: column;
  position: relative;
  overflow: hidden;
  flex-shrink: 0;
}

.sidebar.collapsed { width: 50px; }

.sidebar-resize-handle {
  position: absolute;
  top: 0;
  right: 0;
  width: 5px;
  height: 100%;
  cursor: col-resize;
  background: transparent;
  z-index: 20;
  transition: background 0.2s;
}

.sidebar-resize-handle:hover { background: #409eff; }

.sidebar-header {
  padding: 20px 15px;
  border-bottom: 1px solid #e4e7ed;
}

.sidebar-header h2 {
  margin: 0;
  font-size: 18px;
  color: #303133;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.sidebar-header h2:hover { color: #409eff; }

.sidebar-toggle {
  position: absolute;
  top: 25px;
  right: 0;
  cursor: pointer;
  padding: 5px;
  color: #909399;
  z-index: 10;
}

.sidebar-toggle:hover { color: #409eff; }

.sidebar-menu {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  padding: 10px 0;
}

.menu-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 15px;
  cursor: pointer;
  transition: background 0.2s;
}

.menu-item:hover { background: #ecf5ff; }
.menu-item.active { background: #e6f0ff; color: #409eff; }
.menu-item-text { flex: 1; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

.sidebar-section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 15px 15px 10px;
}

.sidebar-section-title { font-weight: bold; color: #666; font-size: 14px; }
.section-title-icon { margin-right: 6px; color: #909399; font-size: 14px; }
.section-title-group { display: flex; align-items: center; }

.add-folder-btn {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 13px;
  padding: 4px 8px;
  border-radius: 4px;
  color: #909399;
  transition: all 0.2s ease;
}

.add-folder-btn:hover { color: #409eff; background: rgba(64, 158, 255, 0.1); }

.folder-list { padding: 0 10px; }
</style>