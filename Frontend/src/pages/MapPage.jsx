import { useEffect, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Card,
  Checkbox,
  Col,
  Descriptions,
  Divider,
  Empty,
  Progress,
  Row,
  Select,
  Space,
  Spin,
  Tag,
  Typography
} from 'antd'
import { api } from '../api/client'
import { useAnalysisRun } from '../hooks/useAnalysisRun'
import { useRasters } from '../hooks/useRasters'
import { useAreas } from '../hooks/queries'
import { getRunContext, saveRunContext } from '../utils/runContext'
import { pushRun } from '../utils/runHistory'
import MapView from '../components/MapView'
import ZoneDetails from '../components/ZoneDetails'
import ZonesTable from '../components/ZonesTable'
import {
  CAUSE_STATUS,
  DISCLAIMERS,
  EVIDENCE_LABELS,
  LAYER_DEFINITIONS,
  MAX_YEAR,
  METHOD_PROFILES,
  MIN_YEAR,
  YEARS,
  formatDate,
  formatNumber
} from '../constants'

const { Title, Paragraph, Text } = Typography

const causeMeta = (status) => CAUSE_STATUS[status] ?? { color: 'default', label: status ?? '—' }

const renderLegend = (legend) => {
  if (!legend) return null
  if (typeof legend === 'string') return legend
  if (Array.isArray(legend)) {
    return legend
      .map((item) => (typeof item === 'string' ? item : JSON.stringify(item)))
      .join(', ')
  }
  return Object.entries(legend)
    .map(([key, value]) => `${key}: ${value}`)
    .join('; ')
}

const toPercent = (progress) => {
  const value = Number(progress ?? 0)
  if (!Number.isFinite(value)) return 0
  return Math.max(0, Math.min(100, Math.round(value <= 1 ? value * 100 : value)))
}

