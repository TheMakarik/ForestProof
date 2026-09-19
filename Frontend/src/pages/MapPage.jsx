import { useState } from 'react'
import { Alert, Card, Checkbox, Col, Row, Select, Space, Spin, Tag, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { api } from '../api/client'
import { CAUSE_STATUS, LAYER_DEFINITIONS, MAX_YEAR, MIN_YEAR, YEARS } from '../constants'
import MapView from '../components/MapView'

const { Title, Paragraph, Text } = Typography

export default function MapPage() {
  const { data: areas = [] } = useQuery({ queryKey: ['areas'], queryFn: api.listAreas })
  const [selection, setSelection] = useState({ aoiId: 'RU_VOLOGDA_02', startYear: MIN_YEAR, endYear: MAX_YEAR })
  const [visibleLayers, setVisibleLayers] = useState({
    aoi: true,
    zones: true,
    gfc: false,
    modis: false,
    coverage: true
  })

  const summary = useQuery({
    queryKey: ['map', selection],
    queryFn: () => api.createAnalysis(selection),
    enabled: Boolean(selection.aoiId)
  })

  const data = summary.data

  return (
    <Space direction="vertical" size="large" style={{ width: '100%' }}>
      <div>
        <Title level={2} style={{ marginBottom: 4 }}>
          Карта
        </Title>
        <Paragraph type="secondary" style={{ marginBottom: 0 }}>
          Слои включаются независимо: граница AOI, зоны изменений, пригодное покрытие. GFC, MODIS и
          AGB-растры подключаются из Go API.
        </Paragraph>
      </div>

      <Card size="small">
        <Space wrap size="middle">
          <span>Территория:</span>
          <Select
            style={{ width: 260 }}
            value={selection.aoiId}
            onChange={(value) => setSelection((prev) => ({ ...prev, aoiId: value }))}
            options={areas.map((area) => ({ value: area.aoi_id, label: area.aoi_id }))}
          />
          <span>Годы:</span>
          <Select
            style={{ width: 100 }}
            value={selection.startYear}
            onChange={(value) => setSelection((prev) => ({ ...prev, startYear: value }))}
            options={YEARS.map((year) => ({ value: year, label: year }))}
          />
          <Select
            style={{ width: 100 }}
            value={selection.endYear}
            onChange={(value) => setSelection((prev) => ({ ...prev, endYear: value }))}
            options={YEARS.map((year) => ({ value: year, label: year }))}
          />
        </Space>
      </Card>

      <Card
        size="small"
        title="Слои"
        extra={
          <Space wrap>
            {Object.entries(CAUSE_STATUS).slice(0, 3).map(([key, meta]) => (
              <Tag key={key} color={meta.color}>
                {meta.label}
              </Tag>
            ))}
          </Space>
        }
      >
        <Checkbox.Group
          style={{ marginBottom: 12 }}
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

        {summary.isLoading ? (
          <Space align="center" style={{ width: '100%', justifyContent: 'center', padding: 48 }}>
            <Spin />
            <Text type="secondary">Загрузка слоёв…</Text>
          </Space>
        ) : (
          <Row gutter={16}>
            <Col xs={24} lg={17}>
              <MapView
                geometry={data?.geometry}
                zones={data?.changeZones ?? []}
                visibleLayers={visibleLayers}
                height={540}
              />
            </Col>
            <Col xs={24} lg={7}>
              <Space direction="vertical" style={{ width: '100%' }}>
                <Alert
                  type="info"
                  showIcon
                  message="Клик по зоне"
                  description="Показывает площадь, вклад в ΔC и статус причины."
                />
                {(data?.changeZones ?? []).map((zone) => (
                  <Card key={zone.id} size="small">
                    <Space>
                      <Text strong>Зона {zone.id}</Text>
                      <Tag color={CAUSE_STATUS[zone.causeStatus]?.color}>
                        {CAUSE_STATUS[zone.causeStatus]?.label}
                      </Tag>
                    </Space>
                    <div>
                      <Text type="secondary">Площадь: {zone.areaHectares} га</Text>
                    </div>
                    <div>
                      <Text type="secondary">
                        Подтверждения: {zone.evidenceTypes.join(', ') || '—'}
                      </Text>
                    </div>
                  </Card>
                ))}
              </Space>
            </Col>
          </Row>
        )}
      </Card>
    </Space>
  )
}
