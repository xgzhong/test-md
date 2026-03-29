<template>
  <div class="shared-container">
    <div v-if="loading" style="text-align: center; padding: 50px;">
      <el-icon class="is-loading" style="font-size: 32px;"><Loading /></el-icon>
      <p style="margin-top: 15px; color: #666;">加载中...</p>
    </div>

    <div v-else-if="error" style="text-align: center; padding: 50px;">
      <div style="font-size: 48px; margin-bottom: 20px;">😕</div>
      <h2 style="margin-bottom: 10px;">{{ error }}</h2>
      <p style="color: #666;">该笔记可能不存在或未分享</p>
      <el-button type="primary" style="margin-top: 20px;" @click="$router.push('/login')">
        登录后创建自己的笔记
      </el-button>
    </div>

    <div v-else class="shared-card">
      <div class="shared-header">
        <h1>{{ note.title }}</h1>
        <div class="author">
          作者：{{ note.author }} · {{ formatDate(note.createdAt) }}
        </div>
      </div>
      <div class="shared-body">
        <div class="content" v-html="renderedContent"></div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRoute } from 'vue-router'
import { Loading } from '@element-plus/icons-vue'
import { marked } from 'marked'
import DOMPurify from 'dompurify'
import { sharedAPI } from '../api'

const route = useRoute()

const note = ref({})
const loading = ref(true)
const error = ref('')

// Configure marked for safer parsing
marked.setOptions({
  breaks: true,
  gfm: true
})

const renderedContent = computed(() => {
  if (!note.value.content) return ''
  const html = marked(note.value.content)
  return DOMPurify.sanitize(html, {
    ALLOWED_TAGS: ['p', 'br', 'strong', 'em', 'code', 'pre', 'blockquote',
                   'ul', 'ol', 'li', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
                   'a', 'img', 'table', 'thead', 'tbody', 'tr', 'th', 'td'],
    ALLOWED_ATTR: ['href', 'alt', 'title', 'class']
  })
})

const loadSharedNote = async () => {
  try {
    loading.value = true
    const token = route.params.token
    note.value = await sharedAPI.getSharedNote(token)
  } catch (err) {
    error.value = '笔记不存在'
  } finally {
    loading.value = false
  }
}

const formatDate = (dateStr) => {
  const date = new Date(dateStr)
  return date.toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}

onMounted(() => {
  loadSharedNote()
})
</script>
