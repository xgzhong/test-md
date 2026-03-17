const { DataTypes } = require('sequelize');
const sequelize = require('../config/database');

const Note = sequelize.define('Note', {
  id: {
    type: DataTypes.INTEGER,
    primaryKey: true,
    autoIncrement: true
  },
  userId: {
    type: DataTypes.INTEGER,
    allowNull: false,
    field: 'user_id',
    references: {
      model: 'users',
      key: 'id'
    }
  },
  folderId: {
    type: DataTypes.INTEGER,
    allowNull: true,
    field: 'folder_id',
    references: {
      model: 'folders',
      key: 'id'
    }
  },
  title: {
    type: DataTypes.STRING(255),
    allowNull: false,
    defaultValue: '无标题笔记'
  },
  content: {
    type: DataTypes.TEXT,
    allowNull: true,
    defaultValue: ''
  },
  isShared: {
    type: DataTypes.BOOLEAN,
    allowNull: false,
    defaultValue: false,
    field: 'is_shared'
  },
  shareToken: {
    type: DataTypes.STRING(255),
    allowNull: true,
    field: 'share_token'
  }
}, {
  tableName: 'notes',
  timestamps: true
});

module.exports = Note;
