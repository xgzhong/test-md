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
 * Optimized: uses string replacement instead of creating DOM elements
 */
const HTML_ESCAPE_MAP: Record<string, string> = {
  '&': '&amp;',
  '<': '&lt;',
  '>': '&gt;',
  '"': '&quot;',
  "'": '&#39;'
}

export const escapeHtml = (str: string | null | undefined): string => {
  if (!str) return ''
  return str.replace(/[&<>"']/g, char => HTML_ESCAPE_MAP[char])
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
