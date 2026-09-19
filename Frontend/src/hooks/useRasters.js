import { useEffect, useRef, useState } from 'react'
import { api } from '../api/client'
import { loadRasterFromArrayBuffer } from '../api/raster'
import { LAYER_DEFINITIONS } from '../constants'

// Ленивая загрузка растровых слоёв (GeoTIFF -> canvas) по списку ключей.
export const useRasters = (runId, keys) => {
  const [rasters, setRasters] = useState({})
  const loadedRef = useRef({ runId: null, keys: new Set() })
  const keyString = keys.slice().sort().join(',')

  useEffect(() => {
    if (loadedRef.current.runId !== runId) {
      loadedRef.current = { runId, keys: new Set() }
      setRasters({})
    }
    if (!runId) return undefined

    let cancelled = false
    keys.forEach(async (key) => {
      if (loadedRef.current.keys.has(key)) return
      loadedRef.current.keys.add(key)
      setRasters((prev) => ({ ...prev, [key]: { status: 'loading' } }))
      try {
        const definition = LAYER_DEFINITIONS.find((layer) => layer.key === key)
        const buffer = await api.getLayerArrayBuffer(runId, key)
        const raster = await loadRasterFromArrayBuffer(buffer, definition?.ramp ?? 'biomass')
        if (!cancelled) setRasters((prev) => ({ ...prev, [key]: { status: 'ready', ...raster } }))
      } catch (error) {
        loadedRef.current.keys.delete(key)
        if (!cancelled) {
          setRasters((prev) => ({ ...prev, [key]: { status: 'error', error: error.message } }))
        }
      }
    })

    return () => {
      cancelled = true
    }
  }, [runId, keyString])

  return rasters
}
