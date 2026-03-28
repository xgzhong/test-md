import { ref, computed, type Ref, type ComputedRef } from 'vue'
import { ElMessage } from 'element-plus'
import { notesAPI, foldersAPI, type Folder } from '../api'

// ============== Standalone Utility Functions ==============

/**
 * Format date string for display
 */
export const formatDate = (dateStr: string | null | undefined): string => {
  if (!dateStr) return ''
  const date = new Date(dateStr + 'Z')
  return date.toLocaleDateString('zh-CN', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

/**
 * Escape HTML to prevent XSS when displaying plain text
 */
export const escapeHtml = (str: string | null | undefined): string => {
  if (!str) return ''
  const div = document.createElement('div')
  div.textContent = str
  return div.innerHTML
}

/**
 * Drag-drop helper: Check if ID represents root level
 */
export const isRootId = (id: string | number | null | undefined): boolean => {
  if (id === null || id === undefined) return true
  const strId = String(id)
  return strId === '0' || strId === '' || strId === 'null' || strId === 'undefined'
}

/**
 * Drag-drop helper: Get parent ID as string
 */
export const getParentIdStr = (id: string | number | null | undefined): string => {
  if (id === null || id === undefined) return '0'
  const strId = String(id)
  return isRootId(strId) ? '0' : strId
}

/**
 * Drag-drop helper: Check if two IDs have same parent
 */
export const isSameParentId = (id1: string | number | null | undefined, id2: string | number | null | undefined): boolean => {
  return getParentIdStr(id1) === getParentIdStr(id2)
}

/**
 * Drag-drop helper: Check if targetId is descendant of parent
 */
export const isDescendant = (parent: Folder, childId: string | number, flatFolders: Folder[]): boolean => {
  const parentIdStr = String(parent.id)
  const children = flatFolders.filter(f => String(f.parentId) === parentIdStr)
  for (const child of children) {
    if (String(child.id) === String(childId)) return true
    if (isDescendant(child, childId, flatFolders)) return true
  }
  return false
}

/**
 * Flatten folder tree for drag-drop operations
 */
export const flattenFolders = (folderList: Folder[]): Folder[] => {
  const result: Folder[] = []
  const flatten = (items: Folder[], parentId: string = '0'): void => {
    for (const item of items) {
      const pid = item.parentId ? String(item.parentId) : '0'
      result.push({ ...item, parentId: pid as any })
      if (item.children && item.children.length > 0) {
        flatten(item.children, String(item.id))
      }
    }
  }
  flatten(folderList)
  return result
}

// ============== Composables ==============

interface UseFolderDragOptions {
  folders: Ref<Folder[]>
  loadFoldersCallback?: () => void
}

interface DragState {
  draggedFolder: Ref<Folder | null>
  dragOverFolder: Ref<Folder | null>
  hoverSide: Ref<'sibling' | 'child'>
  hoverPosition: Ref<'above' | 'below'>
  isLevelChange: ComputedRef<boolean>
  onDragStart: (event: DragEvent, folder: Folder) => void
  onDragOver: (event: DragEvent, folder: Folder) => void
  onDragLeave: () => void
  onDrop: (event: DragEvent, targetFolder: Folder) => Promise<void>
}

/**
 * Composable for folder drag-and-drop operations
 */
export function useFolderDrag(options: UseFolderDragOptions): DragState {
  const { folders, loadFoldersCallback } = options

  const draggedFolder = ref<Folder | null>(null)
  const dragOverFolder = ref<Folder | null>(null)
  const hoverSide = ref<'sibling' | 'child'>('child')
  const hoverPosition = ref<'above' | 'below'>('below')

  const DRAG_SIBLING_THRESHOLD = 0.35

  const isLevelChange = computed(() => {
    if (!draggedFolder.value || !dragOverFolder.value) return false
    const draggedPid = draggedFolder.value.parentId ? String(draggedFolder.value.parentId) : '0'
    return draggedPid !== String(dragOverFolder.value.id)
  })

  const onDragStart = (event: DragEvent, folder: Folder): void => {
    draggedFolder.value = folder
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move'
    }
  }

  const onDragOver = (event: DragEvent, folder: Folder): void => {
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

  const onDragLeave = (): void => {
    dragOverFolder.value = null
  }

  const onDrop = async (event: DragEvent, targetFolder: Folder): Promise<void> => {
    const dragged = draggedFolder.value
    draggedFolder.value = null
    dragOverFolder.value = null

    if (!dragged || !targetFolder) return
    if (String(dragged.id) === String(targetFolder.id)) return

    const flatFolders = flattenFolders(folders.value)
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
        loadFoldersCallback?.()
      } catch (error) {
        ElMessage.error('保存排序失败')
        loadFoldersCallback?.()
      }
      return
    }

    let newParentId: string | number
    if (hoverSide.value === 'child') {
      newParentId = targetFolder.id
    } else {
      newParentId = getParentIdStr(targetFolder.parentId)
    }

    try {
      await foldersAPI.updateFolder(dragged.id, { parentId: newParentId })
      ElMessage.success('分类移动成功')
      loadFoldersCallback?.()
    } catch (error: any) {
      console.error('移动分类失败:', error)
      ElMessage.error(error?.message || '移动分类失败')
      loadFoldersCallback?.()
    }
  }

  return {
    draggedFolder,
    dragOverFolder,
    hoverSide,
    hoverPosition,
    isLevelChange,
    onDragStart,
    onDragOver,
    onDragLeave,
    onDrop
  }
}

