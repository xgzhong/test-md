import { ref, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { notesAPI, foldersAPI } from '../api'

/**
 * Composable for folder drag-and-drop operations
 */
export function useFolderDrag(folders, loadFoldersCallback) {
  const draggedFolder = ref(null)
  const dragOverFolder = ref(null)
  const hoverSide = ref('child')
  const hoverPosition = ref('below')

  const DRAG_SIBLING_THRESHOLD = 0.35

  const isLevelChange = computed(() => {
    if (!draggedFolder.value || !dragOverFolder.value) return false
    const draggedPid = draggedFolder.value.parentId ? String(draggedFolder.value.parentId) : '0'
    return draggedPid !== String(dragOverFolder.value.id)
  })

  const onDragStart = (event, folder) => {
    draggedFolder.value = folder
    event.dataTransfer.effectAllowed = 'move'
  }

  const onDragOver = (event, folder) => {
    dragOverFolder.value = folder
    event.dataTransfer.dropEffect = 'move'
    const target = event.target.closest('.folder-item')
    if (!target) return
    const rect = target.getBoundingClientRect()
    const y = event.clientY - rect.top
    const relativeY = y / rect.height
    hoverSide.value = relativeY < DRAG_SIBLING_THRESHOLD ? 'sibling' : 'child'
    hoverPosition.value = relativeY < 0.5 ? 'above' : 'below'
  }

  const onDragLeave = () => {
    dragOverFolder.value = null
  }

  const flattenFolders = (folderList) => {
    const result = []
    const flatten = (items, parentId = '0') => {
      for (const item of items) {
        const pid = item.parentId ? String(item.parentId) : '0'
        result.push({ ...item, parentId: pid })
        if (item.children && item.children.length > 0) {
          flatten(item.children, String(item.id))
        }
      }
    }
    flatten(folderList)
    return result
  }

  const isRootId = (id) => {
    const strId = String(id)
    return strId === '0' || strId === '' || strId === 'null' || strId === 'undefined' || id === null || id === undefined
  }

  const getParentIdStr = (id) => {
    if (id === null || id === undefined) return '0'
    const strId = String(id)
    return isRootId(strId) ? '0' : strId
  }

  const isSameParentId = (id1, id2) => {
    return getParentIdStr(id1) === getParentIdStr(id2)
  }

  const isDescendant = (parent, childId, flatFolders) => {
    const parentIdStr = String(parent.id)
    const children = flatFolders.filter(f => String(f.parentId) === parentIdStr)
    for (const child of children) {
      if (String(child.id) === String(childId)) return true
      if (isDescendant(child, childId, flatFolders)) return true
    }
    return false
  }

  const onDrop = async (event, targetFolder) => {
    const dragged = draggedFolder.value
    draggedFolder.value = null
    dragOverFolder.value = null

    if (!dragged || !targetFolder) return
    if (dragged.id === targetFolder.id) return

    const flatFolders = flattenFolders(folders.value)
    const draggedItem = flatFolders.find(f => f.id === dragged.id)
    const targetItem = flatFolders.find(f => f.id === targetFolder.id)

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
        await foldersAPI.reorder(folderIds)
        loadFoldersCallback?.()
      } catch (error) {
        ElMessage.error('保存排序失败')
        loadFoldersCallback?.()
      }
      return
    }

    let newParentId
    if (hoverSide.value === 'child') {
      newParentId = targetFolder.id
    } else {
      newParentId = getParentIdStr(targetFolder.parentId)
    }

    try {
      await foldersAPI.update(dragged.id, { parentId: newParentId })
      ElMessage.success('分类移动成功')
      loadFoldersCallback?.()
    } catch (error) {
      console.error('移动分类失败:', error)
      ElMessage.error(error.message || '移动分类失败')
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

/**
 * Composable for notes operations
 */
export function useNotes() {
  const notes = ref([])
  const totalCount = ref(0)
  const loading = ref(false)

  const loadNotes = async (params = {}) => {
    loading.value = true
    try {
      const res = await notesAPI.getAll(params)
      notes.value = res.notes || []
      totalCount.value = res.totalCount || 0
    } catch (error) {
      ElMessage.error(error.message)
    } finally {
      loading.value = false
    }
  }

  const createNote = async (data) => {
    const res = await notesAPI.create(data)
    return res.note
  }

  const deleteNote = async (noteId) => {
    await notesAPI.delete(noteId)
  }

  const confirmDeleteNote = (note) => {
    return ElMessageBox.confirm(`确定要删除笔记"${note.title}"吗？`, '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
  }

  const formatDate = (dateStr) => {
    if (!dateStr) return ''
    const date = new Date(dateStr + 'Z')
    return date.toLocaleDateString('zh-CN', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  }

  return {
    notes,
    totalCount,
    loading,
    loadNotes,
    createNote,
    deleteNote,
    confirmDeleteNote,
    formatDate
  }
}

/**
 * Composable for folders operations
 */
export function useFolders() {
  const folders = ref([])
  const uncategorizedCount = ref(0)
  const loading = ref(false)

  const loadFolders = async () => {
    loading.value = true
    try {
      const res = await foldersAPI.getAll()
      folders.value = res.folders || []
      uncategorizedCount.value = res.uncategorizedCount || 0
    } catch (error) {
      ElMessage.error(error.message)
    } finally {
      loading.value = false
    }
  }

  const createFolder = async (data) => {
    await foldersAPI.create(data)
  }

  const updateFolder = async (id, data) => {
    await foldersAPI.update(id, data)
  }

  const deleteFolder = async (folderId) => {
    await foldersAPI.delete(folderId)
  }

  const getFolderName = (folderId, folderList = folders.value) => {
    if (!folderId) return ''
    const findFolder = (items) => {
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

  const findFolderById = (folderId, folderList = folders.value) => {
    const find = (items) => {
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

  const getChildFolders = (parentId, folderList = folders.value) => {
    const result = []
    const collectChildren = (items, pid) => {
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