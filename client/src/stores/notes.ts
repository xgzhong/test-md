import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { notesAPI, foldersAPI } from '../api'

export interface Note {
  id: number
  folderId: number | null
  folderName: string | null
  title: string
  content: string
  isShared: boolean
  shareToken: string | null
  version: number
  createdAt: string
  updatedAt: string
}

export interface Folder {
  id: number
  name: string
  parentId: number
  noteCount: number
  sortOrder: number
  isPinned: boolean
  children?: Folder[]
}

export const useNotesStore = defineStore('notes', () => {
  // State
  const notes = ref<Note[]>([])
  const folders = ref<Folder[]>([])
  const currentNote = ref<Note | null>(null)
  const isLoading = ref(false)
  const searchQuery = ref('')

  // Getters
  const filteredNotes = computed(() => {
    if (!searchQuery.value) return notes.value
    const query = searchQuery.value.toLowerCase()
    return notes.value.filter(
      n => n.title.toLowerCase().includes(query) || n.content.toLowerCase().includes(query)
    )
  })

  const rootFolders = computed(() => folders.value.filter(f => f.parentId === 0))

  // Actions
  async function fetchNotes(folderId?: number) {
    isLoading.value = true
    try {
      const params: Record<string, any> = {}
      if (folderId !== undefined) params.folderId = folderId
      if (searchQuery.value) params.search = searchQuery.value

      const res = await notesAPI.getNotes(params)
      notes.value = res.notes as Note[]
    } finally {
      isLoading.value = false
    }
  }

  async function fetchFolders() {
    const res = await foldersAPI.getFolders()
    folders.value = res.folders as Folder[]
  }

  async function createNote(title: string, content: string, folderId?: number) {
    const res = await notesAPI.createNote({ title, content, folderId } as any)
    notes.value.unshift(res.note as Note)
    return res.note as Note
  }

  async function updateNote(id: number, data: Partial<Note>) {
    const res = await notesAPI.updateNote(id, data as any)
    const index = notes.value.findIndex(n => String(n.id) === String(id))
    if (index !== -1) {
      notes.value[index] = res.note as Note
    }
    if (String(currentNote.value?.id) === String(id)) {
      currentNote.value = res.note as Note
    }
    return res.note as Note
  }

  async function deleteNote(id: number) {
    await notesAPI.deleteNote(id)
    notes.value = notes.value.filter(n => String(n.id) !== String(id))
    if (String(currentNote.value?.id) === String(id)) {
      currentNote.value = null
    }
  }

  async function getNote(id: number) {
    const res = await notesAPI.getNote(id)
    currentNote.value = res.note as Note
    return res.note as Note
  }

  function setSearchQuery(query: string) {
    searchQuery.value = query
  }

  return {
    // State
    notes,
    folders,
    currentNote,
    isLoading,
    searchQuery,
    // Getters
    filteredNotes,
    rootFolders,
    // Actions
    fetchNotes,
    fetchFolders,
    createNote,
    updateNote,
    deleteNote,
    getNote,
    setSearchQuery
  }
})