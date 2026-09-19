import { useState } from 'react'
import { Alert, Button, Card, Col, Descriptions, Row, Select, Space, Table, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { api } from '../api/client'
import { useAreas, useSensitivity } from '../hooks/queries'
import {
  MIN_YEAR,
  MAX_YEAR,
  YEARS,
  formatNumber,
  formatUnits,
  formatPercent,
  DISCLAIMERS
} from '../constants'

const { Title, Paragraph, Text } = Typography

const CHANGED_AOI = 'RU_VOLOGDA_02'
const CONTROL_AOI = 'RU_TVER_01'

const kRows = (data) => [
  {
    key: 'k1',
    variant: 'k = 1',
    lower: data.k1?.lower,
    upper: data.k1?.upper,
    halfWidth: data.k1?.halfWidth,
    eproj: data.k1?.eproj,
    coverage: data.k1?.coverage,
    zoneArea: data.k1?.zoneAreaHectares,
    units: data.k1?.units
  },
  {
    key: 'k2',
    variant: 'k = 2',
    lower: data.k2?.lower,
    upper: data.k2?.upper,
    halfWidth: data.k2?.halfWidth,
    eproj: data.k2?.eproj,
    coverage: data.k2?.coverage,
    zoneArea: data.k2?.zoneAreaHectares,
    units: data.k2?.units
  }
]

const maskRows = (mask, validPixels) => [
  { key: 'units', label: 'Q, единицы', value: formatUnits(mask?.units) },
  { key: 'eproj', label: 'Eproj, т CO₂-экв.', value: formatNumber(mask?.eproj) },
  { key: 'coverage', label: 'Покрытие', value: formatPercent(mask?.coverage) },
  {
    key: 'zoneArea',
    label: 'Площадь зон, га',
    value: formatNumber(mask?.zoneAreaHectares)
  },
  {
    key: 'validPixels',
    label: 'Валидные пиксели',
    value: formatUnits(validPixels)
  }
]

export default function ExperimentPage() {
  const { data: areas = [], isLoading: areasLoading, isError: areasError } = useAreas()
  const [selection, setSelection] = useState({
    aoiId: null,
    startYear: MIN_YEAR,
    endYear: MAX_YEAR
  })
  const [applied, setApplied] = useState({
    aoiId: null,
    startYear: MIN_YEAR,
    endYear: MAX_YEAR
  })
  const [formError, setFormError] = useState(null)

  const defaultAoiId =
    areas.find((area) => area.aoiId === CHANGED_AOI)?.aoiId ?? areas[0]?.aoiId ?? null
  const appliedAoiId = applied.aoiId ?? defaultAoiId

  const sensitivity = useSensitivity({
    aoiId: appliedAoiId,
    startYear: applied.startYear,
    endYear: applied.endYear
  })

  const compare = useQuery({
    queryKey: ['experiment-compare', MIN_YEAR, MAX_YEAR],
    queryFn: async () => {
      const [changed, control] = await Promise.all([
        api.getSensitivity({ aoiId: CHANGED_AOI, startYear: MIN_YEAR, endYear: MAX_YEAR }),
        api.getSensitivity({ aoiId: CONTROL_AOI, startYear: MIN_YEAR, endYear: MAX_YEAR })
      ])
      return { changed, control }
    }
  })

  const runCompare = () => {
    const aoiId = selection.aoiId ?? defaultAoiId
    if (!aoiId) {
      setFormError('Выберите территорию для сравнения.')
      return
    }
    if (selection.endYear <= selection.startYear) {
      setFormError('Конечный год должен быть больше начального.')
      return
    }
    setFormError(null)
    setApplied({
      aoiId,
      startYear: selection.startYear,
      endYear: selection.endYear
    })
  }

  const data = sensitivity.data

  const compareRows = compare.data
    ? [
        {
          key: 'changed',
          aoi: `${CHANGED_AOI} (изменение)`,
          h1: compare.data.changed.k1?.halfWidth,
          h2: compare.data.changed.k2?.halfWidth,
          units1: compare.data.changed.k1?.units,
          units2: compare.data.changed.k2?.units,
          eproj: compare.data.changed.k1?.eproj,
          coverage: compare.data.changed.k1?.coverage
        },
        {
          key: 'control',
          aoi: `${CONTROL_AOI} (контроль)`,
          h1: compare.data.control.k1?.halfWidth,
          h2: compare.data.control.k2?.halfWidth,
          units1: compare.data.control.k1?.units,
          units2: compare.data.control.k2?.units,
          eproj: compare.data.control.k1?.eproj,
          coverage: compare.data.control.k1?.coverage
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
          Проверка устойчивости вывода: SCL 4–5 против 4–7, сценарный диапазон k = 1 против k = 2.
          Изменение настроек не подменяет основной метод и не превращает естественную динамику в
          нарушение.
        </Paragraph>
      </div>

      <Alert
        type="info"
        showIcon
        message="Сравнение вариантов не является подтверждением нарушения"
        description={`${DISCLAIMERS.validation} ${DISCLAIMERS.uncertainty}`}
      />

      <Card title="Параметры сравнения" size="small">
        {areasError && (
          <Alert
            type="error"
            showIcon
            style={{ marginBottom: 16 }}
            message="Не удалось загрузить список территорий"
          />
        )}
        <Space wrap size="middle">
          <Space direction="vertical" size={4}>
            <Text type="secondary">Территория (AOI)</Text>
            <Select
              style={{ width: 260 }}
              loading={areasLoading}
              placeholder="Выберите участок"
              value={selection.aoiId ?? defaultAoiId ?? undefined}
              onChange={(value) => {
                setFormError(null)
                setSelection((prev) => ({ ...prev, aoiId: value }))
              }}
              options={areas.map((area) => ({
                value: area.aoiId,
                label: `${area.aoiId} — ${formatNumber(area.areaHa)} га`
              }))}
            />
          </Space>
          <Space direction="vertical" size={4}>
            <Text type="secondary">Начальный год</Text>
            <Select
              style={{ width: 110 }}
              value={selection.startYear}
              onChange={(value) => {
                setFormError(null)
                setSelection((prev) => ({ ...prev, startYear: value }))
              }}
              options={YEARS.map((year) => ({ value: year, label: year }))}
            />
          </Space>
          <Space direction="vertical" size={4}>
            <Text type="secondary">Конечный год</Text>
            <Select
              style={{ width: 110 }}
              value={selection.endYear}
              onChange={(value) => {
                setFormError(null)
                setSelection((prev) => ({ ...prev, endYear: value }))
              }}
              options={YEARS.map((year) => ({ value: year, label: year }))}
            />
          </Space>
          <Space direction="vertical" size={4}>
            <Text type="secondary">&nbsp;</Text>
            <Button type="primary" loading={sensitivity.isFetching} onClick={runCompare}>
              Сравнить варианты
            </Button>
          </Space>
        </Space>

        {formError && (
          <Alert
            type="warning"
            showIcon
            style={{ marginTop: 16 }}
            message={formError}
          />
        )}
      </Card>

      <Card
        title="Чувствительность k"
        size="small"
        loading={sensitivity.isLoading}
      >
        {sensitivity.isError && (
          <Alert
            type="error"
            showIcon
            message="Ошибка запроса чувствительности"
            description={sensitivity.error?.message}
          />
        )}

        {data && (
          <Space direction="vertical" size="middle" style={{ width: '100%' }}>
            <Table
              size="small"
              rowKey="key"
              pagination={false}
              dataSource={kRows(data)}
              columns={[
                { title: 'Вариант', dataIndex: 'variant', width: 100 },
                {
                  title: 'L, т CO₂-экв.',
                  dataIndex: 'lower',
                  render: (value) => formatNumber(value)
                },
                {
                  title: 'U, т CO₂-экв.',
                  dataIndex: 'upper',
                  render: (value) => formatNumber(value)
                },
                {
                  title: 'H, т CO₂-экв.',
                  dataIndex: 'halfWidth',
                  render: (value) => formatNumber(value)
                },
                {
                  title: 'Eproj, т CO₂-экв.',
                  dataIndex: 'eproj',
                  render: (value) => formatNumber(value)
                },
                {
                  title: 'Покрытие, %',
                  dataIndex: 'coverage',
                  render: (value) => formatPercent(value)
                },
                {
                  title: 'Площадь зон, га',
                  dataIndex: 'zoneArea',
                  render: (value) => formatNumber(value)
                },
                { title: 'Q', dataIndex: 'units', render: (value) => formatUnits(value) }
              ]}
            />
            <Text type="secondary" style={{ fontSize: 12 }}>
              H — сценарная полуширина диапазона L–U, а не статистическая погрешность.
            </Text>
          </Space>
        )}
      </Card>

      <Card title="Маска SCL" size="small" loading={sensitivity.isLoading}>
        {data && (
          <Space direction="vertical" size="middle" style={{ width: '100%' }}>
            <Row gutter={[16, 16]}>
              <Col xs={24} md={12}>
                <Card size="small" type="inner" title="SCL 4–5 (строгая)">
                  <Descriptions
                    size="small"
                    column={1}
                    items={maskRows(data.sclStrict, data.sclStrictValidPixels)}
                  />
                </Card>
              </Col>
              <Col xs={24} md={12}>
                <Card size="small" type="inner" title="SCL 4–7 (расширенная)">
                  <Descriptions
                    size="small"
                    column={1}
                    items={maskRows(data.sclExtended, data.sclExtendedValidPixels)}
                  />
                </Card>
              </Col>
            </Row>
            <Paragraph type="secondary" style={{ marginBottom: 0 }}>
              Расширение маски SCL меняет число валидных пикселей. Разница в валидных пикселях
              показывает, насколько вывод чувствителен к выбору маски, и не является наземным
              подтверждением результата.
            </Paragraph>
          </Space>
        )}
      </Card>

      <Card
        title="CCI Change против собственной разности"
        size="small"
        loading={sensitivity.isLoading}
      >
        {data && (
          <Space direction="vertical" size="middle" style={{ width: '100%' }}>
            <Row gutter={[16, 16]}>
              <Col xs={24} md={12}>
                <Card size="small" type="inner" title="CCI Change">
                  <Text strong style={{ fontSize: 20 }}>
                    {formatNumber(data.cciChangeMean, 2)}
                  </Text>{' '}
                  <Text type="secondary">т/га</Text>
                </Card>
              </Col>
              <Col xs={24} md={12}>
                <Card size="small" type="inner" title="Собственная разность (год к году)">
                  <Text strong style={{ fontSize: 20 }}>
                    {formatNumber(data.selfDifferenceMean, 2)}
                  </Text>{' '}
                  <Text type="secondary">т/га</Text>
                </Card>
              </Col>
            </Row>
            <Paragraph type="secondary" style={{ marginBottom: 0 }}>
              Здесь проверяется, согласуется ли независимый продукт CCI Change с собственной
              межгодовой разностью для периода 2019–2020. Совпадение направлений поддерживает
              вывод, но сравнение спутниковых продуктов не является наземной валидацией.
            </Paragraph>
          </Space>
        )}
      </Card>

      <Card
        title="Контрольный и изменившийся участки"
        size="small"
        loading={compare.isLoading}
      >
        {compare.isError && (
          <Alert
            type="error"
            showIcon
            message="Ошибка запроса сравнения участков"
            description={compare.error?.message}
          />
        )}

        {compare.data && (
          <Space direction="vertical" size="middle" style={{ width: '100%' }}>
            <Table
              size="small"
              rowKey="key"
              pagination={false}
              scroll={{ x: true }}
              dataSource={compareRows}
              columns={[
                { title: 'Участок', dataIndex: 'aoi' },
                { title: 'H (k=1)', dataIndex: 'h1', render: (value) => formatNumber(value) },
                { title: 'H (k=2)', dataIndex: 'h2', render: (value) => formatNumber(value) },
                { title: 'Q (k=1)', dataIndex: 'units1', render: (value) => formatUnits(value) },
                { title: 'Q (k=2)', dataIndex: 'units2', render: (value) => formatUnits(value) },
                {
                  title: 'Eproj (k=1)',
                  dataIndex: 'eproj',
                  render: (value) => formatNumber(value)
                },
                {
                  title: 'Покрытие (k=1)',
                  dataIndex: 'coverage',
                  render: (value) => formatPercent(value)
                }
              ]}
            />
            <Paragraph type="secondary" style={{ marginBottom: 0 }}>
              Контрольный участок не должен показывать уверенное нарушение без подтверждений:
              близкие к нулю H и Q при отсутствии признаков потери — ожидаемая картина. Расхождение
              H между k = 1 и k = 2 говорит о чувствительности вывода к выбору множителя.
            </Paragraph>
          </Space>
        )}
      </Card>

      <Paragraph type="secondary" style={{ fontSize: 12, marginBottom: 0 }}>
        {DISCLAIMERS.emission} {DISCLAIMERS.pool}
      </Paragraph>
    </Space>
  )
}
