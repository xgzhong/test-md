import { ref, computed, type Ref, type ComputedRef } from 'vue'
import { ElMessage } from 'element-plus'
import { notesAPI, foldersAPI, type Folder, type Note } from '../api'
import { getParentIdStr, isRootId, isSameParentId, isDescendant, flattenFolders } from './useCommon'

/**
 * Sidebar Composable - 封装侧边栏所有逻辑
 * 支持独立使用或配合 Sidebar 组件
 */
export function useSidebar() {
  // ============== State ==============
  const folders = ref<Folder[]>([])
  const notes = ref<Note[]>([])
  const uncategorizedCount = ref(0)
  const isLoading = ref(false)
  const saving = ref(false)

  // 拖拽状态
  const draggedFolder = ref<Folder | null>(null)
  const dragOverFolder = ref<Folder | null>(null)
  const hoverSide = ref<'sibling' | 'child'>('child')
  const hoverPosition = ref<'above' | 'below'>('below')

  // 展开状态
  const expandedKeys = ref<Record<number | string, boolean>>({})

  // ============== Computed ==============
  const isLevelChange = computed(() => {
    if (!draggedFolder.value || !dragOverFolder.value) return false
    const draggedPid = draggedFolder.value.parentId ? String(draggedFolder.value.parentId) : '0'
    return draggedPid !== String(dragOverFolder.value.id)
  })

  const rootFolders = computed(() => folders.value.filter(f => Number(f.parentId) === 0))

  // ============== Drag-Drop Helpers ==============
  const DRAG_SIBLING_THRESHOLD = 0.35

  // ============== Actions ==============

  // 加载分类列表
  const loadFolders = async () => {
    isLoading.value = true
    try {
      const res = await foldersAPI.getFolders()
      folders.value = res.folders || []
      uncategorizedCount.value = res.uncategorizedCount || 0
    } catch (error: unknown) {
      ElMessage.error(error instanceof Error ? error.message : '加载分类失败')
    } finally {
      isLoading.value = false
    }
  }

  // 加载笔记列表
  const loadNotes = async (params: Record<string, string | number | undefined> = {}) => {
    try {
      const res = await notesAPI.getNotes(params)
      notes.value = res.notes || []
    } catch (error: unknown) {
      ElMessage.error(error instanceof Error ? error.message : '加载笔记失败')
    }
  }

  // 加载所有数据
  const loadAll = async () => {
    await Promise.all([loadFolders(), loadNotes()])
  }

  // 展开/折叠
  const toggleExpand = (folderId: number | string) => {
    expandedKeys.value = {
      ...expandedKeys.value,
      [folderId]: !expandedKeys.value[folderId]
    }
  }

  // 拖拽开始
  const onDragStart = (event: DragEvent, folder: Folder) => {
    draggedFolder.value = folder
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move'
    }
  }

  // 拖拽经过
  const onDragOver = (event: DragEvent, folder: Folder) => {
    dragOverFolder.value = folder
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'move'
    }
    const target = (event.target as HTMLElement).closest('.folder-item')
    if (!target) return
    const rect = target.getBoundingClientRect()
    const y = event.clientY - rect.top
    const relativeY = y / rect.height
    hoverSide.value = relativeY < DRAG_SIBLING_THRESHOLD ? 'sibling' : 'child'
    hoverPosition.value = relativeY < 0.5 ? 'above' : 'below'
  }

  // 拖拽离开
  const onDragLeave = () => {
    dragOverFolder.value = null
  }

  // 拖拽放下
  const onDrop = async (event: DragEvent, targetFolder: Folder | null) => {
    const dragged = draggedFolder.value
    draggedFolder.value = null
    dragOverFolder.value = null

    if (!dragged) return
    if (targetFolder && String(dragged.id) === String(targetFolder.id)) return

    const flatFolders = flattenFolders(folders.value)

    if (targetFolder) {
      const draggedItem = flatFolders.find(f => String(f.id) === String(dragged.id))
      const targetItem = flatFolders.find(f => String(f.id) === String(targetFolder.id))

      if (!draggedItem || !targetItem) return

      if (isDescendant(draggedItem, targetFolder.id, flatFolders)) {
        ElMessage.warning('不能将分类拖拽到其子分类下')
        return
      }

      const isSameLevel = isSameParentId(draggedItem.parentId, targetFolder.parentId)
      const shouldBeChild = hoverSide.value === 'child'

      if (isSameLevel && !shouldBeChild) {
        // 同层级内排序
        const parentIdStr = getParentIdStr(draggedItem.parentId)
        const siblings = flatFolders.filter(f => getParentIdStr(f.parentId) === parentIdStr)
        const draggedIndex = siblings.findIndex(f => String(f.id) === String(dragged.id))
        const targetIndex = siblings.findIndex(f => String(f.id) === String(targetFolder.id))

        if (draggedIndex === -1 || targetIndex === -1) return

        const item = siblings.splice(draggedIndex, 1)[0]
        siblings.splice(targetIndex, 0, item)

        try {
          const folderIds = siblings.map(f => f.id)
          await foldersAPI.reorderFolders({ folderIds })
          await loadFolders()
          ElMessage.success('排序已保存')
        } catch (error: unknown) {
          ElMessage.error(error instanceof Error ? error.message : '保存排序失败')
          await loadFolders()
        }
        return
      }

      // 跨层级移动
      let newParentId: string | number
      if (hoverSide.value === 'child') {
        newParentId = targetFolder.id
      } else {
        newParentId = getParentIdStr(targetFolder.parentId)
      }

      try {
        await foldersAPI.updateFolder(dragged.id, { parentId: newParentId })
        ElMessage.success('分类移动成功')
        await loadFolders()
      } catch (error: unknown) {
        ElMessage.error(error instanceof Error ? error.message : '移动分类失败')
        await loadFolders()
      }
    } else {
      // 拖到根区域，设为顶级
      try {
        await foldersAPI.updateFolder(dragged.id, { parentId: '0' })
        ElMessage.success('分类已移至顶级')
        await loadFolders()
      } catch (error: unknown) {
        ElMessage.error(error instanceof Error ? error.message : '操作失败')
        await loadFolders()
      }
    }
  }

  // 创建分类
  const createFolder = async (name: string, parentId: number | string | null = null) => {
    saving.value = true
    try {
      const parentIdValue = parentId === null ? '0' : parentId
      await foldersAPI.createFolder({ name, parentId: parentIdValue })
      ElMessage.success('分类创建成功')
      await loadFolders()
    } catch (error: unknown) {
      ElMessage.error(error instanceof Error ? error.message : '创建分类失败')
    } finally {
      saving.value = false
    }
  }

  // 更新分类
  const updateFolder = async (id: string, name: string, parentId: number | string | null = null) => {
    saving.value = true
    try {
      const parentIdValue = (parentId === null || parentId === undefined) ? '0' : parentId
      await foldersAPI.updateFolder(id, { name, parentId: parentIdValue })
      ElMessage.success('分类修改成功')
      await loadFolders()
    } catch (error: unknown) {
      ElMessage.error(error instanceof Error ? error.message : '修改分类失败')
    } finally {
      saving.value = false
    }
  }

  // 删除分类
  const deleteFolder = async (id: string) => {
    saving.value = true
    try {
      await foldersAPI.deleteFolder(id)
      ElMessage.success('分类删除成功')
      await loadFolders()
    } catch (error: unknown) {
      ElMessage.error(error instanceof Error ? error.message : '删除分类失败')
    } finally {
      saving.value = false
    }
  }

  // 切换置顶
  const togglePinFolder = async (id: string) => {
    saving.value = true
    try {
      await foldersAPI.pinFolder(id)
      await loadFolders()
    } catch (error: unknown) {
      ElMessage.error(error instanceof Error ? error.message : '操作失败')
    } finally {
      saving.value = false
    }
  }

  // 创建笔记
  const createNote = async (title: string = '无标题笔记', content: string = '', folderId?: string | number | null) => {
    saving.value = true
    try {
      const note = await notesAPI.createNote({ title, content, folderId })
      ElMessage.success('笔记创建成功')
      await loadNotes()
      return note
    } catch (error: unknown) {
      ElMessage.error(error instanceof Error ? error.message : '创建笔记失败')
      return null
    } finally {
      saving.value = false
    }
  }

  // 在指定分类下创建笔记
  const createNoteInFolder = async (folder: Folder, title: string = '无标题笔记', content: string = '') => {
    return createNote(title, content, folder.id)
  }

  // 删除笔记
  const deleteNote = async (id: string) => {
    saving.value = true
    try {
      await notesAPI.deleteNote(id)
      ElMessage.success('笔记删除成功')
      await loadNotes()
      await loadFolders()
    } catch (error: unknown) {
      ElMessage.error(error instanceof Error ? error.message : '删除笔记失败')
    } finally {
      saving.value = false
    }
  }

  // 根据 ID 查找分类名称
  const getFolderName = (folderId: number | string | null | undefined): string => {
    if (!folderId) return ''
    const findFolder = (items: Folder[]): Folder | null => {
      for (const folder of items) {
        if (String(folder.id) === String(folderId)) return folder
        if (folder.children?.length) {
          const found = findFolder(folder.children)
          if (found) return found
        }
      }
      return null
    }
    const folder = findFolder(folders.value)
    if (!folder) return ''
    return folder.name.length > 8 ? folder.name.substring(0, 8) + '...' : folder.name
  }

  // 获取分类下的笔记
  const getNotesByFolderId = (folderId: number | string | null): Note[] => {
    if (folderId === null || folderId === undefined) return []
    return notes.value.filter(n => String(n.folderId) === String(folderId))
  }

  const returnValue: SidebarReturn = {
    // State
    folders,
    notes,
    uncategorizedCount,
    isLoading,
    saving,
    draggedFolder,
    dragOverFolder,
    hoverSide,
    hoverPosition,
    expandedKeys,
    // Computed
    isLevelChange,
    rootFolders,
    // Actions
    loadFolders,
    loadNotes,
    loadAll,
    toggleExpand,
    onDragStart,
    onDragOver,
    onDragLeave,
    onDrop,
    createFolder,
    updateFolder,
    deleteFolder,
    togglePinFolder,
    createNote,
    createNoteInFolder,
    deleteNote,
    getFolderName,
    getNotesByFolderId
  }

  return returnValue
}

