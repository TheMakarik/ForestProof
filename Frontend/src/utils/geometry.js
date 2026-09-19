// Клиентские проверки геометрии пользовательского полигона (ТЗ 7.2, 4.1).
// Площадь считается в равновеликой проекции через @turf/area, а не по bbox.
import turfArea from '@turf/area'
import turfBbox from '@turf/bbox'
import booleanValid from '@turf/boolean-valid'
import { feature } from '@turf/helpers'
import { MAX_AREA_HA } from '../constants'

const GEOMETRY_TYPES = ['Polygon', 'MultiPolygon']

const toFeature = (geometry) => feature(geometry)

export const geometryAreaHa = (geometry) => {
  if (!geometry) return null
  try {
    const squareMeters = turfArea(toFeature(geometry))
    return squareMeters / 10_000
  } catch {
    return null
  }
}

export const bboxOf = (geometry) => {
  if (!geometry) return null
  try {
    return turfBbox(toFeature(geometry))
  } catch {
    return null
  }
}

export const parseGeoJson = (text) => {
  if (!text || !text.trim()) return { geometry: null, error: null }
  let parsed
  try {
    parsed = JSON.parse(text)
  } catch (error) {
    return { geometry: null, error: `GeoJSON не является корректным JSON: ${error.message}` }
  }

  const geometry =
    parsed.type === 'FeatureCollection'
      ? parsed.features?.[0]?.geometry
      : parsed.type === 'Feature'
        ? parsed.geometry
        : parsed

  if (!geometry) return { geometry: null, error: 'В GeoJSON не найдена геометрия.' }
  return { geometry, error: null }
}

const coordinatesInRange = (coordinates) => {
  let ok = true
  const visit = (value) => {
    if (!ok) return
    if (typeof value[0] === 'number') {
      const [lng, lat] = value
      if (lng < -180 || lng > 180 || lat < -90 || lat > 90) ok = false
      return
    }
    value.forEach(visit)
  }
  try {
    visit(coordinates)
  } catch {
    ok = false
  }
  return ok
}

// Возвращает { ok, errors[], areaHa, bbox }. Проверяет тип, WGS84,
// самопересечения/валидность и лимит площади.
export const validateGeometry = (geometry) => {
  const errors = []
  if (!geometry) return { ok: false, errors: ['Геометрия не задана.'], areaHa: null, bbox: null }

  if (!GEOMETRY_TYPES.includes(geometry.type)) {
    errors.push('Поддерживаются только Polygon и MultiPolygon.')
    return { ok: false, errors, areaHa: null, bbox: null }
  }

  if (!coordinatesInRange(geometry.coordinates)) {
    errors.push('Координаты выходят за пределы WGS 84 (долгота −180…180, широта −90…90).')
  }

  try {
    if (!booleanValid(toFeature(geometry))) {
      errors.push('Полигон невалиден: возможно самопересечение или незамкнутое кольцо.')
    }
  } catch {
    errors.push('Не удалось проверить геометрию.')
  }

  const areaHa = geometryAreaHa(geometry)
  if (areaHa === null) {
    errors.push('Не удалось вычислить площадь полигона.')
  } else if (areaHa > MAX_AREA_HA) {
    errors.push(
      `Площадь полигона ${areaHa.toFixed(1)} га превышает допустимые ${MAX_AREA_HA} га (20 км²).`
    )
  } else if (areaHa <= 0) {
    errors.push('Площадь полигона равна нулю.')
  }

  return { ok: errors.length === 0, errors, areaHa, bbox: bboxOf(geometry) }
}
