require('dotenv').config();

const { Sequelize } = require('sequelize');

const dbType = process.env.DB_TYPE || 'sqlite';

let sequelize;

if (dbType === 'mysql') {
  // MySQL 数据库配置
  sequelize = new Sequelize(
    process.env.DB_NAME || 'markdown_notes',
    process.env.DB_USER || 'root',
    process.env.DB_PASSWORD || '',
    {
      host: process.env.DB_HOST || 'localhost',
      port: process.env.DB_PORT || 3306,
      dialect: 'mysql',
      logging: false,
      define: {
        timestamps: true,
        underscored: true
      }
    }
  );
} else {
  // SQLite 数据库配置
  sequelize = new Sequelize({
    dialect: 'sqlite',
    storage: process.env.DB_PATH || './database.sqlite',
    logging: false,
    define: {
      timestamps: true
    }
  });
}

// 测试数据库连接
sequelize.authenticate()
  .then(() => console.log('数据库连接成功'))
  .catch(err => console.error('数据库连接失败:', err));

module.exports = sequelize;
