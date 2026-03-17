const express = require('express');
const { Op } = require('sequelize');
const { v4: uuidv4 } = require('uuid');
const { Note, NoteVersion, Folder } = require('../models');
const { auth } = require('../middleware/auth');

const router = express.Router();

// 获取笔记列表
router.get('/', auth, async (req, res) => {
  try {
    const { folderId, search } = req.query;

    const where = { userId: req.user.id };

    if (folderId) {
      where.folderId = folderId === 'null' ? null : folderId;
    }

    if (search) {
      where[Op.or] = [
        { title: { [Op.like]: `%${search}%` } },
        { content: { [Op.like]: `%${search}%` } }
      ];
    }

    const notes = await Note.findAll({
      where,
      include: [{ model: Folder, as: 'folder', attributes: ['id', 'name'] }],
      order: [['updatedAt', 'DESC']]
    });

    res.json({ notes });
  } catch (error) {
    res.status(500).json({ error: '获取笔记列表失败: ' + error.message });
  }
});

// 获取单个笔记
router.get('/:id', auth, async (req, res) => {
  try {
    const note = await Note.findOne({
      where: { id: req.params.id, userId: req.user.id },
      include: [{ model: Folder, as: 'folder', attributes: ['id', 'name'] }]
    });

    if (!note) {
      return res.status(404).json({ error: '笔记不存在' });
    }

    res.json({ note });
  } catch (error) {
    res.status(500).json({ error: '获取笔记失败: ' + error.message });
  }
});

// 创建笔记
router.post('/', auth, async (req, res) => {
  try {
    const { title, content, folderId } = req.body;

    const note = await Note.create({
      userId: req.user.id,
      title: title || '无标题笔记',
      content: content || '',
      folderId: folderId || null
    });

    // 创建初始版本
    await NoteVersion.create({
      noteId: note.id,
      title: note.title,
      content: note.content
    });

    res.status(201).json({ message: '笔记创建成功', note });
  } catch (error) {
    res.status(500).json({ error: '创建笔记失败: ' + error.message });
  }
});

// 更新笔记
router.put('/:id', auth, async (req, res) => {
  try {
    const note = await Note.findOne({
      where: { id: req.params.id, userId: req.user.id }
    });

    if (!note) {
      return res.status(404).json({ error: '笔记不存在' });
    }

    const { title, content, folderId } = req.body;

    // 如果内容有变化，保存版本历史
    if (content !== note.content || title !== note.title) {
      await NoteVersion.create({
        noteId: note.id,
        title: note.title,
        content: note.content
      });
    }

    await note.update({
      title: title !== undefined ? title : note.title,
      content: content !== undefined ? content : note.content,
      folderId: folderId !== undefined ? folderId : note.folderId
    });

    res.json({ message: '笔记更新成功', note });
  } catch (error) {
    res.status(500).json({ error: '更新笔记失败: ' + error.message });
  }
});

// 删除笔记
router.delete('/:id', auth, async (req, res) => {
  try {
    const note = await Note.findOne({
      where: { id: req.params.id, userId: req.user.id }
    });

    if (!note) {
      return res.status(404).json({ error: '笔记不存在' });
    }

    // 删除关联的版本历史
    await NoteVersion.destroy({ where: { noteId: note.id } });

    await note.destroy();

    res.json({ message: '笔记删除成功' });
  } catch (error) {
    res.status(500).json({ error: '删除笔记失败: ' + error.message });
  }
});

// 分享笔记
router.post('/:id/share', auth, async (req, res) => {
  try {
    const note = await Note.findOne({
      where: { id: req.params.id, userId: req.user.id }
    });

    if (!note) {
      return res.status(404).json({ error: '笔记不存在' });
    }

    const shareToken = uuidv4();

    await note.update({
      isShared: true,
      shareToken
    });

    res.json({
      message: '分享成功',
      shareUrl: `/shared/${shareToken}`
    });
  } catch (error) {
    res.status(500).json({ error: '分享笔记失败: ' + error.message });
  }
});

// 取消分享
router.post('/:id/unshare', auth, async (req, res) => {
  try {
    const note = await Note.findOne({
      where: { id: req.params.id, userId: req.user.id }
    });

    if (!note) {
      return res.status(404).json({ error: '笔记不存在' });
    }

    await note.update({
      isShared: false,
      shareToken: null
    });

    res.json({ message: '取消分享成功' });
  } catch (error) {
    res.status(500).json({ error: '取消分享失败: ' + error.message });
  }
});

// 获取版本历史
router.get('/:id/versions', auth, async (req, res) => {
  try {
    const note = await Note.findOne({
      where: { id: req.params.id, userId: req.user.id }
    });

    if (!note) {
      return res.status(404).json({ error: '笔记不存在' });
    }

    const versions = await NoteVersion.findAll({
      where: { noteId: note.id },
      order: [['createdAt', 'DESC']],
      limit: 50
    });

    res.json({ versions });
  } catch (error) {
    res.status(500).json({ error: '获取版本历史失败: ' + error.message });
  }
});

// 恢复到指定版本
router.post('/:id/restore/:versionId', auth, async (req, res) => {
  try {
    const note = await Note.findOne({
      where: { id: req.params.id, userId: req.user.id }
    });

    if (!note) {
      return res.status(404).json({ error: '笔记不存在' });
    }

    const version = await NoteVersion.findOne({
      where: { id: req.params.versionId, noteId: note.id }
    });

    if (!version) {
      return res.status(404).json({ error: '版本不存在' });
    }

    // 保存当前版本到历史
    await NoteVersion.create({
      noteId: note.id,
      title: note.title,
      content: note.content
    });

    // 恢复到指定版本
    await note.update({
      title: version.title,
      content: version.content
    });

    res.json({ message: '恢复成功', note });
  } catch (error) {
    res.status(500).json({ error: '恢复版本失败: ' + error.message });
  }
});

module.exports = router;
