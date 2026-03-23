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

      <!-- 分类列表 -->
      <div class="folder-list">
        <div
          v-for="(folder, index) in folders"
          :key="folder.id"
          class="folder-item"
          :class="{ active: currentFolder === folder.id, pinned: folder.isPinned }"
          draggable="true"
          @click="$emit('select', folder.id)"
          @dragstart="onDragStart($event, index)"
          @dragover.prevent="onDragOver($event, index)"
          @drop="onDrop($event, index)"
        >
          <el-icon v-if="folder.isPinned" class="folder-pin-icon"><Star /></el-icon>
          <span v-else class="folder-index">{{ index + 1 }}.</span>
          <el-tooltip :content="folder.name" placement="top" :disabled="folder.name.length <= 10">
            <span class="folder-name">{{ folder.name }}</span>
          </el-tooltip>
          <el-tag v-if="folder.noteCount !== undefined" size="small">{{ folder.noteCount }}</el-tag>
          <el-icon v-if="showPin" class="folder-pin-btn" @click.stop="$emit('pin', folder)">
            <span v-if="folder.isPinned">&#x2605;</span>
            <span v-else>&#x2606;</span>
          </el-icon>
          <el-icon v-if="showEdit" class="folder-edit-icon" @click.stop="$emit('edit', folder)"><Edit /></el-icon>
          <el-icon v-if="showDelete" class="folder-delete-icon" @click.stop="$emit('delete', folder)"><Delete /></el-icon>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'

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
  showPin: {
    type: Boolean,
    default: true
  },
  showEdit: {
    type: Boolean,
    default: true
  },
  showDelete: {
    type: Boolean,
    default: true
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
  'drop'
])

const draggedIndex = ref(null)

const onDragStart = (event, index) => {
  draggedIndex.value = index
  event.dataTransfer.effectAllowed = 'move'
}

const onDragOver = (event, index) => {
  event.dataTransfer.dropEffect = 'move'
}

const onDrop = (event, index) => {
  emit('drop', draggedIndex.value, index)
  draggedIndex.value = null
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

.folder-pin-btn,
.folder-edit-icon,
.folder-delete-icon {
  margin-left: 5px;
  cursor: pointer;
  opacity: 0;
  transition: opacity 0.2s;
  color: #909399;
}

.folder-item:hover .folder-pin-btn,
.folder-item:hover .folder-edit-icon,
.folder-item:hover .folder-delete-icon {
  opacity: 1;
}

.folder-pin-btn:hover {
  color: #e6a23c;
}

.folder-edit-icon:hover {
  color: #409eff;
}

.folder-delete-icon:hover {
  color: #f56c6c;
}
</style>
