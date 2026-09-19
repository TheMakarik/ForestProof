import { useEffect, useRef } from 'react'
import { LngLatBounds, Map, NavigationControl, Popup, ScaleControl } from 'maplibre-gl'
import { CAUSE_STATUS, formatNumber } from '../constants'

const BASE_STYLE = {
  version: 8,
  sources: {
    osm: {
      type: 'raster',
      tiles: ['https://tile.openstreetmap.org/{z}/{x}/{y}.png'],
      tileSize: 256,
      attribution: '© OpenStreetMap'
    }
  },
  layers: [
    { id: 'background', type: 'background', paint: { 'background-color': '#dfe7e2' } },
    { id: 'osm', type: 'raster', source: 'osm' }
  ]
}

const EMPTY = { type: 'FeatureCollection', features: [] }

const toFeatureCollection = (value) => {
  if (!value) return EMPTY
  if (value.type === 'FeatureCollection') return value
  if (value.type === 'Feature') return { type: 'FeatureCollection', features: [value] }
  return { type: 'FeatureCollection', features: [{ type: 'Feature', geometry: value, properties: {} }] }
}

const boundsOf = (geometry) => {
  const coordinates = []
  const collect = (value) => {
    if (typeof value?.[0] === 'number') coordinates.push(value)
    else if (Array.isArray(value)) value.forEach(collect)
  }
  collect(geometry?.coordinates)
  if (coordinates.length === 0) return null
  return coordinates.reduce(
    (acc, coordinate) => acc.extend(coordinate),
    new LngLatBounds(coordinates[0], coordinates[0])
  )
}

const causeColor = (cause) => {
  if (cause === 'confirmed') return CAUSE_STATUS.confirmed.color === 'error' ? '#c62828' : '#c62828'
  if (cause === 'likely' || cause === 'probable') return '#ed6c02'
  return '#607d8b'
}

