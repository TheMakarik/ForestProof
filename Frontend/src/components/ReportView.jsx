import { Alert, Card, Col, Descriptions, Row, Table, Tag, Typography } from 'antd'
import {
  BLOCK_REASON_LABELS,
  CAUSE_STATUS,
  DISCLAIMERS,
  EVIDENCE_LABELS,
  formatDate,
  formatMoney,
  formatNumber,
  formatPercent,
  formatUnits
} from '../constants'
import StatusTag from './StatusTag'

const { Title, Paragraph, Text } = Typography

const num = (value, digits = 2) => formatNumber(value, digits)

export default function ReportView({
  summary,
  zones = [],
  sources = [],
  request = null,
  status = null
}) {
  if (!summary) return null

  const first = summary.yearlySeries?.[0] ?? {}
  const last = summary.yearlySeries?.[summary.yearlySeries.length - 1] ?? {}
  const change = summary.change ?? {}
  const uncertainty = summary.uncertainty ?? {}
  const baseline = summary.baseline ?? {}
  const unitsInfo = summary.units ?? {}
  const runStatus = status?.status ?? summary.status

  const qValue = unitsInfo.units
  const qUnavailable = qValue === null || qValue === undefined
  const qZero = qValue === 0

  const zoneList =
    Array.isArray(zones) && zones.length > 0
      ? zones
      : (summary.changeZones ?? []).map((zone) => {
          const evidence = (summary.changeZoneEvidence ?? []).find(
            (item) => item.zoneId === zone.id
          )
          return {
            ...zone,
            evidenceTypes: evidence?.evidenceTypes ?? [],
            causeStatus: evidence?.causeStatus ?? 'undetermined'
          }
        })

  const sourceFiles = sources.flatMap((source) =>
    (source.files ?? []).map((file) => ({ ...file, sourceId: source.sourceId }))
  )

  const methodProfile = request?.methodProfile ?? 'default'
  const aoiLabel = summary.aoiId ? summary.aoiId : 'Произвольный контур'

  const resultItems = [
    ['C_t0', num(first.totalCarbon), 'т C'],
    ['C_t1', num(last.totalCarbon), 'т C'],
    ['ΔC', num(change.deltaCarbon), 'т C'],
    ['Eproj', num(change.projectEmission), 'т CO₂-экв.'],
    ['e', num(change.emissionPerHectarePerYear, 3), 'т CO₂-экв./га/год'],
    ['L', num(uncertainty.lower), 'т CO₂-экв.'],
    ['U', num(uncertainty.upper), 'т CO₂-экв.'],
    ['H', num(uncertainty.halfWidth), 'т CO₂-экв.'],
    ['Покрытие t0', formatPercent(first.coverage), ''],
    ['Покрытие t1', formatPercent(last.coverage), ''],
    ['Ebase', num(baseline.baselineEmission), 'т CO₂-экв.'],
    ['R', num(unitsInfo.resultRelativeToBaseline), 'т CO₂-экв.'],
    ['UNC', num(unitsInfo.uncertaintyDeduction, 4), ''],
    ['Radj', num(unitsInfo.adjustedResult), 'т CO₂-экв.'],
    ['B (резерв 15%)', num(unitsInfo.reserve), 'т CO₂-экв.']
  ]

  return (
    <Row gutter={[16, 16]}>
      <Col span={24}>
        <Alert
          type="warning"
          showIcon
          message="Потенциальные, не сертифицированные единицы"
          description={DISCLAIMERS.units}
        />
      </Col>

      <Col span={24}>
        <Card size="small" title="Территория и период">
          <Descriptions size="small" column={{ xs: 1, sm: 2, md: 3 }}>
            <Descriptions.Item label="Территория">{aoiLabel}</Descriptions.Item>
            <Descriptions.Item label="Период">
              {summary.startYear}–{summary.endYear}
            </Descriptions.Item>
            <Descriptions.Item label="Статус">
              <StatusTag type="run" value={runStatus} />
            </Descriptions.Item>
            <Descriptions.Item label="Площадь полигона">
              {num(summary.polygonAreaHectares)} га
            </Descriptions.Item>
            <Descriptions.Item label="run_id">{summary.runId ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Дата расчёта">
              {formatDate(summary.createdAt)}
            </Descriptions.Item>
            <Descriptions.Item label="method_version">
              {summary.methodVersion ?? '—'}
            </Descriptions.Item>
            <Descriptions.Item label="data_version">
              {summary.dataVersion ?? '—'}
            </Descriptions.Item>
            <Descriptions.Item label="input_hash">
              <Text code style={{ fontSize: 11 }}>
                {summary.inputHash ?? '—'}
              </Text>
            </Descriptions.Item>
          </Descriptions>
        </Card>
      </Col>

      <Col span={24}>
        <Card size="small" title="Итоговая таблица">
          <Descriptions size="small" bordered column={{ xs: 1, sm: 2, md: 3 }}>
            {resultItems.map(([label, value, unit]) => (
              <Descriptions.Item key={label} label={label}>
                {value}
                {unit ? ` ${unit}` : ''}
              </Descriptions.Item>
            ))}
            <Descriptions.Item label="Q (потенциальные единицы)">
              {qUnavailable ? (
                <Text type="danger">недоступно</Text>
              ) : (
                <Text strong={qZero === false}>{formatUnits(qValue)}</Text>
              )}
            </Descriptions.Item>
          </Descriptions>

          {qZero && (
            <Alert
              type="warning"
              showIcon
              style={{ marginTop: 12 }}
              message="Q = 0, расчёт допустим"
              description="Условия выполнены, но положительного результата относительно baseline нет."
            />
          )}
          {qUnavailable && (
            <Alert
              type="error"
              showIcon
              style={{ marginTop: 12 }}
              message="Q недоступен"
              description={BLOCK_REASON_LABELS[unitsInfo.reason] ?? unitsInfo.reason ?? '—'}
            />
          )}
        </Card>
      </Col>

      <Col xs={24} lg={12}>
        <Card size="small" title="Ценовые сценарии">
          <Table
            size="small"
            rowKey="pricePerUnit"
            pagination={false}
            dataSource={unitsInfo.priceScenarios ?? []}
            locale={{ emptyText: 'Ценовые сценарии не заданы.' }}
            columns={[
              {
                title: 'Цена, руб./ед.',
                dataIndex: 'pricePerUnit',
                render: (value) => formatUnits(value)
              },
              {
                title: 'Сценарная стоимость',
                dataIndex: 'value',
                render: (value) => formatMoney(value)
              }
            ]}
          />
        </Card>
      </Col>

      <Col xs={24} lg={12}>
        <Card size="small" title="Метод и параметры">
          <Descriptions size="small" column={1}>
            <Descriptions.Item label="method_version">
              {summary.methodVersion ?? '—'}
            </Descriptions.Item>
            <Descriptions.Item label="data_version">
              {summary.dataVersion ?? '—'}
            </Descriptions.Item>
            <Descriptions.Item label="Профиль метода">{methodProfile}</Descriptions.Item>
            <Descriptions.Item label="Начальный год">{summary.startYear}</Descriptions.Item>
            <Descriptions.Item label="Конечный год">{summary.endYear}</Descriptions.Item>
          </Descriptions>
        </Card>
      </Col>

      <Col span={24}>
        <Card size="small" title="Годовая динамика">
          <Table
            size="small"
            rowKey="year"
            pagination={false}
            dataSource={summary.yearlySeries ?? []}
            locale={{ emptyText: 'Нет данных годовой динамики.' }}
            columns={[
              { title: 'Год', dataIndex: 'year', width: 80 },
              {
                title: 'Площадь, га',
                dataIndex: 'areaHectares',
                render: (value) => num(value)
              },
              {
                title: 'C, т C',
                dataIndex: 'totalCarbon',
                render: (value) => num(value)
              },
              {
                title: 'c̄, т C/га',
                dataIndex: 'meanCarbonPerHectare',
                render: (value) => num(value, 3)
              },
              {
                title: 'Покрытие',
                dataIndex: 'coverage',
                render: (value) => formatPercent(value)
              }
            ]}
          />
        </Card>
      </Col>

      <Col span={24}>
        <Card size="small" title="Зоны изменений">
          <Table
            size="small"
            rowKey="id"
            pagination={false}
            dataSource={zoneList}
            locale={{ emptyText: 'Зоны изменений не выявлены.' }}
            columns={[
              { title: 'Id', dataIndex: 'id', width: 60 },
              {
                title: 'Площадь, га',
                dataIndex: 'areaHectares',
                render: (value) => num(value)
              },
              {
                title: 'Вклад в ΔC, т C',
                dataIndex: 'contributionToDeltaCarbon',
                render: (value) => (
                  <Text type={value < 0 ? 'danger' : 'success'}>{num(value)}</Text>
                )
              },
              {
                title: 'Подтверждения',
                dataIndex: 'evidenceTypes',
                render: (types) =>
                  (types ?? []).length === 0
                    ? '—'
                    : (types ?? []).map((type) => (
                        <Tag key={type} color="geekblue">
                          {EVIDENCE_LABELS[type] ?? type}
                        </Tag>
                      ))
              },
              {
                title: 'Статус причины',
                dataIndex: 'causeStatus',
                render: (value) => (
                  <StatusTag type="cause" value={value} tooltip={CAUSE_STATUS[value]?.label} />
                )
              },
              {
                title: 'Интерпретация',
                dataIndex: 'interpretation',
                render: (value) => value ?? '—'
              }
            ]}
          />
        </Card>
      </Col>

      <Col span={24}>
        <Card size="small" title="Источники">
          <Table
            size="small"
            rowKey="sourceId"
            pagination={false}
            dataSource={sources}
            locale={{ emptyText: 'Каталог источников пуст.' }}
            columns={[
              { title: 'source_id', dataIndex: 'sourceId', width: 180 },
              { title: 'Продукт', dataIndex: 'product' },
              { title: 'Версия', dataIndex: 'version', width: 160 },
              {
                title: 'Лицензия',
                dataIndex: 'licenseUrl',
                render: (value) =>
                  value ? (
                    <a href={value} target="_blank" rel="noreferrer">
                      Лицензия
                    </a>
                  ) : (
                    '—'
                  )
              }
            ]}
          />
        </Card>
      </Col>

      <Col span={24}>
        <Card size="small" title="Хэши">
          <Descriptions size="small" column={1} style={{ marginBottom: 12 }}>
            <Descriptions.Item label="input_hash">
              <Text code style={{ fontSize: 11 }}>
                {summary.inputHash ?? '—'}
              </Text>
            </Descriptions.Item>
          </Descriptions>
          <Table
            size="small"
            rowKey={(file) => `${file.sourceId}:${file.relativePath}`}
            pagination={false}
            dataSource={sourceFiles}
            locale={{ emptyText: 'Файлы источников не зарегистрированы.' }}
            columns={[
              { title: 'source_id', dataIndex: 'sourceId', width: 160 },
              { title: 'Файл', dataIndex: 'relativePath' },
              {
                title: 'sha256 (запись)',
                dataIndex: 'sha256Recorded',
                render: (value) => <Text code style={{ fontSize: 11 }}>{value ?? '—'}</Text>
              },
              {
                title: 'sha256 (факт)',
                dataIndex: 'sha256Actual',
                render: (value) => <Text code style={{ fontSize: 11 }}>{value ?? '—'}</Text>
              },
              { title: 'Проверка', dataIndex: 'verified', width: 110 }
            ]}
          />
        </Card>
      </Col>

      <Col xs={24} lg={12}>
        <Card size="small" title="Baseline">
          <Descriptions size="small" column={1}>
            <Descriptions.Item label="Ebase">
              {num(baseline.baselineEmission)} т CO₂-экв.
            </Descriptions.Item>
            <Descriptions.Item label="R">
              {num(unitsInfo.resultRelativeToBaseline)} т CO₂-экв.
            </Descriptions.Item>
            <Descriptions.Item label="Статус единиц">
              {BLOCK_REASON_LABELS[unitsInfo.reason] ?? unitsInfo.reason ?? '—'}
            </Descriptions.Item>
          </Descriptions>
        </Card>
      </Col>

      <Col xs={24} lg={12}>
        <Card size="small" title="Неопределённость">
          <Descriptions size="small" column={1}>
            <Descriptions.Item label="L">{num(uncertainty.lower)} т CO₂-экв.</Descriptions.Item>
            <Descriptions.Item label="U">{num(uncertainty.upper)} т CO₂-экв.</Descriptions.Item>
            <Descriptions.Item label="H">{num(uncertainty.halfWidth)} т CO₂-экв.</Descriptions.Item>
          </Descriptions>
          <Paragraph type="secondary" style={{ fontSize: 12, marginBottom: 0 }}>
            {DISCLAIMERS.uncertainty}
          </Paragraph>
        </Card>
      </Col>

      <Col span={24}>
        <Card size="small" title="Ограничения">
          {(summary.warnings ?? []).length > 0 && (
            <Alert
              type="info"
              showIcon
              style={{ marginBottom: 12 }}
              message="Предупреждения"
              description={
                <ul style={{ margin: 0, paddingLeft: 18 }}>
                  {(summary.warnings ?? []).map((warning) => (
                    <li key={warning}>{warning}</li>
                  ))}
                </ul>
              }
            />
          )}
          <ul style={{ margin: 0, paddingLeft: 18 }}>
            {Object.entries(DISCLAIMERS).map(([key, item]) => (
              <li key={key}>
                <Text type="secondary">{item}</Text>
              </li>
            ))}
          </ul>
        </Card>
      </Col>

      <Col span={24}>
        <Title level={5} style={{ margin: 0, color: '#8c6d1f' }}>
          Потенциальные, не сертифицированные единицы
        </Title>
        <Text type="secondary" style={{ fontSize: 12 }}>
          {DISCLAIMERS.units}
        </Text>
      </Col>
    </Row>
  )
}
