import { useEffect, useMemo, useRef, useState } from 'react'
import { useSearchParams, useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Card,
  Checkbox,
  Col,
  Descriptions,
  Result,
  Row,
  Space,
  Spin,
  Typography
} from 'antd'
import { api } from '../api/client'
import { useAnalysisRun } from '../hooks/useAnalysisRun'
import { useRasters } from '../hooks/useRasters'
import { useAreas } from '../hooks/queries'
import { getRunContext, saveRunContext } from '../utils/runContext'
import { pushRun, updateRun } from '../utils/runHistory'
import { DISCLAIMERS, LAYER_DEFINITIONS, formatDate, formatNumber } from '../constants'
import MetricCard from '../components/MetricCard'
import StatusTag from '../components/StatusTag'
import YearlyChart from '../components/YearlyChart'
import UncertaintyChart from '../components/UncertaintyChart'
import UnitsPanel from '../components/UnitsPanel'
import ZonesTable from '../components/ZonesTable'
import ZoneDetails from '../components/ZoneDetails'
import ProgressPanel from '../components/ProgressPanel'
import MapView from '../components/MapView'

const { Title, Text } = Typography

const DEFAULT_VISIBLE_LAYERS = ['aoi', 'zones', 'coverage']

const initialVisibleLayers = () =>
  Object.fromEntries(
    LAYER_DEFINITIONS.filter((layer) => layer.kind !== 'computed').map((layer) => [
      layer.key,
      DEFAULT_VISIBLE_LAYERS.includes(layer.key)
    ])
  )

const layerLabel = (key) =>
  LAYER_DEFINITIONS.find((layer) => layer.key === key)?.label ?? key

const parseGeometry = (raw) => {
  if (!raw) return null
  try {
    const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw
    if (!parsed) return null
    if (parsed.type === 'FeatureCollection') return parsed.features?.[0]?.geometry ?? null
    if (parsed.type === 'Feature') return parsed.geometry ?? null
    return parsed
  } catch {
    return null
  }
}

