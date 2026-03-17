const sequelize = require('../config/database');
const User = require('./User');
const Folder = require('./Folder');
const Note = require('./Note');
const NoteVersion = require('./NoteVersion');

// 建立关联关系
User.hasMany(Folder, { foreignKey: 'userId', as: 'folders' });
Folder.belongsTo(User, { foreignKey: 'userId', as: 'user' });

User.hasMany(Note, { foreignKey: 'userId', as: 'notes' });
Note.belongsTo(User, { foreignKey: 'userId', as: 'user' });

Folder.hasMany(Note, { foreignKey: 'folderId', as: 'notes' });
Note.belongsTo(Folder, { foreignKey: 'folderId', as: 'folder' });

Note.hasMany(NoteVersion, { foreignKey: 'noteId', as: 'versions' });
NoteVersion.belongsTo(Note, { foreignKey: 'noteId', as: 'note' });

module.exports = {
  sequelize,
  User,
  Folder,
  Note,
  NoteVersion
};