// ============== Note Operations Composable ==============

interface NoteItem {
  id: number
  title: string
  [key: string]: any
}

export function useNotes() {
  const notes = ref<NoteItem[]>([])
  const totalCount = ref(0)
  const loading = ref(false)

  const loadNotes = async (params: Record<string, string | number> = {}): Promise<void> => {
    loading.value = true
    try {
      const res = await notesAPI.getNotes(params)
      notes.value = res.notes || []
      totalCount.value = res.totalCount || 0
    } catch (error: any) {
      ElMessage.error(error?.message || '加载笔记失败')
    } finally {
      loading.value = false
    }
  }

  const createNote = async (data: { title?: string; content?: string; folderId?: number }): Promise<NoteItem | null> => {
    try {
      const res = await notesAPI.createNote(data)
      return res.note
    } catch (error: any) {
      ElMessage.error(error?.message || '创建笔记失败')
      return null
    }
  }

  const deleteNote = async (noteId: number): Promise<void> => {
    try {
      await notesAPI.deleteNote(noteId)
    } catch (error: any) {
      ElMessage.error(error?.message || '删除笔记失败')
    }
  }

  return {
    notes,
    totalCount,
    loading,
    loadNotes,
    createNote,
    deleteNote
  }
}

// ============== Folder Operations Composable ==============

export function useFolders() {
  const folders = ref<Folder[]>([])
  const uncategorizedCount = ref(0)
  const loading = ref(false)

  const loadFolders = async (): Promise<void> => {
    loading.value = true
    try {
      const res = await foldersAPI.getFolders()
      folders.value = res.folders || []
      uncategorizedCount.value = res.uncategorizedCount || 0
    } catch (error: any) {
      ElMessage.error(error?.message || '加载分类失败')
    } finally {
      loading.value = false
    }
  }

  const createFolder = async (data: { name: string; parentId?: string | number | null }): Promise<void> => {
    try {
      await foldersAPI.createFolder(data)
    } catch (error: any) {
      ElMessage.error(error?.message || '创建分类失败')
    }
  }

  const updateFolder = async (id: number, data: { name?: string; parentId?: string | number | null }): Promise<void> => {
    try {
      await foldersAPI.updateFolder(id, data)
    } catch (error: any) {
      ElMessage.error(error?.message || '更新分类失败')
    }
  }

  const deleteFolder = async (folderId: number): Promise<void> => {
    try {
      await foldersAPI.deleteFolder(folderId)
    } catch (error: any) {
      ElMessage.error(error?.message || '删除分类失败')
    }
  }

  const getFolderName = (folderId: string | number | null | undefined, folderList: Folder[] = folders.value): string => {
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
    const folder = findFolder(folderList)
    if (!folder) return ''
    return folder.name.length > 8 ? folder.name.substring(0, 8) + '...' : folder.name
  }

  const findFolderById = (folderId: string | number, folderList: Folder[] = folders.value): Folder | null => {
    const find = (items: Folder[]): Folder | null => {
      for (const folder of items) {
        if (String(folder.id) === String(folderId)) return folder
        if (folder.children?.length) {
          const found = find(folder.children)
          if (found) return found
        }
      }
      return null
    }
    return find(folderList)
  }

  const getChildFolders = (parentId: string | number, folderList: Folder[] = folders.value): Folder[] => {
    const result: Folder[] = []
    const collectChildren = (items: Folder[], pid: string | number): void => {
      for (const folder of items) {
        if (String(folder.parentId) === String(pid)) {
          result.push(folder)
        }
        if (folder.children?.length) {
          collectChildren(folder.children, pid)
        }
      }
    }
    collectChildren(folderList, parentId)
    return result
  }

  return {
    folders,
    uncategorizedCount,
    loading,
    loadFolders,
    createFolder,
    updateFolder,
    deleteFolder,
    getFolderName,
    findFolderById,
    getChildFolders
  }
}