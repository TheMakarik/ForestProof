import { useEffect, useRef } from 'react'
import { LngLatBounds, Map, NavigationControl, Popup, ScaleControl } from 'maplibre-gl'

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

const toFeature = (geometry, properties) => ({ type: 'Feature', geometry, properties })

const boundsOf = (geometry) => {
  const coordinates = []
  const collect = (value) => {
    if (typeof value[0] === 'number') coordinates.push(value)
    else value.forEach(collect)
  }
  collect(geometry.coordinates)
  if (coordinates.length === 0) return null
  return coordinates.reduce(
    (acc, coordinate) => acc.extend(coordinate),
    new LngLatBounds(coordinates[0], coordinates[0])
  )
}

export default function MapView({ geometry, zones = [], visibleLayers = {}, height = 460 }) {
  const containerRef = useRef(null)
  const mapRef = useRef(null)
  const dataRef = useRef({ geometry, zones })
  const layersRef = useRef(visibleLayers)
  const applyDataRef = useRef(() => {})
  const applyLayersRef = useRef(() => {})

  dataRef.current = { geometry, zones }
  layersRef.current = visibleLayers

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

      map.getSource('aoi')?.setData(
        aoi ? { type: 'FeatureCollection', features: [toFeature(aoi, {})] } : EMPTY
      )
      map.getSource('coverage')?.setData(
        aoi ? { type: 'FeatureCollection', features: [toFeature(aoi, {})] } : EMPTY
      )
      map.getSource('zones')?.setData({
        type: 'FeatureCollection',
        features: currentZones
          .filter((zone) => zone.geometry)
          .map((zone) =>
            toFeature(zone.geometry, {
              id: zone.id,
              area: zone.areaHectares,
              contribution: zone.contributionToDeltaCarbon,
              cause: zone.causeStatus,
              evidence: (zone.evidenceTypes ?? []).join(', ')
            })
          )
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
      setVisible('coverage-line', visible.coverage !== false)
      setVisible('zones-fill', visible.zones !== false)
      setVisible('zones-line', visible.zones !== false)
    }

    applyDataRef.current = applyData
    applyLayersRef.current = applyLayers

    map.on('load', () => {
      map.addSource('aoi', { type: 'geojson', data: EMPTY })
      map.addLayer({
        id: 'aoi-fill',
        type: 'fill',
        source: 'aoi',
        paint: { 'fill-color': '#2f7d32', 'fill-opacity': 0.18 }
      })
      map.addLayer({
        id: 'aoi-line',
        type: 'line',
        source: 'aoi',
        paint: { 'line-color': '#1b5e20', 'line-width': 2 }
      })

      map.addSource('coverage', { type: 'geojson', data: EMPTY })
      map.addLayer({
        id: 'coverage-line',
        type: 'line',
        source: 'coverage',
        paint: { 'line-color': '#1565c0', 'line-width': 1.5, 'line-dasharray': [2, 2] }
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
            '#c62828',
            'likely',
            '#ed6c02',
            '#607d8b'
          ],
          'fill-opacity': 0.5
        }
      })
      map.addLayer({
        id: 'zones-line',
        type: 'line',
        source: 'zones',
        paint: { 'line-color': '#263238', 'line-width': 1.2 }
      })

      map.on('click', 'zones-fill', (event) => {
        const feature = event.features?.[0]
        if (!feature) return
        const properties = feature.properties ?? {}
        new Popup()
          .setLngLat(event.lngLat)
          .setHTML(
            `<strong>Зона ${properties.id}</strong><br/>` +
              `Площадь: ${Number(properties.area).toFixed(2)} га<br/>` +
              `Вклад в ΔC: ${Number(properties.contribution).toFixed(2)} т C<br/>` +
              `Причина: ${properties.cause}`
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
    })

    mapRef.current = map
    return () => map.remove()
  }, [])

  useEffect(() => {
    if (mapRef.current?.getSource('aoi')) applyDataRef.current()
  }, [geometry, zones])

  useEffect(() => {
    applyLayersRef.current()
  }, [visibleLayers])

  return <div ref={containerRef} style={{ height, borderRadius: 8, overflow: 'hidden' }} />
}
