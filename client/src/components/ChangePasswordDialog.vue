<template>
  <el-dialog v-model="visible" title="修改密码" width="400px" @closed="resetForm">
    <el-form :model="form" :rules="rules" ref="formRef" @submit.prevent="handleSubmit">
      <el-form-item label="旧密码" prop="oldPassword">
        <el-input v-model="form.oldPassword" type="password" placeholder="请输入旧密码" show-password />
      </el-form-item>
      <el-form-item label="新密码" prop="newPassword">
        <el-input v-model="form.newPassword" type="password" placeholder="请输入新密码" show-password />
      </el-form-item>
      <el-form-item label="确认密码" prop="confirmPassword">
        <el-input v-model="form.confirmPassword" type="password" placeholder="请再次输入新密码" show-password @keyup.enter="handleSubmit" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="visible = false">取消</el-button>
      <el-button type="primary" :loading="loading" @click="handleSubmit">确定</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { ElMessage } from 'element-plus'
import { authAPI } from '../api'

const visible = defineModel<boolean>({ default: false })
const formRef = ref(null)
const loading = ref(false)

const form = ref({
  oldPassword: '',
  newPassword: '',
  confirmPassword: ''
})

const validateConfirmPassword = (rule: any, value: any, callback: any) => {
  if (value !== form.value.newPassword) {
    callback(new Error('两次输入的密码不一致'))
  } else {
    callback()
  }
}

const rules = {
  oldPassword: [{ required: true, message: '请输入旧密码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 8, message: '密码长度至少8位', trigger: 'blur' },
    { pattern: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/, message: '密码必须包含大小写字母和数字', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, message: '请确认新密码', trigger: 'blur' },
    { validator: validateConfirmPassword, trigger: 'blur' }
  ]
}

const resetForm = () => {
  form.value = { oldPassword: '', newPassword: '', confirmPassword: '' }
}

const handleSubmit = async () => {
  if (!formRef.value) return
  try {
    await formRef.value.validate()
  } catch {
    return
  }

  loading.value = true
  try {
    await authAPI.changePassword({
      oldPassword: form.value.oldPassword,
      newPassword: form.value.newPassword
    })
    ElMessage.success('密码修改成功')
    visible.value = false
    resetForm()
  } catch (error: any) {
    ElMessage.error(error?.message || '修改密码失败')
  } finally {
    loading.value = false
  }
}
</script>
