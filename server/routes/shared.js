const express = require('express');
const { Note, User } = require('../models');

const router = express.Router();

// 通过分享链接查看笔记
router.get('/:token', async (req, res) => {
  try {
    const note = await Note.findOne({
      where: {
        shareToken: req.params.token,
        isShared: true
      },
      include: [{
        model: User,
        as: 'user',
        attributes: ['username']
      }]
    });

    if (!note) {
      return res.status(404).json({ error: '笔记不存在或未分享' });
    }

    res.json({
      note: {
        id: note.id,
        title: note.title,
        content: note.content,
        author: note.user.username,
        createdAt: note.createdAt,
        updatedAt: note.updatedAt
      }
    });
  } catch (error) {
    res.status(500).json({ error: '获取笔记失败: ' + error.message });
  }
});

module.exports = router;
