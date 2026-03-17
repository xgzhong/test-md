const express = require('express');
const { Folder, Note } = require('../models');
const { auth } = require('../middleware/auth');

const router = express.Router();

// 获取分类列表
router.get('/', auth, async (req, res) => {
  try {
    const folders = await Folder.findAll({
      where: { userId: req.user.id },
      order: [['createdAt', 'ASC']]
    });

    // 获取每个分类下的笔记数量
    const foldersWithCount = await Promise.all(
      folders.map(async (folder) => {
        const noteCount = await Note.count({
          where: { folderId: folder.id, userId: req.user.id }
        });
        return {
          ...folder.toJSON(),
          noteCount
        };
      })
    );

    // 添加"未分类"选项
    const uncategorizedCount = await Note.count({
      where: { folderId: null, userId: req.user.id }
    });

    res.json({
      folders: foldersWithCount,
      uncategorizedCount
    });
  } catch (error) {
    res.status(500).json({ error: '获取分类列表失败: ' + error.message });
  }
});

// 创建分类
router.post('/', auth, async (req, res) => {
  try {
    const { name } = req.body;

    if (!name) {
      return res.status(400).json({ error: '请输入分类名称' });
    }

    const folder = await Folder.create({
      userId: req.user.id,
      name
    });

    res.status(201).json({ message: '分类创建成功', folder });
  } catch (error) {
    res.status(500).json({ error: '创建分类失败: ' + error.message });
  }
});

// 更新分类
router.put('/:id', auth, async (req, res) => {
  try {
    const folder = await Folder.findOne({
      where: { id: req.params.id, userId: req.user.id }
    });

    if (!folder) {
      return res.status(404).json({ error: '分类不存在' });
    }

    const { name } = req.body;

    await folder.update({ name: name || folder.name });

    res.json({ message: '分类更新成功', folder });
  } catch (error) {
    res.status(500).json({ error: '更新分类失败: ' + error.message });
  }
});

// 删除分类
router.delete('/:id', auth, async (req, res) => {
  try {
    const folder = await Folder.findOne({
      where: { id: req.params.id, userId: req.user.id }
    });

    if (!folder) {
      return res.status(404).json({ error: '分类不存在' });
    }

    // 将该分类下的笔记移动到未分类
    await Note.update(
      { folderId: null },
      { where: { folderId: folder.id } }
    );

    await folder.destroy();

    res.json({ message: '分类删除成功' });
  } catch (error) {
    res.status(500).json({ error: '删除分类失败: ' + error.message });
  }
});

module.exports = router;
