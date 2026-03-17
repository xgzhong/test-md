const express = require('express');
const cors = require('cors');
const { sequelize } = require('./models');

const authRoutes = require('./routes/auth');
const notesRoutes = require('./routes/notes');
const foldersRoutes = require('./routes/folders');
const sharedRoutes = require('./routes/shared');

const app = express();
const PORT = process.env.PORT || 3000;

// 中间件
app.use(cors());
app.use(express.json());

// 路由
app.use('/api/auth', authRoutes);
app.use('/api/notes', notesRoutes);
app.use('/api/folders', foldersRoutes);
app.use('/api/shared', sharedRoutes);

// 根路由
app.get('/', (req, res) => {
  res.json({ message: 'Markdown Notes API Server', version: '1.0.0' });
});

// 同步数据库并启动服务器
const startServer = async () => {
  try {
    await sequelize.sync({ alter: true });
    console.log('数据库同步成功');

    app.listen(PORT, () => {
      console.log(`服务器运行在 http://localhost:${PORT}`);
    });
  } catch (error) {
    console.error('启动服务器失败:', error);
    process.exit(1);
  }
};

startServer();
