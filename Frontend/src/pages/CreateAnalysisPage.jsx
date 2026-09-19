import { useMemo, useState } from 'react'
import {
  Alert,
  Button,
  Card,
  Col,
  Form,
  Input,
  InputNumber,
  Row,
  Select,
  Space,
  Tag,
  Typography,
  Upload
} from 'antd'
import { useMutation } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { useAreas } from '../hooks/queries'
import { saveRunContext } from '../utils/runContext'
import { parseGeoJson, validateGeometry } from '../utils/geometry'
import { MAX_YEAR, METHOD_PROFILES, MIN_YEAR, YEARS, formatNumber } from '../constants'

const { Title, Paragraph, Text } = Typography

export default function CreateAnalysisPage() {
  const navigate = useNavigate()
  const [form] = Form.useForm()
  const [geoJson, setGeoJson] = useState('')

  const { data: areas = [], isLoading: areasLoading, error: areasError } = useAreas()
  const aoiId = Form.useWatch('aoiId', form)

  const hasPolygon = geoJson.trim().length > 0
  const hasAoi = Boolean(aoiId)

  const geometryState = useMemo(() => {
    if (!hasPolygon) return { kind: 'empty', errors: [], areaHa: null }
    const parsed = parseGeoJson(geoJson)
    if (parsed.error) return { kind: 'invalid', errors: [parsed.error], areaHa: null }
    const result = validateGeometry(parsed.geometry)
    if (!result.ok) return { kind: 'invalid', errors: result.errors, areaHa: result.areaHa }
    return { kind: 'valid', errors: [], areaHa: result.areaHa }
  }, [geoJson, hasPolygon])

  const createMutation = useMutation({
    mutationFn: (payload) => api.createAnalysis(payload),
    onSuccess: (run, variables) => {
      saveRunContext(run.id, variables)
      navigate(`/analysis?run=${run.id}`)
    }
  })

  const polygonValid = geometryState.kind === 'valid'
  const sourceSelected = hasAoi || hasPolygon
  const canSubmit = sourceSelected && (!hasPolygon || polygonValid) && !createMutation.isPending

  const readFile = (file) => {
    const reader = new FileReader()
    reader.onload = () => {
      setGeoJson(String(reader.result ?? ''))
      form.setFieldValue('aoiId', undefined)
    }
    reader.readAsText(file)
    return false
  }

  const onFinish = (values) => {
    if (!canSubmit) return
    createMutation.mutate({
      aoiId: values.aoiId || undefined,
      polygonGeoJson: hasPolygon ? geoJson.trim() : undefined,
      startYear: values.startYear,
      endYear: values.endYear,
      methodProfile: values.methodProfile,
      name: values.name,
      claimedResult: values.claimedResult ?? null,
      description: values.description?.trim() ? values.description.trim() : null
    })
  }

  const modeTag = hasAoi
    ? { color: 'blue', label: 'Режим: территория из каталога' }
    : hasPolygon
      ? { color: 'green', label: 'Режим: пользовательский полигон' }
      : { color: 'default', label: 'Режим: источник не выбран' }

  return (
    <Space direction="vertical" size="large" style={{ width: '100%' }}>
      <div>
        <Title level={2} style={{ marginBottom: 4 }}>
          Новая проверка
        </Title>
        <Paragraph type="secondary" style={{ marginBottom: 0 }}>
          Выберите территорию из каталога или задайте собственный полигон GeoJSON. Создаётся задание
          расчёта — результат появится на отдельной странице анализа.
        </Paragraph>
      </div>

      {areasError && (
        <Alert
          type="error"
          showIcon
          message="Не удалось загрузить каталог территорий"
          description={areasError.message}
        />
      )}

      <Card>
        <Form
          form={form}
          layout="vertical"
          initialValues={{
            startYear: MIN_YEAR,
            endYear: MAX_YEAR,
            methodProfile: METHOD_PROFILES[0]?.value
          }}
          onFinish={onFinish}
          onValuesChange={(changed) => {
            if ('aoiId' in changed && changed.aoiId) setGeoJson('')
          }}
        >
          <Row gutter={16}>
            <Col xs={24} md={12}>
              <Form.Item label="Территория (AOI)" name="aoiId">
                <Select
                  allowClear
                  showSearch
                  loading={areasLoading}
                  disabled={hasPolygon}
                  optionFilterProp="label"
                  placeholder={hasPolygon ? 'Отключено: задан свой полигон' : 'Выберите участок'}
                  options={areas.map((area) => ({
                    value: area.aoiId,
                    label: `${area.aoiId} — ${formatNumber(area.areaHa)} га`
                  }))}
                />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item label="Пользовательский полигон (GeoJSON)">
                <Space direction="vertical" size="small" style={{ width: '100%' }}>
                  <Input.TextArea
                    rows={6}
                    value={geoJson}
                    disabled={hasAoi}
                    onChange={(event) => {
                      const value = event.target.value
                      setGeoJson(value)
                      if (value.trim()) form.setFieldValue('aoiId', undefined)
                    }}
                    placeholder='{"type":"Polygon","coordinates":[[[lon,lat],...]]}'
                  />
                  <Upload
                    accept=".geojson,.json,application/geo+json"
                    showUploadList={false}
                    beforeUpload={readFile}
                    disabled={hasAoi}
                  >
                    <Button disabled={hasAoi}>Загрузить GeoJSON</Button>
                  </Upload>
                </Space>
              </Form.Item>
            </Col>
          </Row>

          <Tag color={modeTag.color} style={{ marginBottom: 16 }}>
            {modeTag.label}
          </Tag>

          {hasPolygon && geometryState.kind === 'invalid' && (
            <Alert
              type="error"
              showIcon
              style={{ marginBottom: 16 }}
              message="Полигон не прошёл проверку"
              description={
                <ul style={{ margin: 0, paddingLeft: 18 }}>
                  {geometryState.errors.map((error) => (
                    <li key={error}>{error}</li>
                  ))}
                </ul>
              }
            />
          )}

          {hasPolygon && geometryState.kind === 'valid' && (
            <Alert
              type="success"
              showIcon
              style={{ marginBottom: 16 }}
              message={`Полигон корректен. Площадь: ${formatNumber(geometryState.areaHa)} га`}
              description="Клиентская площадь оценочная; backend считает итоговую площадь по фактическому пересечению пикселей растров."
            />
          )}

          <Row gutter={16}>
            <Col xs={24} md={16}>
              <Form.Item
                label="Название проекта"
                name="name"
                rules={[{ required: true, message: 'Укажите название проекта' }]}
              >
                <Input placeholder="Например, «Участок Север-3, 2024»" maxLength={200} />
              </Form.Item>
            </Col>
            <Col xs={24} md={8}>
              <Form.Item label="Заявленный результат, т CO₂-экв." name="claimedResult">
                <InputNumber
                  min={0}
                  style={{ width: '100%' }}
                  placeholder="Необязательно"
                  addonAfter="т CO₂-экв."
                />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item label="Описание" name="description">
            <Input.TextArea rows={3} placeholder="Необязательно" maxLength={2000} />
          </Form.Item>

          <Row gutter={16}>
            <Col xs={12} md={8}>
              <Form.Item
                label="Начальный год"
                name="startYear"
                dependencies={['endYear']}
                rules={[
                  { required: true, message: 'Укажите начальный год' },
                  ({ getFieldValue }) => ({
                    validator(_, value) {
                      const endYear = getFieldValue('endYear')
                      if (!value || !endYear || value < endYear) return Promise.resolve()
                      return Promise.reject(
                        new Error('Начальный год должен быть меньше конечного')
                      )
                    }
                  })
                ]}
              >
                <Select options={YEARS.map((year) => ({ value: year, label: year }))} />
              </Form.Item>
            </Col>
            <Col xs={12} md={8}>
              <Form.Item
                label="Конечный год"
                name="endYear"
                dependencies={['startYear']}
                rules={[
                  { required: true, message: 'Укажите конечный год' },
                  ({ getFieldValue }) => ({
                    validator(_, value) {
                      const startYear = getFieldValue('startYear')
                      if (!value || !startYear || value > startYear) return Promise.resolve()
                      return Promise.reject(
                        new Error('Конечный год должен быть больше начального')
                      )
                    }
                  })
                ]}
              >
                <Select options={YEARS.map((year) => ({ value: year, label: year }))} />
              </Form.Item>
            </Col>
            <Col xs={24} md={8}>
              <Form.Item
                label="Профиль методики"
                name="methodProfile"
                rules={[{ required: true, message: 'Выберите профиль методики' }]}
              >
                <Select options={METHOD_PROFILES} />
              </Form.Item>
            </Col>
          </Row>

          <Space direction="vertical" size="middle" style={{ width: '100%' }}>
            <Alert
              type="info"
              showIcon
              message="Что проверяется до запуска"
              description={
                <ul style={{ margin: 0, paddingLeft: 18 }}>
                  <li>координаты в WGS 84 (долгота −180…180, широта −90…90);</li>
                  <li>полигон без самопересечений, кольца замкнуты;</li>
                  <li>площадь не более 20 км²;</li>
                  <li>полигон должен попадать в зону покрытия данных;</li>
                  <li>начальный год строго меньше конечного;</li>
                  <li>
                    годы в допустимом диапазоне {MIN_YEAR}–{MAX_YEAR}.
                  </li>
                </ul>
              }
            />

            <Alert
              type="warning"
              showIcon
              message="Площадь расчёта"
              description="Итоговая площадь считается на backend по фактическому пересечению пикселей растров, а не по площади контура полигона."
            />

            {!sourceSelected && (
              <Text type="secondary">
                Выберите территорию из каталога или задайте пользовательский полигон.
              </Text>
            )}

            {createMutation.isError && (
              <Alert
                type="error"
                showIcon
                message="Не удалось создать задание"
                description={createMutation.error?.message ?? 'Повторите попытку позже.'}
              />
            )}

            <Button
              type="primary"
              htmlType="submit"
              size="large"
              loading={createMutation.isPending}
              disabled={!canSubmit}
            >
              Запустить расчёт
            </Button>
          </Space>
        </Form>
      </Card>
    </Space>
  )
}