export default function MapView({
  geometry,
  zones = [],
  rasters = {},
  visibleLayers = {},
  height = 460,
  onZoneClick,
  selectedZoneId
}) {
  const containerRef = useRef(null)
  const mapRef = useRef(null)
  const readyRef = useRef(false)
  const dataRef = useRef({ geometry, zones })
  const layersRef = useRef(visibleLayers)
  const rastersRef = useRef(rasters)
  const zoneClickRef = useRef(onZoneClick)
  const selectedRef = useRef(selectedZoneId)
  const applyRef = useRef({ data: () => {}, layers: () => {}, rasters: () => {}, selected: () => {} })

  dataRef.current = { geometry, zones }
  layersRef.current = visibleLayers
  rastersRef.current = rasters
  zoneClickRef.current = onZoneClick
  selectedRef.current = selectedZoneId

  useEffect(() => {
    if (!containerRef.current) return undefined

    const map = new Map({
      container: containerRef.current,
      style: BASE_STYLE,
      center: [40.7, 59.45],
      zoom: 9
    })

    map.addControl(new NavigationControl({ showCompass: false }), 'top-right')
    map.addControl(new ScaleControl({ unit: 'metric' }))

    const applyData = () => {
      const { geometry: aoi, zones: currentZones } = dataRef.current

      map.getSource('aoi')?.setData(toFeatureCollection(aoi))
      map.getSource('zones')?.setData({
        type: 'FeatureCollection',
        features: currentZones
          .filter((zone) => zone.geometry)
          .map((zone) => ({
            type: 'Feature',
            geometry: zone.geometry,
            properties: {
              id: zone.id,
              area: zone.areaHectares,
              contribution: zone.contributionToDeltaCarbon,
              cause: zone.causeStatus,
              evidence: (zone.evidenceTypes ?? []).join(', ')
            }
          }))
      })

      if (aoi) {
        const bounds = boundsOf(aoi)
        if (bounds) map.fitBounds(bounds, { padding: 48, duration: 600, maxZoom: 13 })
      }
    }

    const applyLayers = () => {
      const visible = layersRef.current
      const setVisible = (id, value) => {
        if (map.getLayer(id)) map.setLayoutProperty(id, 'visibility', value ? 'visible' : 'none')
      }
      setVisible('aoi-fill', visible.aoi !== false)
      setVisible('aoi-line', visible.aoi !== false)
      setVisible('zones-fill', visible.zones !== false)
      setVisible('zones-line', visible.zones !== false)
      setVisible('zones-selected-line', visible.zones !== false)

      Object.keys(rastersRef.current ?? {}).forEach((key) => {
        setVisible(`raster-${key}-layer`, Boolean(visible[key]))
      })
    }

    const applyRasters = () => {
      if (!readyRef.current) return
      const entries = Object.entries(rastersRef.current ?? {})
      entries.forEach(([key, raster]) => {
        if (!raster || raster.status !== 'ready' || !raster.canvas) return
        const sourceId = `raster-${key}`
        const layerId = `${sourceId}-layer`
        const dataUrl = raster.canvas.toDataURL('image/png')

        if (!map.getSource(sourceId)) {
          map.addSource(sourceId, {
            type: 'image',
            url: dataUrl,
            coordinates: raster.coordinates
          })
          const beforeId = map.getLayer('aoi-fill') ? 'aoi-fill' : undefined
          map.addLayer(
            {
              id: layerId,
              type: 'image',
              source: sourceId,
              paint: { 'raster-opacity': 0.85, 'raster-fade-duration': 0 }
            },
            beforeId
          )
        } else {
          const source = map.getSource(sourceId)
          if (typeof source.updateImage === 'function') {
            source.updateImage({ image: raster.canvas, coordinates: raster.coordinates })
          }
        }
      })
      applyLayers()
    }

    const applySelected = () => {
      if (!map.getLayer('zones-selected-line')) return
      const id = selectedRef.current
      map.setFilter('zones-selected-line', ['==', ['get', 'id'], id ?? -1])
    }

    applyRef.current = {
      data: applyData,
      layers: applyLayers,
      rasters: applyRasters,
      selected: applySelected
    }

    map.on('load', () => {
      map.addSource('aoi', { type: 'geojson', data: EMPTY })
      map.addLayer({
        id: 'aoi-fill',
        type: 'fill',
        source: 'aoi',
        paint: { 'fill-color': '#2f7d32', 'fill-opacity': 0.12 }
      })
      map.addLayer({
        id: 'aoi-line',
        type: 'line',
        source: 'aoi',
        paint: { 'line-color': '#1b5e20', 'line-width': 2 }
      })

      map.addSource('zones', { type: 'geojson', data: EMPTY })
      map.addLayer({
        id: 'zones-fill',
        type: 'fill',
        source: 'zones',
        paint: {
          'fill-color': [
            'match',
            ['get', 'cause'],
            'confirmed',
            causeColor('confirmed'),
            'likely',
            causeColor('likely'),
            causeColor('undetermined')
          ],
          'fill-opacity': 0.55
        }
      })
      map.addLayer({
        id: 'zones-line',
        type: 'line',
        source: 'zones',
        paint: { 'line-color': '#263238', 'line-width': 1.2 }
      })
      map.addLayer({
        id: 'zones-selected-line',
        type: 'line',
        source: 'zones',
        filter: ['==', ['get', 'id'], -1],
        paint: { 'line-color': '#000000', 'line-width': 3 }
      })

      map.on('click', 'zones-fill', (event) => {
        const feature = event.features?.[0]
        if (!feature) return
        const properties = feature.properties ?? {}
        const zone = (dataRef.current.zones ?? []).find(
          (item) => String(item.id) === String(properties.id)
        )
        if (zone) zoneClickRef.current?.(zone)
        new Popup()
          .setLngLat(event.lngLat)
          .setHTML(
            `<strong>Зона ${properties.id}</strong><br/>` +
              `Площадь: ${formatNumber(Number(properties.area))} га<br/>` +
              `Вклад в ΔC: ${formatNumber(Number(properties.contribution))} т C<br/>` +
              `Причина: ${CAUSE_STATUS[properties.cause]?.label ?? properties.cause}`
          )
          .addTo(map)
      })
      map.on('mouseenter', 'zones-fill', () => {
        map.getCanvas().style.cursor = 'pointer'
      })
      map.on('mouseleave', 'zones-fill', () => {
        map.getCanvas().style.cursor = ''
      })

      applyData()
      applyLayers()
      applyRasters()
      applySelected()
      readyRef.current = true
    })

    mapRef.current = map
    return () => map.remove()
  }, [])

  useEffect(() => {
    if (readyRef.current) applyRef.current.data()
  }, [geometry, zones])

  useEffect(() => {
    if (readyRef.current) applyRef.current.layers()
  }, [visibleLayers])

  useEffect(() => {
    if (readyRef.current) applyRef.current.rasters()
  }, [rasters])

  useEffect(() => {
    if (readyRef.current) applyRef.current.selected()
  }, [selectedZoneId])

  return (
    <div
      ref={containerRef}
      style={{ height, borderRadius: 8, overflow: 'hidden', background: '#dfe7e2' }}
    />
  )
}
