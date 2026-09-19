// Декодирование GeoTIFF на клиенте и отрисовка в canvas.
// MapLibre GL v6 не умеет TIFF, поэтому backend-слои (image/tiff)
// декодируются через geotiff.js и передаются в ImageSource как canvas.
import { fromArrayBuffer } from 'geotiff'

const hexToRgb = (hex) => {
  const value = hex.replace('#', '')
  return [
    parseInt(value.slice(0, 2), 16),
    parseInt(value.slice(2, 4), 16),
    parseInt(value.slice(4, 6), 16)
  ]
}

const RAMPS = {
  biomass: ['#f7fcf5', '#d9f0d3', '#a6dba0', '#5aae61', '#1b7837', '#00441b'],
  lossYear: ['#fff5eb', '#fee6ce', '#fdae6b', '#e6550d', '#a63603', '#7f2704'],
  burn: ['#ffffb2', '#fed976', '#feb24c', '#fd8d3c', '#e31a1c', '#800026'],
  diverging: ['#b2182b', '#ef8a62', '#fddbc7', '#f7f7f7', '#d1e5f0', '#67a9cf', '#2166ac'],
  scl: ['#000000', '#ff0000', '#8b4513', '#808080', '#00b050', '#ffff00', '#00b0f0', '#ff00ff', '#c0c0c0', '#ffffff', '#add8e6', '#00ffff']
}

const sampleRamp = (stops, t) => {
  const clamped = Math.min(1, Math.max(0, t))
  const position = clamped * (stops.length - 1)
  const index = Math.floor(position)
  const next = Math.min(stops.length - 1, index + 1)
  const local = position - index
  const [r1, g1, b1] = hexToRgb(stops[index])
  const [r2, g2, b2] = hexToRgb(stops[next])
  return [
    Math.round(r1 + (r2 - r1) * local),
    Math.round(g1 + (g2 - g1) * local),
    Math.round(b1 + (b2 - b1) * local)
  ]
}

const percentile = (values, fraction) => {
  if (values.length === 0) return 0
  const sorted = values.slice().sort((a, b) => a - b)
  const index = Math.min(sorted.length - 1, Math.max(0, Math.floor(sorted.length * fraction)))
  return sorted[index]
}

const makeCanvas = (width, height) => {
  const canvas = document.createElement('canvas')
  canvas.width = width
  canvas.height = height
  return canvas
}

const isTransparentValue = (ramp, value) => {
  if (!Number.isFinite(value)) return true
  if ((ramp === 'lossYear' || ramp === 'burn') && value <= 0) return true
  return false
}

const renderSingleBand = (band, width, height, ramp) => {
  const canvas = makeCanvas(width, height)
  const context = canvas.getContext('2d')
  const imageData = context.createImageData(width, height)

  let min = Infinity
  let max = -Infinity
  for (let i = 0; i < band.length; i += 1) {
    const value = band[i]
    if (!Number.isFinite(value) || isTransparentValue(ramp, value)) continue
    if (value < min) min = value
    if (value > max) max = value
  }
  if (!Number.isFinite(min) || !Number.isFinite(max)) {
    min = 0
    max = 1
  }
  if (min === max) max = min + 1

  const stops = ramp === 'scl' ? null : RAMPS[ramp] ?? RAMPS.biomass

  for (let i = 0; i < band.length; i += 1) {
    const value = band[i]
    const offset = i * 4
    if (isTransparentValue(ramp, value)) {
      imageData.data[offset + 3] = 0
      continue
    }
    let rgb
    if (ramp === 'scl') {
      const palette = RAMPS.scl
      rgb = hexToRgb(palette[Math.round(value) % palette.length] ?? '#000000')
    } else {
      rgb = sampleRamp(stops, (value - min) / (max - min))
    }
    imageData.data[offset] = rgb[0]
    imageData.data[offset + 1] = rgb[1]
    imageData.data[offset + 2] = rgb[2]
    imageData.data[offset + 3] = 220
  }

  context.putImageData(imageData, 0, 0)
  return { canvas, min, max }
}

const renderRgb = (bands, width, height) => {
  const canvas = makeCanvas(width, height)
  const context = canvas.getContext('2d')
  const imageData = context.createImageData(width, height)
  const [red, green, blue] = bands

  const bounds = bands.map((band) => {
    const sample = []
    for (let i = 0; i < band.length; i += 7) {
      const value = band[i]
      if (Number.isFinite(value) && value > 0) sample.push(value)
    }
    const low = percentile(sample, 0.02)
    const high = percentile(sample, 0.98)
    return { low, high: high > low ? high : low + 1 }
  })

  for (let i = 0; i < red.length; i += 1) {
    const offset = i * 4
    const values = [red[i], green[i], blue[i]]
    if (values.some((value) => !Number.isFinite(value) || value <= 0)) {
      imageData.data[offset + 3] = 0
      continue
    }
    imageData.data[offset] = Math.round(Math.min(1, Math.max(0, (red[i] - bounds[0].low) / (bounds[0].high - bounds[0].low))) * 255)
    imageData.data[offset + 1] = Math.round(Math.min(1, Math.max(0, (green[i] - bounds[1].low) / (bounds[1].high - bounds[1].low))) * 255)
    imageData.data[offset + 2] = Math.round(Math.min(1, Math.max(0, (blue[i] - bounds[2].low) / (bounds[2].high - bounds[2].low))) * 255)
    imageData.data[offset + 3] = 235
  }

  context.putImageData(imageData, 0, 0)
  return { canvas }
}

// Возвращает { canvas, coordinates, width, height, bbox } для ImageSource.
export const loadRasterFromArrayBuffer = async (buffer, ramp = 'biomass') => {
  const tiff = await fromArrayBuffer(buffer)
  const image = await tiff.getImage()
  const width = image.getWidth()
  const height = image.getHeight()
  const bbox = image.getBoundingBox()
  const samples = image.getSamplesPerPixel()

  let result
  if (ramp === 'rgb' && samples >= 3) {
    const bands = await image.readRasters({ samples: [0, 1, 2] })
    result = renderRgb(bands, width, height)
  } else {
    const rasters = await image.readRasters({ samples: [0] })
    const band = rasters[0]
    result = renderSingleBand(band, width, height, ramp)
  }

  const [minX, minY, maxX, maxY] = bbox
  return {
    ...result,
    width,
    height,
    bbox: [minX, minY, maxX, maxY],
    // порядок: верхний-левый, верхний-правый, нижний-правый, нижний-левый
    coordinates: [
      [minX, maxY],
      [maxX, maxY],
      [maxX, minY],
      [minX, minY]
    ]
  }
}

export const loadRasterFromUrl = async (url, ramp) => {
  const response = await fetch(url)
  if (!response.ok) throw new Error(`Слой недоступен (${response.status})`)
  const buffer = await response.arrayBuffer()
  return loadRasterFromArrayBuffer(buffer, ramp)
}

export const RAMPS_EXPORT = RAMPS