export default function MapPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const initialRunId = searchParams.get('run')
  const initialContext = initialRunId ? getRunContext(initialRunId) : null

  const { data: areas = [] } = useAreas()

  const [aoiId, setAoiId] = useState(initialContext?.aoiId ?? 'RU_VOLOGDA_02')
  const [startYear, setStartYear] = useState(initialContext?.startYear ?? MIN_YEAR)
  const [endYear, setEndYear] = useState(initialContext?.endYear ?? MAX_YEAR)
  const [methodProfile, setMethodProfile] = useState(
    initialContext?.methodProfile ?? METHOD_PROFILES[0].value
  )
  const [runId, setRunId] = useState(initialRunId)
  const [selectedZoneId, setSelectedZoneId] = useState(null)
  const [visibleLayers, setVisibleLayers] = useState({ aoi: true, zones: true })
  const [geometry, setGeometry] = useState(null)
  const [layerError, setLayerError] = useState(null)

  useEffect(() => {
    if (areas.length === 0) return
    if (areas.some((area) => area.aoiId === aoiId)) return
    const preferred = areas.find((area) => area.aoiId === 'RU_VOLOGDA_02')
    setAoiId(preferred?.aoiId ?? areas[0].aoiId)
  }, [areas, aoiId])

  const invalidPeriod = Number(endYear) <= Number(startYear)

  const createRun = useMutation({
    mutationFn: (payload) => api.createAnalysis(payload),
    onSuccess: (run) => {
      const createdAt = new Date().toISOString()
      saveRunContext(run.id, { aoiId, startYear, endYear, methodProfile, createdAt })
      pushRun({
        runId: run.id,
        aoiId,
        name: aoiId,
        startYear,
        endYear,
        status: 'running',
        createdAt
      })
      setRunId(run.id)
      setSelectedZoneId(null)
      setGeometry(null)
      setLayerError(null)
      setVisibleLayers({ aoi: true, zones: true })
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev)
          next.set('run', run.id)
          return next
        },
        { replace: true }
      )
    }
  })

  const run = useAnalysisRun(runId)

  const availableMap = useMemo(
    () => Object.fromEntries((run.layers ?? []).map((layer) => [layer.key, layer])),
    [run.layers]
  )

  const layerOptions = useMemo(
    () =>
      LAYER_DEFINITIONS.map((layer) => {
        const backend = availableMap[layer.key]
        const computed = layer.kind === 'computed'
        const unavailable = Boolean(backend) && backend.available === false
        const reason =
          backend?.reason ?? (computed ? 'Вычисляемый слой доступен только в исследовании' : null)
        return {
          value: layer.key,
          disabled: computed || unavailable,
          reason,
          label: (
            <Space size={6}>
              <span>{layer.label}</span>
              {computed && <Text type="secondary">(вычисляемый)</Text>}
              {unavailable && !computed && <Text type="secondary">(недоступен)</Text>}
            </Space>
          )
        }
      }),
    [availableMap]
  )

  const unavailableLayers = useMemo(
    () =>
      LAYER_DEFINITIONS.map((layer) => ({ layer, backend: availableMap[layer.key] })).filter(
        ({ layer, backend }) =>
          layer.kind !== 'computed' && backend && backend.available === false
      ),
    [availableMap]
  )

  const rasterLegends = useMemo(
    () =>
      LAYER_DEFINITIONS.filter((layer) => layer.kind === 'raster')
        .map((layer) => ({ layer, backend: availableMap[layer.key] }))
        .filter(({ backend }) => backend?.available && backend.legend),
    [availableMap]
  )

  const visibleRasterKeys = useMemo(
    () =>
      LAYER_DEFINITIONS.filter(
        (layer) =>
          layer.kind === 'raster' && visibleLayers[layer.key] && availableMap[layer.key]?.available
      ).map((layer) => layer.key),
    [visibleLayers, availableMap]
  )

  const rasters = useRasters(run.ready ? runId : null, visibleRasterKeys)

  useEffect(() => {
    if (!run.ready || !runId) return undefined
    let cancelled = false
    api
      .getLayerGeoJson(runId, 'aoi')
      .then((collection) => {
        if (cancelled) return
        const feature =
          collection?.features?.[0] ?? (collection?.type === 'Feature' ? collection : null)
        setGeometry(feature?.geometry ?? null)
      })
      .catch((error) => {
        if (!cancelled) setLayerError(error.message)
      })
    return () => {
      cancelled = true
    }
  }, [run.ready, runId])

  const selectedZone = useMemo(
    () => run.zones.find((zone) => zone.id === selectedZoneId) ?? null,
    [run.zones, selectedZoneId]
  )

  const causeLegend = useMemo(
    () =>
      Object.values(CAUSE_STATUS).filter(
        (meta, index, all) =>
          all.findIndex((item) => item.label === meta.label && item.color === meta.color) === index
      ),
    []
  )

  const busy = runId && !run.ready && !run.failed

  return (
    <Space direction="vertical" size="large" style={{ width: '100%' }}>
      <div>
        <Title level={2} style={{ marginBottom: 4 }}>
          Карта
        </Title>
        <Paragraph type="secondary" style={{ marginBottom: 0 }}>
          Слои включаются независимо: граница AOI и зоны изменений — векторные, а GFC, MODIS и
          AGB-растры приходят из Go-шлюза. Растры запрашиваются только при включении слоя.
        </Paragraph>
      </div>

      <Card size="small" title="Параметры карты">
        <Space wrap size="middle" align="end">
          <Space direction="vertical" size={4}>
            <Text type="secondary">Территория</Text>
            <Select
              style={{ width: 280 }}
              value={aoiId}
              placeholder="Выберите участок"
              onChange={setAoiId}
              options={areas.map((area) => ({
                value: area.aoiId,
                label: `${area.aoiId} — ${area.name ?? area.aoiId}`
              }))}
            />
          </Space>

          <Space direction="vertical" size={4}>
            <Text type="secondary">Начальный год</Text>
            <Select
              style={{ width: 120 }}
              value={startYear}
              onChange={setStartYear}
              options={YEARS.map((year) => ({ value: year, label: year }))}
            />
          </Space>

          <Space direction="vertical" size={4}>
            <Text type="secondary">Конечный год</Text>
            <Select
              style={{ width: 120 }}
              value={endYear}
              status={invalidPeriod ? 'error' : undefined}
              onChange={setEndYear}
              options={YEARS.map((year) => ({ value: year, label: year }))}
            />
          </Space>

          <Space direction="vertical" size={4}>
            <Text type="secondary">Профиль метода</Text>
            <Select
              style={{ width: 280 }}
              value={methodProfile}
              onChange={setMethodProfile}
              options={METHOD_PROFILES.map((profile) => ({
                value: profile.value,
                label: profile.label
              }))}
            />
          </Space>

          <Button
            type="primary"
            onClick={() => createRun.mutate({ aoiId, startYear, endYear, methodProfile })}
            loading={createRun.isPending}
            disabled={!aoiId || invalidPeriod}
          >
            Построить карту
          </Button>
        </Space>

        {invalidPeriod && (
          <Alert
            style={{ marginTop: 12 }}
            type="warning"
            showIcon
            message="Конечный год должен быть больше начального"
          />
        )}

        {createRun.isError && (
          <Alert
            style={{ marginTop: 12 }}
            type="error"
            showIcon
            message="Не удалось создать анализ"
            description={createRun.error?.message}
          />
        )}
      </Card>

      {runId && (
        <Card size="small" title="Запуск">
          <Descriptions size="small" column={{ xs: 1, sm: 2, md: 4 }}>
            <Descriptions.Item label="run_id">{runId}</Descriptions.Item>
            <Descriptions.Item label="Территория">{run.summary?.aoiId ?? aoiId}</Descriptions.Item>
            <Descriptions.Item label="Период">
              {run.summary?.startYear ?? startYear}–{run.summary?.endYear ?? endYear}
            </Descriptions.Item>
            <Descriptions.Item label="Создан">
              {formatDate(run.status?.createdAt ?? run.summary?.createdAt)}
            </Descriptions.Item>
          </Descriptions>
        </Card>
      )}

      {busy && (
        <Card size="small">
          <Space
            direction="vertical"
            size="middle"
            align="center"
            style={{ width: '100%', padding: 24 }}
          >
            <Spin size="large" />
            <Text>{run.phase || 'Выполняется расчёт…'}</Text>
            <Progress
              percent={toPercent(run.progress)}
              status="active"
              style={{ maxWidth: 420, width: '100%' }}
            />
            <Text type="secondary">Запуск {runId}</Text>
          </Space>
        </Card>
      )}

      {run.failed && (
        <Alert
          type="error"
          showIcon
          message="Расчёт завершился с ошибкой"
          description={run.status?.errorMessage ?? 'Backend вернул статус failed.'}
        />
      )}

      {run.isError && (
        <Alert
          type="error"
          showIcon
          message="Ошибка запроса к Go-шлюзу"
          description={run.error?.message}
        />
      )}

      {run.warnings.length > 0 && (
        <Alert
          type="info"
          showIcon
          message="Предупреждения расчёта"
          description={
            <ul style={{ margin: 0, paddingLeft: 18 }}>
              {run.warnings.map((warning) => (
                <li key={warning}>{warning}</li>
              ))}
            </ul>
          }
        />
      )}

      {run.ready && (
        <>
          <Card
            size="small"
            title="Слои"
            extra={
              <Space wrap>
                {causeLegend.map((meta) => (
                  <Tag key={meta.label} color={meta.color}>
                    {meta.label}
                  </Tag>
                ))}
              </Space>
            }
          >
            <Checkbox.Group
              style={{ width: '100%' }}
              options={layerOptions}
              value={Object.keys(visibleLayers).filter((key) => visibleLayers[key])}
              onChange={(values) =>
                setVisibleLayers(
                  Object.fromEntries(
                    LAYER_DEFINITIONS.map((layer) => [layer.key, values.includes(layer.key)])
                  )
                )
              }
            />

            {unavailableLayers.length > 0 && (
              <Alert
                style={{ marginTop: 12 }}
                type="info"
                showIcon
                message="Недоступные слои"
                description={
                  <ul style={{ margin: 0, paddingLeft: 18 }}>
                    {unavailableLayers.map(({ layer, backend }) => (
                      <li key={layer.key}>
                        {layer.label} — {backend.reason ?? 'нет данных от шлюза'}
                      </li>
                    ))}
                  </ul>
                }
              />
            )}

            {rasterLegends.length > 0 && (
              <>
                <Divider style={{ margin: '16px 0 8px' }} />
                <Text type="secondary">Условные обозначения растровых слоёв</Text>
                <ul style={{ margin: '8px 0 0', paddingLeft: 18 }}>
                  {rasterLegends.map(({ layer, backend }) => (
                    <li key={layer.key}>
                      <Text strong>{layer.label}: </Text>
                      <Text type="secondary">{renderLegend(backend.legend)}</Text>
                    </li>
                  ))}
                </ul>
              </>
            )}
          </Card>

          <Row gutter={[16, 16]}>
            <Col xs={24} lg={16}>
              <Card size="small" title="Карта">
                <MapView
                  geometry={geometry}
                  zones={run.zones}
                  rasters={rasters}
                  visibleLayers={visibleLayers}
                  height={560}
                  onZoneClick={(zone) => setSelectedZoneId(zone?.id ?? null)}
                  selectedZoneId={selectedZoneId}
                />
                {layerError && (
                  <Alert
                    style={{ marginTop: 12 }}
                    type="warning"
                    showIcon
                    message="Не удалось загрузить геометрию AOI"
                    description={layerError}
                  />
                )}
              </Card>
            </Col>

            <Col xs={24} lg={8}>
              <Space direction="vertical" size="middle" style={{ width: '100%' }}>
                <ZoneDetails zone={selectedZone} />

                <Card size="small" title={`Зоны изменений (${run.zones.length})`}>
                  {run.zones.length === 0 ? (
                    <Empty description="Зоны изменений не выявлены" />
                  ) : (
                    <Space direction="vertical" size="small" style={{ width: '100%' }}>
                      {run.zones.map((zone) => {
                        const cause = causeMeta(zone.causeStatus)
                        const selected = selectedZoneId === zone.id
                        return (
                          <Card
                            key={zone.id}
                            size="small"
                            hoverable
                            onClick={() => setSelectedZoneId(zone.id)}
                            style={{
                              cursor: 'pointer',
                              borderColor: selected ? '#2f7d32' : undefined
                            }}
                          >
                            <Space direction="vertical" size={4} style={{ width: '100%' }}>
                              <Space>
                                <Text strong>Зона {zone.id}</Text>
                                <Tag color={cause.color}>{cause.label}</Tag>
                              </Space>
                              <Text type="secondary">
                                Площадь: {formatNumber(zone.areaHectares)} га
                              </Text>
                              <Text
                                type={
                                  zone.contributionToDeltaCarbon < 0 ? 'danger' : 'success'
                                }
                              >
                                Вклад в ΔC: {formatNumber(zone.contributionToDeltaCarbon)} т C
                              </Text>
                              <Text type="secondary">
                                Подтверждения:{' '}
                                {zone.evidenceTypes.length === 0
                                  ? '—'
                                  : zone.evidenceTypes
                                      .map((type) => EVIDENCE_LABELS[type] ?? type)
                                      .join(', ')}
                              </Text>
                            </Space>
                          </Card>
                        )
                      })}
                    </Space>
                  )}
                </Card>
              </Space>
            </Col>
          </Row>

          <Card size="small" title="Таблица зон">
            <ZonesTable
              zones={run.zones}
              onSelect={(zone) => setSelectedZoneId(zone?.id ?? null)}
              selectedId={selectedZoneId}
            />
          </Card>

          <Space wrap>
            <Link to={`/analysis?run=${runId}`}>
              <Button type="primary">Открыть результаты</Button>
            </Link>
            <Link to={`/report?run=${runId}`}>
              <Button>Открыть отчёт</Button>
            </Link>
          </Space>

          <Paragraph type="secondary" style={{ fontSize: 12, marginBottom: 0 }}>
            {DISCLAIMERS.cause}
          </Paragraph>
        </>
      )}
    </Space>
  )
}
