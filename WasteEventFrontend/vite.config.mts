import { readdirSync, readFileSync, writeFileSync } from 'node:fs'
import { join } from 'node:path'
import { fileURLToPath, URL } from 'node:url'
import Vue from '@vitejs/plugin-vue'
import Fonts from 'unplugin-fonts/vite'
import { defineConfig } from 'vite'
import Vuetify, { transformAssetUrls } from 'vite-plugin-vuetify'

const publicDirectory = fileURLToPath(new URL('public', import.meta.url))

function getSvgFiles (directory: string): string[] {
  return readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    const path = join(directory, entry.name)

    if (entry.isDirectory()) {
      return getSvgFiles(path)
    }

    return entry.isFile() && entry.name.endsWith('.svg') ? [path] : []
  })
}

function getSvgSize (svg: string) {
  const viewBox = svg.match(/viewBox="[^\d.-]*([\d.-]+)[\s,]+([\d.-]+)[\s,]+([\d.-]+)[\s,]+([\d.-]+)"/)

  return {
    height: viewBox?.[4] ?? '64',
    width: viewBox?.[3] ?? '64',
  }
}

function ensurePublicSvgDimensions () {
  for (const file of getSvgFiles(publicDirectory)) {
    const svg = readFileSync(file, 'utf8')
    const openingTag = svg.match(/<svg\b[^>]*>/)?.[0]

    if (!openingTag) {
      continue
    }

    const hasHeight = /\sheight=/.test(openingTag)
    const hasWidth = /\swidth=/.test(openingTag)

    if (hasHeight && hasWidth) {
      continue
    }

    const size = getSvgSize(openingTag)
    const attributes = [
      hasWidth ? undefined : `width="${size.width}"`,
      hasHeight ? undefined : `height="${size.height}"`,
    ].filter(Boolean).join(' ')
    const nextOpeningTag = openingTag.replace('<svg', `<svg ${attributes}`)

    writeFileSync(file, svg.replace(openingTag, nextOpeningTag))
  }
}

function publicSvgDimensionsPlugin () {
  return {
    buildStart: ensurePublicSvgDimensions,
    configureServer: ensurePublicSvgDimensions,
    name: 'public-svg-dimensions',
  }
}

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    publicSvgDimensionsPlugin(),
    Vue({
      template: { transformAssetUrls },
    }),
    // https://github.com/vuetifyjs/vuetify-loader/tree/master/packages/vite-plugin#readme
    Vuetify({
      autoImport: true,
      styles: {
        configFile: 'src/styles/settings.scss',
      },
    }),
    Fonts({
      fontsource: {
        families: [
          {
            name: 'Roboto',
            weights: [100, 300, 400, 500, 700, 900],
            styles: ['normal', 'italic'],
          },
        ],
      },
    }),
  ],
  define: { 'process.env': {} },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('src', import.meta.url)),
    },
    extensions: [
      '.js',
      '.json',
      '.jsx',
      '.mjs',
      '.ts',
      '.tsx',
      '.vue',
    ],
  },
  server: {
    port: 3000,
  },
})
