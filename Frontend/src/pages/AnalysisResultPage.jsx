import { useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
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
import { DownloadOutlined, PrinterOutlined } from '@ant-design/icons'
import { api, buildReportHtml } from '../api/client'
import { formatNumber, LAYER_DEFINITIONS } from '../constants'
import MetricCard from '../components/MetricCard'
import StatusTag from '../components/StatusTag'
import YearlyChart from '../components/YearlyChart'
import UncertaintyChart from '../components/UncertaintyChart'
import UnitsPanel from '../components/UnitsPanel'
import ZonesTable from '../components/ZonesTable'
import MapView from '../components/MapView'

const { Title, Paragraph, Text } = Typography

export default function AnalysisResultPage() {
  const [searchParams] = useSearchParams()
  const [visibleLayers, setVisibleLayers] = useState({
    aoi: true,
    zones: true,
    gfc: false,
    modis: false,
    coverage: true
  })

  const aoiId = searchParams.get('aoiId') ?? undefined
  const startYear = Number(searchParams.get('start') ?? 2019)
  const endYear = Number(searchParams.get('end') ?? 2024)
  const geometry = searchParams.get('geometry') ?? undefined

  const query = useQuery({
    queryKey: ['analysis', aoiId, startYear, endYear, geometry],
    queryFn: () =>
      api.createAnalysis({ aoiId, startYear, endYear, polygonGeoJson: geometry }),
    enabled: Boolean(aoiId || geometry)
  })

  const summary = query.data

  const first = summary?.yearlySeries?.[0]
  const last = summary?.yearlySeries?.[summary.yearlySeries.length - 1]

  const cards = useMemo(() => {
    if (!summary) return null
    const emission = summary.change.projectEmission
    return (
      <Row gutter={[12, 12]}>
        <Col xs={12} md={6}>
          <MetricCard title="Площадь полигона" value={formatNumber(summary.polygonAreaHectares)} unit="га" />
        </Col>
        <Col xs={12} md={6}>
          <MetricCard title="Рассчитанная площадь" value={formatNumber(last.areaHectares)} unit="га" />
        </Col>
        <Col xs={12} md={6}>
          <MetricCard
            title={`c̄_t0, ${summary.startYear}`}
            value={formatNumber(first.meanCarbonPerHectare, 3)}
            unit="т C/га"
          />
        </Col>
        <Col xs={12} md={6}>
          <MetricCard
            title={`c̄_t1, ${summary.endYear}`}
            value={formatNumber(last.meanCarbonPerHectare, 3)}
            unit="т C/га"
          />
        </Col>
        <Col xs={12} md={6}>
          <MetricCard title="C_t0" value={formatNumber(first.totalCarbon)} unit="т C" />
        </Col>
        <Col xs={12} md={6}>
          <MetricCard title="C_t1" value={formatNumber(last.totalCarbon)} unit="т C" />
        </Col>
        <Col xs={12} md={6}>
          <MetricCard
            title="ΔC"
            value={formatNumber(summary.change.deltaCarbon)}
            unit="т C"
            tone={summary.change.deltaCarbon < 0 ? 'loss' : 'gain'}
            hint="ΔC = C_t1 − C_t0. Положительное — запас вырос."
          />
        </Col>
        <Col xs={12} md={6}>
          <MetricCard
            title="Eproj"
            value={formatNumber(emission)}
            unit="т CO₂-экв."
            tone={emission > 0 ? 'loss' : 'gain'}
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
          <MetricCard title="L" value={formatNumber(summary.uncertainty.lower)} unit="т CO₂-экв." />
        </Col>
        <Col xs={12} md={6}>
          <MetricCard title="U" value={formatNumber(summary.uncertainty.upper)} unit="т CO₂-экв." />
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
            title="Покрытие t1"
            value={formatNumber(last.coverage * 100, 1)}
            unit="%"
            tone={last.coverage >= 1 ? 'default' : 'loss'}
          />
        </Col>
        <Col xs={12} md={6}>
          <MetricCard
            title="CCI Change (2019–2020)"
            value={formatNumber(summary.cciChangeMeanTonnesPerHectare, 2)}
            unit="т/га"
          />
        </Col>
      </Row>
    )
  }, [summary, first, last])

  const downloadHtml = () => {
    const blob = new Blob([buildReportHtml(summary)], { type: 'text/html;charset=utf-8' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `forestproof-${summary.aoiId}-${summary.startYear}-${summary.endYear}.html`
    link.click()
    URL.revokeObjectURL(url)
  }

  const printPdf = () => {
    const report = window.open('', '_blank')
    report.document.write(buildReportHtml(summary))
    report.document.close()
    report.focus()
    report.print()
  }

  if (!aoiId && !geometry) {
    return <Result status="info" title="Выберите территорию" subTitle="Откройте участок с дашборда или создайте проверку." />
  }

  if (query.isLoading) {
    return (
      <Space direction="vertical" align="center" style={{ width: '100%', padding: 64 }}>
        <Spin size="large" />
        <Text type="secondary">Выполняется расчёт: геометрия → маска пикселей → запас → неопределённость → зоны → baseline → единицы.</Text>
      </Space>
    )
  }

  if (query.isError) {
    return <Result status="error" title="Ошибка расчёта" subTitle={query.error.message} />
  }

  return (
    <Space direction="vertical" size="large" style={{ width: '100%' }}>
      <div>
        <Space align="center" wrap>
          <Title level={2} style={{ margin: 0 }}>
            {summary.aoiId}
          </Title>
          <StatusTag type="run" value={summary.status} />
          <Text type="secondary">
            {summary.startYear}–{summary.endYear}, {summary.endYear - summary.startYear} лет
          </Text>
        </Space>
        <Descriptions size="small" column={{ xs: 1, sm: 2, md: 4 }} style={{ marginTop: 8 }}>
          <Descriptions.Item label="run_id">{summary.runId}</Descriptions.Item>
          <Descriptions.Item label="method_version">{summary.methodVersion}</Descriptions.Item>
          <Descriptions.Item label="data_version">{summary.dataVersion}</Descriptions.Item>
          <Descriptions.Item label="Дата расчёта">
            {new Date(summary.createdAt).toLocaleString('ru-RU')}
          </Descriptions.Item>
        </Descriptions>
      </div>

      <Alert
        type="warning"
        showIcon
        message="Потенциальные, не сертифицированные единицы"
        description="Eproj не означает немедленный выброс. GFC подтверждает факт потери, но не причину. Диапазон L–U — сценарный, не доверительный интервал."
      />

      {summary.warnings.length > 0 && (
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
        {cards}
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
              <UnitsPanel units={summary.units} />
            </div>
          </Card>
        </Col>
      </Row>

      <Card
        title="Карта и зоны изменений"
        size="small"
        extra={
          <Space>
            <Button icon={<DownloadOutlined />} onClick={downloadHtml}>
              HTML
            </Button>
            <Button icon={<PrinterOutlined />} onClick={printPdf}>
              PDF (печать)
            </Button>
          </Space>
        }
      >
        <Space direction="vertical" size="middle" style={{ width: '100%' }}>
          <Checkbox.Group
            options={LAYER_DEFINITIONS.map((layer) => ({ label: layer.label, value: layer.key }))}
            value={Object.keys(visibleLayers).filter((key) => visibleLayers[key])}
            onChange={(values) =>
              setVisibleLayers({
                aoi: values.includes('aoi'),
                zones: values.includes('zones'),
                coverage: values.includes('coverage'),
                gfc: values.includes('gfc'),
                modis: values.includes('modis')
              })
            }
          />
          <MapView
            geometry={summary.geometry}
            zones={summary.changeZones}
            visibleLayers={visibleLayers}
          />
          <Text type="secondary" style={{ fontSize: 12 }}>
            Слои GFC, MODIS и AGB-растры подключаются после интеграции с Go API. Клик по зоне
            показывает площадь, вклад и статус причины.
          </Text>
          <ZonesTable zones={summary.changeZones} />
        </Space>
      </Card>

      <Paragraph type="secondary" style={{ fontSize: 12, marginBottom: 0 }}>
        Положительный Eproj означает потерю углерода из учитываемого пула, но не доказывает
        немедленный выброс. Учитывается только живая надземная древесная биомасса.
      </Paragraph>
    </Space>
  )
}
