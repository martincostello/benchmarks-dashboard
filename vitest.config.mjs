// Copyright (c) Martin Costello, 2024. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    clearMocks: true,
    coverage: {
      enabled: true,
      exclude: ['scripts/**/*.test.js'],
      include: ['src/Dashboard/wwwroot/app.js'],
      provider: 'v8',
      reporter: ['html', 'lcov', 'text'],
    },
    environment: 'jsdom',
    include: ['tests/**/*.test.js'],
    reporters: ['default', 'github-actions'],
  },
});
