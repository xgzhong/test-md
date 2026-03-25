<template>
  <div class="folder-tree-item">
    <!-- 放置指示线 - 显示在同级分类的上方 -->
    <div v-if="isDragOver && hoverSide === 'sibling' && isAbove" class="drop-indicator"></div>

    <div
      class="folder-item"
      :class="[
        { active: currentFolder === folder.id, pinned: folder.isPinned, 'drag-over': isDragOver },
        dragOverClass
      ]"
      :style="{ paddingLeft: (level * 20 + 8) + 'px' }"
      draggable="true"
      @click.stop="handleFolderClick"
      @dragstart="onDragStart($event, folder)"
      @dragover.prevent="onDragOver($event, folder)"
      @dragleave="onDragLeave($event)"
      @drop="onDrop($event, folder)"
    >
      <!-- 展开/折叠按钮 -->
      <el-icon class="folder-expand-icon" @click.stop="handleToggleExpand">
        <ArrowRight v-if="!isExpanded" />
        <ArrowDown v-else />
      </el-icon>

      <el-icon v-if="folder.isPinned" class="folder-pin-icon"><Star /></el-icon>
      <el-icon v-else class="folder-icon"><Folder /></el-icon>
      <el-tooltip :content="folder.name" placement="top" :disabled="folder.name.length <= 10">
        <span class="folder-name">{{ folder.name }}</span>
      </el-tooltip>

      <!-- 更多操作下拉菜单 -->
      <el-dropdown trigger="click" @command="handleCommand" v-if="showMore">
        <span class="folder-more-trigger" @click.stop>
          <el-icon class="folder-more-icon">
            <MoreFilled />
          </el-icon>
        </span>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item :command="'addChildFolder'">
              <el-icon><FolderAdd /></el-icon>
              新建子级分类
            </el-dropdown-item>
            <el-dropdown-item :command="'addNote'">
              <el-icon><DocumentAdd /></el-icon>
              新建笔记
            </el-dropdown-item>
            <el-dropdown-item :command="'pin'" divided>
              <el-icon><Star /></el-icon>
              {{ folder.isPinned ? '取消置顶' : '置顶' }}
            </el-dropdown-item>
            <el-dropdown-item :command="'edit'">
              <el-icon><Edit /></el-icon>
              修改分类
            </el-dropdown-item>
            <el-dropdown-item :command="'delete'" divided>
              <el-icon><Delete /></el-icon>
              删除分类
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>

      <!-- 笔记数量 -->
      <el-tag v-if="folder.noteCount !== undefined" size="small" class="folder-note-count">{{ folder.noteCount }}</el-tag>
    </div>

    <!-- 放置指示线 - 显示在同级分类的下方 -->
    <div v-if="isDragOver && hoverSide === 'sibling' && !isAbove" class="drop-indicator"></div>

    <!-- 子分类 -->
    <template v-if="folder.children && folder.children.length > 0 && isExpanded">
      <FolderTreeItem
        v-for="child in folder.children"
        :key="child.id"
        :folder="child"
        :notes="notes"
        :currentFolder="currentFolder"
        :level="level + 1"
        :expandedKeys="expandedKeys"
        :showMore="showMore"
        :dragOverFolder="dragOverFolder"
        :hoverSide="hoverSide"
        :hoverPosition="hoverPosition"
        @select="$emit('select', $event)"
        @openNote="$emit('openNote', $event)"
        @toggleExpand="$emit('toggleExpand', $event)"
        @pin="$emit('pin', $event)"
        @edit="$emit('edit', $event)"
        @delete="$emit('delete', $event)"
        @addChildFolder="$emit('addChildFolder', $event)"
        @addNote="$emit('addNote', $event)"
        @dragStart="$emit('dragStart', $event, child)"
        @dragOver="$emit('dragOver', $event, child)"
        @dragleave="$emit('dragleave', $event)"
        @drop="$emit('drop', $event, child)"
      />
    </template>

    <!-- 该分类下的笔记列表 -->
    <template v-if="isExpanded && folderNotes.length > 0">
      <div
        v-for="note in folderNotes"
        :key="note.id"
        class="note-item"
        :class="{ active: currentNoteId === note.id }"
        :style="{ paddingLeft: (level * 20 + 36) + 'px' }"
        @click.stop="$emit('openNote', note.id)"
      >
        <el-icon class="note-icon"><Document /></el-icon>
        <span class="note-title">{{ note.title || '无标题笔记' }}</span>
      </div>
    </template>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  folder: {
    type: Object,
    required: true
  },
  notes: {
    type: Array,
    default: () => []
  },
  currentFolder: {
    type: [Number, String, null],
    default: null
  },
  level: {
    type: Number,
    default: 0
  },
  expandedKeys: {
    type: Object,
    required: true
  },
  showMore: {
    type: Boolean,
    default: true
  },
  dragOverFolder: {
    type: Object,
    default: null
  },
  draggedFolder: {
    type: Object,
    default: null
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
  'select',
  'openNote',
  'toggleExpand',
  'pin',
  'edit',
  'delete',
  'addChildFolder',
  'addNote',
  'dragStart',
  'dragOver',
  'dragleave',
  'drop'
])

