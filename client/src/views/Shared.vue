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
      <div class="shared-footer">
        <router-link to="/home" class="back-home-link">
          <el-icon><HomeFilled /></el-icon>
          <span>返回首页</span>
        </router-link>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Loading, HomeFilled } from '@element-plus/icons-vue'
import { marked } from 'marked'
import DOMPurify from 'dompurify'
import { sharedAPI } from '../api'
import axios from 'axios'

const route = useRoute()
const router = useRouter()

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
    ALLOWED_ATTR: ['href', 'alt', 'title', 'class', 'src']
  })
})

// 安全访问分享笔记：通过 POST 交换 token 为 session cookie
const accessSharedNote = async (token) => {
  const response = await axios.post('/api/shared/access', { token }, {
    withCredentials: true
  })
  return response.data
}

// 通过 session cookie 获取分享笔记
const loadSharedNoteBySession = async (noteId) => {
  const response = await axios.get(`/api/shared/view/${noteId}`, {
    withCredentials: true
  })
  return response.data
}

const loadSharedNote = async () => {
  try {
    loading.value = true
    const token = route.params.token

    if (token) {
      // 使用新的安全访问方式：通过 POST 交换 token 为 session cookie
      const accessResult = await accessSharedNote(token)
      // 使用 window.location.href 进行真正的重定向（组件会重新挂载）
      window.location.href = accessResult.redirectUrl
      return // 不继续执行，等待页面重载
    } else if (route.params.id) {
      // 直接通过 session 访问（URL 中只有 noteId，无 token）
      note.value = await loadSharedNoteBySession(route.params.id)
    } else {
      error.value = '无效的分享链接'
    }
  } catch (err) {
    if (err.response?.status === 401) {
      error.value = '会话已过期，请重新访问分享链接'
    } else if (err.response?.status === 404) {
      error.value = '笔记不存在'
    } else {
      error.value = '加载失败'
    }
  } finally {
    loading.value = false
  }
}

const extractNoteIdFromRedirect = (redirectUrl) => {
  const match = redirectUrl.match(/\/shared\/view\/(\d+)/)
  return match ? match[1] : null
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

<style scoped>
.shared-footer {
  margin-top: 30px;
  padding-top: 20px;
  padding-bottom: 10px;
  border-top: 1px solid #e4e7ed;
  text-align: center;
}

.back-home-link {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  color: #409eff;
  text-decoration: none;
  font-size: 14px;
  padding: 8px 16px;
  border-radius: 4px;
  transition: all 0.2s;
}

.back-home-link:hover {
  color: #66b1ff;
  background-color: #ecf5ff;
}
</style>
