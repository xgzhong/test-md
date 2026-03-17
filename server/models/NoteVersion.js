const { DataTypes } = require('sequelize');
const sequelize = require('../config/database');

const NoteVersion = sequelize.define('NoteVersion', {
  id: {
    type: DataTypes.INTEGER,
    primaryKey: true,
    autoIncrement: true
  },
  noteId: {
    type: DataTypes.INTEGER,
    allowNull: false,
    field: 'note_id',
    references: {
      model: 'notes',
      key: 'id'
    }
  },
  content: {
    type: DataTypes.TEXT,
    allowNull: true
  },
  title: {
    type: DataTypes.STRING(255),
    allowNull: false
  }
}, {
  tableName: 'note_versions',
  timestamps: true
});

module.exports = NoteVersion;
