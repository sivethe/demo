var path = require('path'),
    rootPath = path.normalize(__dirname + '/..'),
    env = process.env.NODE_ENV || 'development';

var config = {
  development: {
    root: rootPath,
    app: {
      name: 'nodeapp'
    },
    port: process.env.PORT || 3000,
    db: 'mongodb://mongodemovishi:N3rSZw2zbXKmvy4Dc8BH4fphy9YCoxesncWBbPLNKB0IGLz7cs57DISQ1U9Fx1D27H70JTd13hboxDUXD03tmw==@mongodemovishi.documents.azure.com:10250/nodetest/?ssl=true'
  },

  test: {
    root: rootPath,
    app: {
      name: 'nodeapp'
    },
    port: process.env.PORT || 3000,
    db: 'mongodb://mongodemovishi:N3rSZw2zbXKmvy4Dc8BH4fphy9YCoxesncWBbPLNKB0IGLz7cs57DISQ1U9Fx1D27H70JTd13hboxDUXD03tmw==@mongodemovishi.documents.azure.com:10250/nodetest/?ssl=true'
  },

  production: {
    root: rootPath,
    app: {
      name: 'nodeapp'
    },
    port: process.env.PORT,
    db: 'mongodb://mongodemovishi:N3rSZw2zbXKmvy4Dc8BH4fphy9YCoxesncWBbPLNKB0IGLz7cs57DISQ1U9Fx1D27H70JTd13hboxDUXD03tmw==@mongodemovishi.documents.azure.com:10250/nodetest/?ssl=true'
  }
};

module.exports = config[env];