// 筛选属于当前分类的笔记
const folderNotes = computed(() => {
  return props.notes.filter(note => note.folderId === props.folder.id)
})

// 当前选中的笔记ID
const currentNoteId = computed(() => {
  return props.currentFolder
})

const isExpanded = computed(() => !!props.expandedKeys[props.folder.id])

// 处理点击：切换展开/折叠状态
const handleToggleExpand = () => {
  emit('toggleExpand', props.folder.id)
}

// 处理文件夹点击：导航到文件夹详情页
const handleFolderClick = () => {
  emit('select', props.folder.id)
}

const isDragOver = computed(() => {
  if (!props.dragOverFolder) return false
  return props.dragOverFolder.id === props.folder.id
})

const dragOverClass = computed(() => {
  if (!isDragOver.value) return ''
  return props.hoverSide === 'sibling' ? 'drag-over-sibling' : 'drag-over-child'
})

const isAbove = computed(() => {
  return props.hoverPosition === 'above'
})

const handleCommand = (command) => {
  switch (command) {
    case 'addChildFolder':
      emit('addChildFolder', props.folder)
      break
    case 'addNote':
      emit('addNote', props.folder)
      break
    case 'pin':
      emit('pin', props.folder)
      break
    case 'edit':
      emit('edit', props.folder)
      break
    case 'delete':
      emit('delete', props.folder)
      break
  }
}

const onDragStart = (event, folder) => {
  emit('dragStart', event, folder)
}

const onDragOver = (event, folder) => {
  emit('dragOver', event, folder)
}

const onDragLeave = (event) => {
  emit('dragleave', event)
}

const onDrop = (event, folder) => {
  emit('drop', event, folder)
}
</script>

<style scoped>
.folder-item {
  display: flex;
  align-items: center;
  padding: 10px 8px;
  cursor: pointer;
  border-radius: 4px;
  border: 2px solid transparent;
  margin-bottom: 2px;
  transition: background 0.2s, border-color 0.2s;
}

.folder-item:hover {
  background: #ecf5ff;
}

.folder-item.active {
  background: #e6f0ff;
  color: #409eff;
}

.folder-item.active .folder-icon {
  color: #409eff;
}

.folder-item.drag-over-sibling {
  border: 2px solid #409eff !important;
  border-left-width: 4px !important;
  background: #ecf5ff !important;
}

.folder-item.drag-over-child {
  border: 2px solid #e6a23c !important;
  border-right-width: 4px !important;
  background: #fdf6ec !important;
}

.folder-item.drag-over {
  background: #fff3e0 !important;
  border: 2px dashed #ff9800 !important;
}

.drop-indicator {
  height: 3px;
  background: #409eff;
  border-radius: 2px;
  margin: 2px 0;
}

.folder-item.pinned {
  background: #fdf6ec;
}

.folder-expand-icon {
  margin-right: 5px;
  cursor: pointer;
  color: #909399;
  font-size: 12px;
  transition: transform 0.2s;
}

.folder-expand-icon:hover {
  color: #409eff;
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

.folder-icon {
  margin-right: 6px;
  color: #909399;
  font-size: 14px;
}

.folder-more-icon {
  cursor: pointer;
  opacity: 0;
  transition: opacity 0.2s;
  color: #909399;
  font-size: 16px;
}

.folder-item:hover .folder-more-icon {
  opacity: 1;
}

.folder-more-icon:hover {
  color: #409eff;
}

.folder-more-trigger {
  display: inline-flex;
  align-items: center;
  margin-left: 12px;
}

.folder-note-count {
  margin-left: 12px;
}

.note-item {
  display: flex;
  align-items: center;
  padding: 8px 8px;
  cursor: pointer;
  border-radius: 4px;
  margin: 2px 0;
  transition: background 0.2s;
}

.note-item:hover {
  background: #f5f7fa;
}

.note-item.active {
  background: #e6f0ff;
  color: #409eff;
}

.note-icon {
  margin-right: 6px;
  color: #909399;
  font-size: 13px;
}

.note-title {
  flex: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  font-size: 13px;
  color: #606266;
}

.note-item:hover .note-icon {
  color: #409eff;
}

.note-item:hover .note-title {
  color: #409eff;
}
</style>
