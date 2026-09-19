import { Alert, Button, Card, Col, Form, Input, Row, Select, Space, Typography } from 'antd'
import { InboxOutlined } from '@ant-design/icons'
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { useState } from 'react'
import { api } from '../api/client'
import { MAX_YEAR, MIN_YEAR, YEARS, formatNumber } from '../constants'

const { Title, Paragraph, Text } = Typography

export default function CreateAnalysisPage() {
  const navigate = useNavigate()
  const [form] = Form.useForm()
  const [geoJson, setGeoJson] = useState('')
  const { data: areas = [] } = useQuery({ queryKey: ['areas'], queryFn: api.listAreas })

  const onFinish = (values) => {
    const params = new URLSearchParams({
      start: String(values.startYear),
      end: String(values.endYear)
    })
    if (values.aoiId) params.set('aoiId', values.aoiId)
    if (geoJson) params.set('geometry', geoJson)
    navigate(`/analysis?${params.toString()}`)
  }

  const readFile = (file) => {
    const reader = new FileReader()
    reader.onload = () => setGeoJson(String(reader.result))
    reader.readAsText(file)
    return false
  }

  return (
    <Space direction="vertical" size="large" style={{ width: '100%' }}>
      <div>
        <Title level={2} style={{ marginBottom: 4 }}>
          Новая проверка
        </Title>
        <Paragraph type="secondary" style={{ marginBottom: 0 }}>
          Выберите участок из каталога или загрузите свой GeoJSON (WGS 84, площадь ≤ 20 км²).
        </Paragraph>
      </div>

      <Card>
        <Form
          form={form}
          layout="vertical"
          initialValues={{ aoiId: areas[0]?.aoi_id, startYear: MIN_YEAR, endYear: MAX_YEAR }}
          onFinish={onFinish}
        >
          <Row gutter={16}>
            <Col xs={24} md={10}>
              <Form.Item label="Территория (AOI)" name="aoiId">
                <Select
                  allowClear
                  placeholder="Выберите участок"
                  options={areas.map((area) => ({
                    value: area.aoi_id,
                    label: `${area.aoi_id} — ${formatNumber(area.area_ha)} га`
                  }))}
                />
              </Form.Item>
            </Col>
            <Col xs={12} md={7}>
              <Form.Item label="Начальный год" name="startYear" rules={[{ required: true }]}>
                <Select options={YEARS.map((year) => ({ value: year, label: year }))} />
              </Form.Item>
            </Col>
            <Col xs={12} md={7}>
              <Form.Item
                label="Конечный год"
                name="endYear"
                dependencies={['startYear']}
                rules={[
                  { required: true },
                  ({ getFieldValue }) => ({
                    validator(_, value) {
                      if (!value || value > getFieldValue('startYear')) return Promise.resolve()
                      return Promise.reject(new Error('Конечный год должен быть больше начального'))
                    }
                  })
                ]}
              >
                <Select options={YEARS.map((year) => ({ value: year, label: year }))} />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item label="Пользовательский полигон (GeoJSON, необязательно)">
            <Input.TextArea
              rows={5}
              value={geoJson}
              onChange={(event) => setGeoJson(event.target.value)}
              placeholder='{"type":"Polygon","coordinates":[...]}'
            />
          </Form.Item>

          <Form.Item>
            <Space>
              <Button type="primary" htmlType="submit">
                Запустить расчёт
              </Button>
              <UploadBeforeRead onRead={readFile} />
            </Space>
          </Form.Item>
        </Form>

        <Alert
          type="warning"
          showIcon
          message="Валидации"
          description={
            <ul style={{ margin: 0, paddingLeft: 18 }}>
              <li>геометрия WGS 84 без самопересечений;</li>
              <li>площадь не более 20 км² и внутри покрытия;</li>
              <li>
                начальный год меньше конечного, оба в диапазоне {MIN_YEAR}–{MAX_YEAR};
              </li>
              <li>расчёт по фактическому пересечению пикселей.</li>
            </ul>
          }
        />
      </Card>
    </Space>
  )
}

function UploadBeforeRead({ onRead }) {
  return (
    <label style={{ cursor: 'pointer' }}>
      <input
        type="file"
        accept=".geojson,.json,application/geo+json"
        style={{ display: 'none' }}
        onChange={(event) => {
          const file = event.target.files?.[0]
          if (file) onRead(file)
        }}
      />
      <span
        style={{
          display: 'inline-flex',
          alignItems: 'center',
          gap: 8,
          padding: '4px 15px',
          border: '1px solid #d9d9d9',
          borderRadius: 8,
          color: '#2f7d32'
        }}
      >
        <InboxOutlined /> Загрузить GeoJSON
      </span>
    </label>
  )
}
