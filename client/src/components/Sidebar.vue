<template>
  <div class="sidebar" :class="{ collapsed: collapsed }" :style="{ width: collapsed ? '50px' : sidebarWidth + 'px' }">
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
    <div
      v-if="!collapsed"
      class="sidebar-resize-handle"
      @mousedown="startResize"
    ></div>
    <div class="sidebar-menu" v-show="!collapsed">
      <!-- 全部笔记 -->
      <div
        class="menu-item"
        :class="{ active: currentFolder === null }"
        @click="$emit('select', null)"
      >
        <span class="menu-item-text">全部笔记</span>
        <el-tag v-if="totalNotes !== undefined" size="small">{{ totalNotes }}</el-tag>
      </div>
      <!-- 未分类 -->
      <div
        class="menu-item"
        :class="{ active: currentFolder === 'uncategorized' }"
        @click="$emit('select', 'uncategorized')"
      >
        <span class="menu-item-text">未分类</span>
        <el-tag v-if="uncategorizedCount !== undefined" size="small">{{ uncategorizedCount }}</el-tag>
      </div>

      <!-- 分类标题 -->
      <div class="sidebar-section-header">
        <div class="section-title-group">
          <el-icon class="section-title-icon"><Folder /></el-icon>
          <span class="sidebar-section-title">分类</span>
        </div>
        <el-button
          v-if="showAddFolder"
          class="add-folder-btn"
          text
          size="small"
          @click="$emit('addFolder')"
        >
          <el-icon><Plus /></el-icon>
          <span>新增分类</span>
        </el-button>
      </div>

      <!-- 分类列表（树形结构） -->
      <div class="folder-list" @dragover.prevent @drop="onDropToRoot">
        <template v-for="folder in folders" :key="folder.id">
          <FolderTreeItem
            :folder="folder"
            :notes="notes"
            :currentFolder="currentFolder"
            :level="0"
            :expandedKeys="expandedKeys"
            :showMore="true"
            :dragOverFolder="dragOverFolder"
            :draggedFolder="draggedFolder"
            :hoverSide="hoverSide"
            :hoverPosition="hoverPosition"
            @select="$emit('select', $event)"
            @openNote="$emit('openNote', $event)"
            @toggleExpand="toggleExpand"
            @pin="$emit('pin', $event)"
            @edit="$emit('edit', $event)"
            @delete="$emit('delete', $event)"
            @addChildFolder="$emit('addChildFolder', $event)"
            @addNote="$emit('addNote', $event)"
            @dragStart="onDragStart"
            @dragOver="onDragOver"
            @drag-leave="onDragLeave"
            @drop="onDrop"
          />
        </template>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import FolderTreeItem from './FolderTreeItem.vue'

const props = defineProps({
  title: {
    type: String,
    default: 'Markdown 笔记'
  },
  collapsed: {
    type: Boolean,
    default: false
  },
  width: {
    type: Number,
    default: 380
  },
  folders: {
    type: Array,
    default: () => []
  },
  notes: {
    type: Array,
    default: () => []
  },
  currentFolder: {
    type: [Number, String, null],
    default: null
  },
  totalNotes: {
    type: Number,
    default: undefined
  },
  uncategorizedCount: {
    type: Number,
    default: undefined
  },
  showAddFolder: {
    type: Boolean,
    default: true
  },
  draggedFolder: {
    type: Object,
    default: null
  },
  dragOverFolder: {
    type: Object,
    default: null
  },
  isLevelChange: {
    type: Boolean,
    default: false
  },
  hoverSide: {
    type: String,
    default: 'child'
  },
  hoverPosition: {
    type: String,
    default: 'below'
  }
})

const emit = defineEmits([
  'toggle',
  'select',
  'addFolder',
  'pin',
  'edit',
  'delete',
  'dragStart',
  'dragOver',
  'dragleave',
  'drop',
  'update:width',
  'titleClick'
])

// 展开状态 - 使用对象替代 Set 以便 Vue 更好地追踪变化
const expandedKeys = ref({})

// 侧边栏宽度拖动调整
const MIN_WIDTH = 200
const MAX_WIDTH = 600
const sidebarWidth = ref(props.width)
let isResizing = false
let startX = 0
let startWidth = 0

const startResize = (e) => {
  isResizing = true
  startX = e.clientX
  startWidth = sidebarWidth.value
  document.addEventListener('mousemove', onResize)
  document.addEventListener('mouseup', stopResize)
  document.body.style.cursor = 'col-resize'
  document.body.style.userSelect = 'none'
}

const onResize = (e) => {
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

const toggleExpand = (folderId) => {
  expandedKeys.value = {
    ...expandedKeys.value,
    [folderId]: !expandedKeys.value[folderId]
  }
}

const onDragStart = (event, folder) => {
  emit('dragStart', event, folder)
  event.dataTransfer.effectAllowed = 'move'
}

const onDragOver = (event, folder) => {
  emit('dragOver', event, folder)
  event.dataTransfer.dropEffect = 'move'
}

const onDragLeave = () => {
  emit('dragleave')
}

const onDrop = (event, targetFolder) => {
  emit('drop', event, targetFolder)
}

// 拖拽到根区域
const onDropToRoot = (event) => {
  emit('drop', event, null)
}
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

.sidebar.collapsed {
  width: 50px;
}

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

.sidebar-resize-handle:hover {
  background: #409eff;
}

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

.sidebar-header h2:hover {
  color: #409eff;
}

.sidebar-toggle {
  position: absolute;
  top: 25px;
  right: 0;
  cursor: pointer;
  padding: 5px;
  color: #909399;
  z-index: 10;
}

.sidebar-toggle:hover {
  color: #409eff;
}

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

.menu-item:hover {
  background: #ecf5ff;
}

.menu-item.active {
  background: #e6f0ff;
  color: #409eff;
}

.menu-item-text {
  flex: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.sidebar-section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 15px 15px 10px;
}

.sidebar-section-title {
  font-weight: bold;
  color: #666;
  font-size: 14px;
}

.section-title-icon {
  margin-right: 6px;
  color: #909399;
  font-size: 14px;
}

.section-title-group {
  display: flex;
  align-items: center;
}

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

.add-folder-btn:hover {
  color: #409eff;
  background: rgba(64, 158, 255, 0.1);
}

.folder-list {
  padding: 0 10px;
}

.folder-item {
  display: flex;
  align-items: center;
  padding: 10px 8px;
  cursor: pointer;
  border-radius: 4px;
  margin-bottom: 2px;
  transition: background 0.2s;
}

.folder-item:hover {
  background: #ecf5ff;
}

.folder-item.active {
  background: #e6f0ff;
  color: #409eff;
}

.folder-item.pinned {
  background: #fdf6ec;
}

.folder-index {
  color: #909399;
  margin-right: 5px;
  font-size: 12px;
  min-width: 20px;
}

.folder-name {
  flex: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  font-size: 14px;
}

.folder-pin-icon {
  color: #e6a23c;
  margin-right: 5px;
}
</style>