export interface SidebarReturn {
  // State
  folders: Ref<Folder[]>
  notes: Ref<Note[]>
  uncategorizedCount: Ref<number>
  isLoading: Ref<boolean>
  saving: Ref<boolean>
  draggedFolder: Ref<Folder | null>
  dragOverFolder: Ref<Folder | null>
  hoverSide: Ref<'sibling' | 'child'>
  hoverPosition: Ref<'above' | 'below'>
  expandedKeys: Ref<Record<number | string, boolean>>
  // Computed
  isLevelChange: ComputedRef<boolean>
  rootFolders: ComputedRef<Folder[]>
  // Actions
  loadFolders: () => Promise<void>
  loadNotes: (params?: Record<string, string | number | undefined>) => Promise<void>
  loadAll: () => Promise<void>
  toggleExpand: (folderId: number | string) => void
  onDragStart: (event: DragEvent, folder: Folder) => void
  onDragOver: (event: DragEvent, folder: Folder) => void
  onDragLeave: () => void
  onDrop: (event: DragEvent, targetFolder: Folder | null) => Promise<void>
  createFolder: (name: string, parentId?: number | string | null) => Promise<void>
  updateFolder: (id: string, name: string, parentId?: number | string | null) => Promise<void>
  deleteFolder: (id: string) => Promise<void>
  togglePinFolder: (id: string) => Promise<void>
  createNote: (title?: string, content?: string, folderId?: number | null) => Promise<Note | null>
  createNoteInFolder: (folder: Folder, title?: string, content?: string) => Promise<Note | null>
  deleteNote: (id: string) => Promise<void>
  getFolderName: (folderId: number | string | null | undefined) => string
  getNotesByFolderId: (folderId: number | string | null) => Note[]
}

export type SidebarComposable = ReturnType<typeof useSidebar>