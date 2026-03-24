<template>
  <div class="sidebar" :class="{ collapsed: collapsed }">
    <div class="sidebar-header">
      <slot name="header">
        <h2 v-if="!collapsed">{{ title }}</h2>
      </slot>
    </div>
    <!-- 折叠按钮 -->
    <div class="sidebar-toggle" @click="$emit('toggle')">
      <el-icon v-if="collapsed"><Expand /></el-icon>
      <el-icon v-else><Fold /></el-icon>
    </div>
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
        <span class="sidebar-section-title">分类</span>
        <el-button v-if="showAddFolder" class="add-folder-btn" size="small" text @click="$emit('addFolder')">
          <el-icon><Plus /></el-icon>
        </el-button>
      </div>

      <!-- 分类列表（树形结构） -->
      <div class="folder-list" @dragover.prevent @drop="onDropToRoot">
        <template v-for="folder in folders" :key="folder.id">
          <FolderTreeItem
            :folder="folder"
            :currentFolder="currentFolder"
            :level="0"
            :expandedKeys="expandedKeys"
            :showMore="true"
            :dragOverFolder="dragOverFolder"
            :draggedFolder="draggedFolder"
            :hoverSide="hoverSide"
            :hoverPosition="hoverPosition"
            @select="$emit('select', $event)"
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
  folders: {
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
  'drop'
])

// 展开状态 - 使用对象替代 Set 以便 Vue 更好地追踪变化
const expandedKeys = ref({})

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
  width: 380px;
  background: #f5f7fa;
  border-right: 1px solid #e4e7ed;
  display: flex;
  flex-direction: column;
  transition: width 0.3s;
  position: relative;
}

.sidebar.collapsed {
  width: 50px;
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

.sidebar-toggle {
  position: absolute;
  top: 25px;
  right: 10px;
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

.add-folder-btn {
  padding: 0;
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