export default function AnalysisResultPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()

  const [visibleLayers, setVisibleLayers] = useState(initialVisibleLayers)
  const [selectedZone, setSelectedZone] = useState(null)
  const [aoiGeometry, setAoiGeometry] = useState(null)

  const startRef = useRef(Date.now())
  const createRef = useRef(false)
  const startedRef = useRef(null)
  const finalizedRef = useRef(null)

  const run = searchParams.get('run')
  const legacyAoiId = searchParams.get('aoiId') ?? undefined
  const legacyGeometry = searchParams.get('geometry') ?? undefined
  const legacyStartYear = Number(searchParams.get('start') ?? 2019)
  const legacyEndYear = Number(searchParams.get('end') ?? 2024)

  const context = useMemo(() => (run ? getRunContext(run) : null), [run])

  const areasQuery = useAreas()
  const areas = areasQuery.data ?? []

  const areaName = useMemo(() => {
    const id = context?.aoiId || legacyAoiId
    if (!id) return null
    return areas.find((area) => area.aoiId === id)?.name ?? null
  }, [areas, context, legacyAoiId])

  const createMutation = useMutation({
    mutationFn: () =>
      api.createAnalysis({
        aoiId: legacyAoiId,
        polygonGeoJson: legacyGeometry,
        startYear: legacyStartYear,
        endYear: legacyEndYear
      }),
    onSuccess: (data) => {
      saveRunContext(data.id, {
        aoiId: data.aoiId || legacyAoiId || '',
        name: areaName ?? legacyAoiId ?? 'Произвольный контур',
        polygonGeoJson: legacyGeometry ?? null,
        startYear: data.startYear ?? legacyStartYear,
        endYear: data.endYear ?? legacyEndYear
      })
      setSearchParams({ run: data.id }, { replace: true })
    }
  })

  useEffect(() => {
    if (run || createRef.current) return
    if (!legacyAoiId && !legacyGeometry) return
    createRef.current = true
    createMutation.mutate()
  }, [run, legacyAoiId, legacyGeometry, createMutation])

  useEffect(() => {
    startRef.current = Date.now()
  }, [run])

  const analysis = useAnalysisRun(run)

  const runStatus = analysis.status?.status ?? null
  const terminal = analysis.ready || analysis.failed

  useEffect(() => {
    if (!run || !runStatus) return
    if (startedRef.current === run) return
    startedRef.current = run
    const ctx = getRunContext(run)
    pushRun({
      runId: run,
      aoiId: analysis.summary?.aoiId || ctx?.aoiId || legacyAoiId || '',
      name: ctx?.name ?? legacyAoiId ?? 'Произвольный контур',
      startYear: analysis.summary?.startYear ?? ctx?.startYear ?? legacyStartYear,
      endYear: analysis.summary?.endYear ?? ctx?.endYear ?? legacyEndYear,
      status: runStatus,
      createdAt:
        analysis.summary?.createdAt ?? analysis.status?.createdAt ?? new Date().toISOString(),
      durationMs: null
    })
  }, [run, runStatus, analysis.summary, analysis.status, legacyAoiId, legacyStartYear, legacyEndYear])

  useEffect(() => {
    if (!run || !runStatus || !terminal) return
    if (analysis.ready && !analysis.summary) return
    if (finalizedRef.current === run) return
    finalizedRef.current = run
    const ctx = getRunContext(run)
    updateRun(run, {
      aoiId: analysis.summary?.aoiId || ctx?.aoiId || legacyAoiId || '',
      name: ctx?.name ?? legacyAoiId ?? 'Произвольный контур',
      startYear: analysis.summary?.startYear ?? ctx?.startYear ?? legacyStartYear,
      endYear: analysis.summary?.endYear ?? ctx?.endYear ?? legacyEndYear,
      status: runStatus,
      createdAt: analysis.summary?.createdAt ?? analysis.status?.createdAt,
      durationMs: Date.now() - startRef.current
    })
  }, [run, runStatus, terminal, analysis.ready, analysis.summary, analysis.status, legacyAoiId, legacyStartYear, legacyEndYear])

  const summary = analysis.summary

  useEffect(() => {
    if (!run || !summary?.aoiId) {
      setAoiGeometry(null)
      return undefined
    }
    let cancelled = false
    api
      .getLayerGeoJson(run, 'aoi')
      .then((collection) => {
        if (cancelled) return
        setAoiGeometry(collection?.features?.[0]?.geometry ?? collection?.geometry ?? null)
      })
      .catch(() => {
        if (!cancelled) setAoiGeometry(null)
      })
    return () => {
      cancelled = true
    }
  }, [run, summary?.aoiId])

  const mapGeometry = useMemo(() => {
    if (summary?.aoiId) return aoiGeometry
    return parseGeometry(context?.polygonGeoJson)
  }, [summary?.aoiId, aoiGeometry, context])

  const layers = analysis.layers

  const availableLayerKeys = useMemo(
    () => new Set(layers.filter((layer) => layer.available).map((layer) => layer.key)),
    [layers]
  )

  const selectableLayers = useMemo(
    () => LAYER_DEFINITIONS.filter((layer) => layer.kind !== 'computed'),
    []
  )

  const visibleRasterKeys = useMemo(
    () =>
      LAYER_DEFINITIONS.filter(
        (layer) =>
          layer.kind === 'raster' && visibleLayers[layer.key] && availableLayerKeys.has(layer.key)
      ).map((layer) => layer.key),
    [visibleLayers, availableLayerKeys]
  )

  const rasters = useRasters(run, visibleRasterKeys)

  const handleZoneClick = (zone) => {
    if (!zone) return
    if (typeof zone === 'object') {
      setSelectedZone(zone)
      return
    }
    setSelectedZone(analysis.zones.find((item) => item.id === zone) ?? null)
  }

  if (!run) {
    if (createMutation.isError) {
      return (
        <Result
          status="error"
          title="Не удалось создать анализ"
          subTitle={createMutation.error?.message}
          extra={
            <Button type="primary" onClick={() => createMutation.mutate()}>
              Повторить
            </Button>
          }
        />
      )
    }
    if (legacyAoiId || legacyGeometry || createMutation.isPending) {
      return (
        <Space direction="vertical" align="center" style={{ width: '100%', padding: 64 }}>
          <Spin size="large" />
          <Text type="secondary">Создаётся анализ…</Text>
        </Space>
      )
    }
    return (
      <Result
        status="info"
        title="Выберите территорию"
        subTitle="Откройте участок с дашборда или создайте проверку."
      />
    )
  }

  const displayName = summary?.aoiId || context?.aoiId || legacyAoiId || 'Произвольный контур'
  const periodStart = summary?.startYear ?? context?.startYear ?? legacyStartYear
  const periodEnd = summary?.endYear ?? context?.endYear ?? legacyEndYear
  const periodYears = periodEnd - periodStart

  const series = summary?.yearlySeries ?? []
  const first = series[0]
  const last = series[series.length - 1]

  const deltaCarbon = summary?.change.deltaCarbon ?? 0
  const emission = summary?.change.projectEmission ?? 0

  const inProgress = !terminal

  const unavailableLayers = layers.filter((layer) => !layer.available)
  const describedLayers = layers.filter(
    (layer) => layer.available && (layer.description || layer.legend)
  )

  const header = (
    <div>
      <Space align="center" wrap>
        <Title level={2} style={{ margin: 0 }}>
          {displayName}
        </Title>
        <StatusTag type="run" value={runStatus ?? 'validating'} />
        <Text type="secondary">
          {periodStart}–{periodEnd}, {periodYears} лет
        </Text>
      </Space>
      <Descriptions
        size="small"
        column={{ xs: 1, sm: 2, md: 3 }}
        style={{ marginTop: 8 }}
      >
        <Descriptions.Item label="run_id">{run}</Descriptions.Item>
        <Descriptions.Item label="method_version">
          {summary?.methodVersion ?? analysis.status?.methodVersion ?? '—'}
        </Descriptions.Item>
        <Descriptions.Item label="data_version">
          {summary?.dataVersion ?? analysis.status?.dataVersion ?? '—'}
        </Descriptions.Item>
        <Descriptions.Item label="input_hash">{summary?.inputHash ?? '—'}</Descriptions.Item>
        <Descriptions.Item label="Дата расчёта">
          {formatDate(summary?.createdAt ?? analysis.status?.createdAt)}
        </Descriptions.Item>
      </Descriptions>
      <Space style={{ marginTop: 8 }} wrap>
        <Button type="primary" onClick={() => navigate(`/report?run=${run}`)}>
          Открыть отчёт
        </Button>
        <Button onClick={() => navigate('/map')}>На карту</Button>
      </Space>
    </div>
  )

  return (
    <Space direction="vertical" size="large" style={{ width: '100%' }}>
      {header}

      {analysis.isError && (
        <Result
          status="error"
          title="Ошибка загрузки анализа"
          subTitle={analysis.error?.message}
          extra={
            <Button type="primary" onClick={() => analysis.refetch()}>
              Повторить
            </Button>
          }
        />
      )}

      {!analysis.isError && analysis.failed && (
        <Result
          status="error"
          title="Расчёт не выполнен"
          subTitle={analysis.status?.errorMessage ?? 'Неизвестная ошибка'}
          extra={
            <Button type="primary" onClick={() => analysis.refetch()}>
              Повторить
            </Button>
          }
        />
      )}

      {!analysis.isError && !analysis.failed && inProgress && (
        <ProgressPanel
          status={runStatus ?? 'validating'}
          progress={analysis.progress}
          phase={analysis.phase}
          errorMessage={analysis.status?.errorMessage}
          warnings={analysis.warnings}
        />
      )}

      {!analysis.isError && !analysis.failed && !inProgress && !summary && (
        <Space direction="vertical" align="center" style={{ width: '100%', padding: 64 }}>
          <Spin size="large" />
          <Text type="secondary">Загрузка результатов…</Text>
        </Space>
      )}

      {!analysis.isError && !analysis.failed && summary && (
        <>
          <Alert
            type="warning"
            showIcon
            message="Потенциальные, не сертифицированные единицы"
            description={
              <>
                <div>{DISCLAIMERS.units}</div>
                <div>{DISCLAIMERS.emission}</div>
                <div>{DISCLAIMERS.uncertainty}</div>
              </>
            }
          />

          {summary.warnings?.length > 0 && (
            <Alert
              type="info"
              showIcon
              message="Предупреждения"
              description={
                <ul style={{ margin: 0, paddingLeft: 18 }}>
                  {summary.warnings.map((warning) => (
                    <li key={warning}>{warning}</li>
                  ))}
                </ul>
              }
            />
          )}

          <Card title="Результаты" size="small">
            <Row gutter={[12, 12]}>
              <Col xs={12} md={6}>
                <MetricCard
                  title="Площадь полигона"
                  value={formatNumber(summary.polygonAreaHectares)}
                  unit="га"
                />
              </Col>
              <Col xs={12} md={6}>
                <MetricCard
                  title="Рассчитанная площадь"
                  value={formatNumber(last?.areaHectares)}
                  unit="га"
                />
              </Col>
              <Col xs={12} md={6}>
                <MetricCard
                  title={`c̄_t0, ${periodStart}`}
                  value={formatNumber(first?.meanCarbonPerHectare, 3)}
                  unit="т C/га"
                />
              </Col>
              <Col xs={12} md={6}>
                <MetricCard
                  title={`c̄_t1, ${periodEnd}`}
                  value={formatNumber(last?.meanCarbonPerHectare, 3)}
                  unit="т C/га"
                />
              </Col>
              <Col xs={12} md={6}>
                <MetricCard title="C_t0" value={formatNumber(first?.totalCarbon)} unit="т C" />
              </Col>
              <Col xs={12} md={6}>
                <MetricCard title="C_t1" value={formatNumber(last?.totalCarbon)} unit="т C" />
              </Col>
              <Col xs={12} md={6}>
                <MetricCard
                  title="ΔC"
                  value={formatNumber(deltaCarbon)}
                  unit="т C"
                  tone={deltaCarbon < 0 ? 'loss' : deltaCarbon > 0 ? 'gain' : 'default'}
                  hint="ΔC = C_t1 − C_t0. Положительное — запас вырос."
                />
              </Col>
              <Col xs={12} md={6}>
                <MetricCard
                  title="Eproj"
                  value={formatNumber(emission)}
                  unit="т CO₂-экв."
                  tone={emission > 0 ? 'loss' : emission < 0 ? 'gain' : 'default'}
                  hint="Eproj = −ΔC × 44/12. Положительное означает потерю учитываемого пула."
                />
              </Col>
              <Col xs={12} md={6}>
                <MetricCard
                  title="e"
                  value={formatNumber(summary.change.emissionPerHectarePerYear, 3)}
                  unit="т CO₂-экв./га/год"
                />
              </Col>
              <Col xs={12} md={6}>
                <MetricCard
                  title="L"
                  value={formatNumber(summary.uncertainty.lower)}
                  unit="т CO₂-экв."
                />
              </Col>
              <Col xs={12} md={6}>
                <MetricCard
                  title="U"
                  value={formatNumber(summary.uncertainty.upper)}
                  unit="т CO₂-экв."
                />
              </Col>
              <Col xs={12} md={6}>
                <MetricCard
                  title="H"
                  value={formatNumber(summary.uncertainty.halfWidth)}
                  unit="т CO₂-экв."
                  hint="H = max(Eproj − L, U − Eproj)."
                />
              </Col>
              <Col xs={12} md={6}>
                <MetricCard
                  title={`Покрытие t0, ${periodStart}`}
                  value={formatNumber((first?.coverage ?? 0) * 100, 1)}
                  unit="%"
                  tone={first && first.coverage >= 1 ? 'default' : 'loss'}
                />
              </Col>
              <Col xs={12} md={6}>
                <MetricCard
                  title={`Покрытие t1, ${periodEnd}`}
                  value={formatNumber((last?.coverage ?? 0) * 100, 1)}
                  unit="%"
                  tone={last && last.coverage >= 1 ? 'default' : 'loss'}
                />
              </Col>
              <Col xs={12} md={6}>
                <MetricCard
                  title="Ebase"
                  value={formatNumber(summary.baseline?.baselineEmission)}
                  unit="т CO₂-экв."
                />
              </Col>
              {summary.cciChangeMeanTonnesPerHectare != null && (
                <Col xs={12} md={6}>
                  <MetricCard
                    title="CCI Change (2019–2020)"
                    value={formatNumber(summary.cciChangeMeanTonnesPerHectare, 2)}
                    unit="т/га"
                  />
                </Col>
              )}
            </Row>
          </Card>

          <Row gutter={[16, 16]}>
            <Col xs={24} lg={14}>
              <Card title="Годовая динамика" size="small">
                <YearlyChart series={summary.yearlySeries} />
              </Card>
            </Col>
            <Col xs={24} lg={10}>
              <Card title="Неопределённость и единицы" size="small">
                <UncertaintyChart
                  uncertainty={summary.uncertainty}
                  projectEmission={summary.change.projectEmission}
                />
                <div style={{ marginTop: 16 }}>
                  <UnitsPanel units={summary.units} baseline={summary.baseline} />
                </div>
              </Card>
            </Col>
          </Row>

          <Card title="Карта и зоны изменений" size="small">
            <Space direction="vertical" size="middle" style={{ width: '100%' }}>
              <Checkbox.Group
                options={selectableLayers.map((layer) => ({
                  label: layer.label,
                  value: layer.key
                }))}
                value={selectableLayers
                  .filter((layer) => visibleLayers[layer.key])
                  .map((layer) => layer.key)}
                onChange={(values) =>
                  setVisibleLayers(
                    Object.fromEntries(
                      selectableLayers.map((layer) => [layer.key, values.includes(layer.key)])
                    )
                  )
                }
              />

              {unavailableLayers.length > 0 && (
                <Alert
                  type="warning"
                  showIcon
                  message="Недоступные слои"
                  description={
                    <ul style={{ margin: 0, paddingLeft: 18 }}>
                      {unavailableLayers.map((layer) => (
                        <li key={layer.key}>
                          {layerLabel(layer.key)}: {layer.reason ?? 'причина не указана'}
                        </li>
                      ))}
                    </ul>
                  }
                />
              )}

              {describedLayers.length > 0 && (
                <Space direction="vertical" size={4} style={{ width: '100%' }}>
                  {describedLayers.map((layer) => (
                    <div key={layer.key}>
                      <Text strong style={{ fontSize: 12 }}>
                        {layerLabel(layer.key)}
                      </Text>
                      {layer.description && (
                        <Text type="secondary" style={{ marginLeft: 8, fontSize: 12 }}>
                          {layer.description}
                        </Text>
                      )}
                      {layer.legend != null && (
                        <Text type="secondary" style={{ display: 'block', fontSize: 12 }}>
                          Легенда:{' '}
                          {typeof layer.legend === 'string'
                            ? layer.legend
                            : JSON.stringify(layer.legend)}
                        </Text>
                      )}
                    </div>
                  ))}
                </Space>
              )}

              <Row gutter={16}>
                <Col xs={24} lg={17}>
                  <MapView
                    geometry={mapGeometry}
                    zones={analysis.zones}
                    rasters={rasters}
                    visibleLayers={visibleLayers}
                    height={520}
                    onZoneClick={handleZoneClick}
                    selectedZoneId={selectedZone?.id}
                  />
                </Col>
                <Col xs={24} lg={7}>
                  {selectedZone ? (
                    <ZoneDetails zone={selectedZone} />
                  ) : (
                    <Text type="secondary">
                      Клик по зоне на карте или в таблице показывает площадь, вклад и статус
                      причины.
                    </Text>
                  )}
                </Col>
              </Row>

              <ZonesTable
                zones={analysis.zones}
                onSelect={setSelectedZone}
                selectedId={selectedZone?.id}
              />
            </Space>
          </Card>
        </>
      )}
    </Space>
  )
}
