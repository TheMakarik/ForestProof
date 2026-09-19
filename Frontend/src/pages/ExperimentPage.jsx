import { useState } from 'react'
import { Alert, Button, Card, Col, Form, Row, Select, Space, Table, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { api } from '../api/client'
import { MAX_YEAR, MIN_YEAR, YEARS, formatNumber, formatUnits } from '../constants'

const { Title, Paragraph, Text } = Typography

const CHANGED_AOI = 'RU_VOLOGDA_02'
const CONTROL_AOI = 'RU_TVER_01'

export default function ExperimentPage() {
  const { data: areas = [] } = useQuery({ queryKey: ['areas'], queryFn: api.listAreas })
  const [selection, setSelection] = useState({ aoiId: CHANGED_AOI, startYear: MIN_YEAR, endYear: MAX_YEAR })
  const [active, setActive] = useState(selection)

  const sensitivity = useQuery({
    queryKey: ['sensitivity', active],
    queryFn: () => api.getSensitivity(active),
    enabled: Boolean(active.aoiId)
  })

  const compare = useQuery({
    queryKey: ['experiment-compare'],
    queryFn: async () => {
      const changed = await api.getSensitivity({ aoiId: CHANGED_AOI, startYear: MIN_YEAR, endYear: MAX_YEAR })
      const control = await api.getSensitivity({ aoiId: CONTROL_AOI, startYear: MIN_YEAR, endYear: MAX_YEAR })
      return { changed, control }
    }
  })

  const rows = sensitivity.data
    ? [
        {
          key: 'k1',
          variant: 'k = 1',
          lower: sensitivity.data.k1.lower,
          upper: sensitivity.data.k1.upper,
          halfWidth: sensitivity.data.k1.halfWidth,
          units: sensitivity.data.k1.units
        },
        {
          key: 'k2',
          variant: 'k = 2',
          lower: sensitivity.data.k2.lower,
          upper: sensitivity.data.k2.upper,
          halfWidth: sensitivity.data.k2.halfWidth,
          units: sensitivity.data.k2.units
        }
      ]
    : []

  return (
    <Space direction="vertical" size="large" style={{ width: '100%' }}>
      <div>
        <Title level={2} style={{ marginBottom: 4 }}>
          Исследовательский режим
        </Title>
        <Paragraph type="secondary" style={{ marginBottom: 0 }}>
          Проверка устойчивости вывода: SCL 4–5 против 4–7, сценарный диапазон k=1 против k=2.
          Результаты не подменяют основной метод.
        </Paragraph>
      </div>

      <Alert
        type="info"
        showIcon
        message="Сравнение спутниковых продуктов не является наземной валидацией"
        description="Изменение настроек может менять число единиц, но не должно превращать естественную динамику в нарушение без подтверждений."
      />

      <Card title="Чувствительность выбранного AOI" size="small">
        <Form layout="inline" style={{ marginBottom: 16, rowGap: 8 }}>
          <Form.Item label="Территория">
            <Select
              style={{ width: 240 }}
              value={selection.aoiId}
              onChange={(value) => setSelection((prev) => ({ ...prev, aoiId: value }))}
              options={areas.map((area) => ({ value: area.aoi_id, label: area.aoi_id }))}
            />
          </Form.Item>
          <Form.Item label="С">
            <Select
              style={{ width: 100 }}
              value={selection.startYear}
              onChange={(value) => setSelection((prev) => ({ ...prev, startYear: value }))}
              options={YEARS.map((year) => ({ value: year, label: year }))}
            />
          </Form.Item>
          <Form.Item label="По">
            <Select
              style={{ width: 100 }}
              value={selection.endYear}
              onChange={(value) => setSelection((prev) => ({ ...prev, endYear: value }))}
              options={YEARS.map((year) => ({ value: year, label: year }))}
            />
          </Form.Item>
          <Form.Item>
            <Button type="primary" loading={sensitivity.isFetching} onClick={() => setActive(selection)}>
              Сравнить варианты
            </Button>
          </Form.Item>
        </Form>

        <Table
          size="small"
          rowKey="key"
          pagination={false}
          loading={sensitivity.isLoading}
          dataSource={rows}
          columns={[
            { title: 'Вариант', dataIndex: 'variant', width: 100 },
            { title: 'L, т CO₂-экв.', dataIndex: 'lower', render: (v) => formatNumber(v) },
            { title: 'U, т CO₂-экв.', dataIndex: 'upper', render: (v) => formatNumber(v) },
            { title: 'H, т CO₂-экв.', dataIndex: 'halfWidth', render: (v) => formatNumber(v) },
            { title: 'Q', dataIndex: 'units', render: (v) => formatUnits(v) }
          ]}
        />

        {sensitivity.data && (
          <Row gutter={16} style={{ marginTop: 16 }}>
            <Col xs={24} md={12}>
              <Card size="small" type="inner" title="SCL 4–5 (строгая маска)">
                <Text strong>{formatUnits(sensitivity.data.sclStrictValidPixels)}</Text> валидных
                пикселей
              </Card>
            </Col>
            <Col xs={24} md={12}>
              <Card size="small" type="inner" title="SCL 4–7 (расширенная маска)">
                <Text strong>{formatUnits(sensitivity.data.sclExtendedValidPixels)}</Text> валидных
                пикселей
              </Card>
            </Col>
          </Row>
        )}
      </Card>

      <Card title="Контрольный и изменившийся участки" size="small" loading={compare.isLoading}>
        {compare.data && (
          <Table
            size="small"
            rowKey="aoi"
            pagination={false}
            dataSource={[
              {
                key: 'changed',
                aoi: `${CHANGED_AOI} (изменение)`,
                h1: compare.data.changed.k1.halfWidth,
                h2: compare.data.changed.k2.halfWidth,
                units1: compare.data.changed.k1.units,
                units2: compare.data.changed.k2.units
              },
              {
                key: 'control',
                aoi: `${CONTROL_AOI} (контроль)`,
                h1: compare.data.control.k1.halfWidth,
                h2: compare.data.control.k2.halfWidth,
                units1: compare.data.control.k1.units,
                units2: compare.data.control.k2.units
              }
            ]}
            columns={[
              { title: 'Участок', dataIndex: 'aoi' },
              { title: 'H (k=1)', dataIndex: 'h1', render: (v) => formatNumber(v) },
              { title: 'H (k=2)', dataIndex: 'h2', render: (v) => formatNumber(v) },
              { title: 'Q (k=1)', dataIndex: 'units1', render: (v) => formatUnits(v) },
              { title: 'Q (k=2)', dataIndex: 'units2', render: (v) => formatUnits(v) }
            ]}
          />
        )}
      </Card>
    </Space>
  )
}
