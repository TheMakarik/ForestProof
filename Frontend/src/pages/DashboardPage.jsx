import { Alert, Button, Card, Col, Row, Skeleton, Space, Statistic, Table, Tag, Typography } from 'antd'
import { PlusOutlined } from '@ant-design/icons'
import { useNavigate } from 'react-router-dom'
import { useAreas, useProjects, useSources } from '../hooks/queries'
import {
  DISCLAIMERS,
  RUN_STATUS,
  formatDate,
  formatNumber,
  roleMeta
} from '../constants'
import { averageDurationMs, listRuns } from '../utils/runHistory'

const { Title, Paragraph, Text } = Typography

const averageDurationLabel = (averageMs) =>
  averageMs === null || averageMs === undefined ? '—' : `${formatNumber(averageMs / 1000, 1)} с`

export default function DashboardPage() {
  const navigate = useNavigate()
  const areasQuery = useAreas()
  const projectsQuery = useProjects()
  const sourcesQuery = useSources()

  const areas = areasQuery.data ?? []
  const projects = projectsQuery.data ?? []
  const sources = sourcesQuery.data ?? []
  const runs = listRuns()
  const averageMs = averageDurationMs(runs)

  const openAnalysis = (aoiId) => navigate(`/analysis?aoiId=${aoiId}&start=2019&end=2024`)

  const statsLoading = projectsQuery.isLoading || areasQuery.isLoading

  return (
    <Space direction="vertical" size="large" style={{ width: '100%' }}>
      <div>
        <Space align="center" wrap style={{ justifyContent: 'space-between', width: '100%' }}>
          <Title level={2} style={{ marginBottom: 4 }}>
            Дашборд
          </Title>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => navigate('/new')}>
            Новая проверка
          </Button>
        </Space>
        <Paragraph type="secondary" style={{ marginBottom: 0 }}>
          Спутниковая верификация «зелёных» инвестиций. Учитываемый пул — живая надземная древесная
          биомасса. Период 2019–2024.
        </Paragraph>
      </div>

      {projectsQuery.isError && (
        <Alert
          type="error"
          showIcon
          message="Не удалось загрузить проекты"
          description={projectsQuery.error?.message}
        />
      )}
      {areasQuery.isError && (
        <Alert
          type="error"
          showIcon
          message="Не удалось загрузить участки"
          description={areasQuery.error?.message}
        />
      )}
      <Row gutter={[16, 16]}>
        <Col xs={24} sm={12} lg={6}>
          <Card size="small" style={{ height: '100%' }}>
            {statsLoading ? (
              <Skeleton active paragraph={{ rows: 1 }} title={false} />
            ) : (
              <Statistic title="Проектов" value={projects.length} />
            )}
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card size="small" style={{ height: '100%' }}>
            <Statistic title="Проверок" value={runs.length} />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card size="small" style={{ height: '100%' }}>
            <Statistic title="Средняя длительность расчёта" value={averageDurationLabel(averageMs)} />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card size="small" style={{ height: '100%' }}>
            {statsLoading ? (
              <Skeleton active paragraph={{ rows: 1 }} title={false} />
            ) : (
              <Statistic title="Участков в каталоге" value={areas.length} />
            )}
          </Card>
        </Col>
      </Row>

      <Alert
        type="info"
        showIcon
        message="Потенциальные, не сертифицированные единицы"
        description={
          <>
            <div>{DISCLAIMERS.units}</div>
            <div>{DISCLAIMERS.pool}</div>
          </>
        }
      />

      <div>
        <Title level={4} style={{ marginBottom: 12 }}>
          Участки
        </Title>
        {areasQuery.isLoading ? (
          <Row gutter={[16, 16]}>
            {[0, 1, 2, 3].map((key) => (
              <Col key={key} xs={24} sm={12} lg={6}>
                <Card size="small">
                  <Skeleton active paragraph={{ rows: 3 }} />
                </Card>
              </Col>
            ))}
          </Row>
        ) : areas.length === 0 ? (
          <Card size="small">
            <Text type="secondary">Участки не найдены.</Text>
          </Card>
        ) : (
          <Row gutter={[16, 16]}>
            {areas.map((area) => {
              const meta = roleMeta(area.selectionRole)
              return (
                <Col key={area.aoiId} xs={24} sm={12} lg={6}>
                  <Card
                    size="small"
                    style={{ height: '100%' }}
                    title={area.aoiId}
                    extra={<Tag color={meta.color}>{meta.label}</Tag>}
                    actions={[
                      <Button key="open" type="link" onClick={() => openAnalysis(area.aoiId)}>
                        Открыть проверку
                      </Button>
                    ]}
                  >
                    <Paragraph style={{ marginBottom: 8 }}>{area.name}</Paragraph>
                    <Text type="secondary" style={{ display: 'block' }}>
                      {area.region ?? '—'}
                    </Text>
                    <Text strong>{formatNumber(area.areaHa)} га</Text>
                  </Card>
                </Col>
              )
            })}
          </Row>
        )}
      </div>

      <Card title="Последние проверки" size="small">
        <Table
          size="small"
          rowKey="runId"
          pagination={false}
          dataSource={runs}
          locale={{ emptyText: 'Проверки ещё не запускались.' }}
          columns={[
            {
              title: 'Проверка',
              key: 'name',
              render: (_, run) => run.name || run.aoiId || run.runId || '—'
            },
            {
              title: 'Период',
              key: 'period',
              render: (_, run) =>
                run.startYear || run.endYear ? `${run.startYear ?? '—'}–${run.endYear ?? '—'}` : '—'
            },
            {
              title: 'Статус',
              key: 'status',
              render: (_, run) => {
                const meta = RUN_STATUS[run.status] ?? {
                  color: 'default',
                  label: run.status ?? '—'
                }
                return <Tag color={meta.color}>{meta.label}</Tag>
              }
            },
            {
              title: 'Дата',
              dataIndex: 'createdAt',
              render: (value) => formatDate(value)
            },
            {
              title: '',
              key: 'action',
              render: (_, run) => (
                <Button type="link" onClick={() => navigate(`/analysis?run=${run.runId}`)}>
                  Открыть
                </Button>
              )
            }
          ]}
        />
      </Card>

      <Card title="Каталог источников" size="small">
        {sourcesQuery.isError ? (
          <Alert
            type="error"
            showIcon
            message="Не удалось загрузить каталог источников"
            description={sourcesQuery.error?.message}
          />
        ) : (
          <Table
            size="small"
            rowKey="sourceId"
            loading={sourcesQuery.isLoading}
            pagination={false}
            dataSource={sources}
            locale={{ emptyText: 'Источники не найдены.' }}
            columns={[
              { title: 'source_id', dataIndex: 'sourceId', width: 180 },
              { title: 'Продукт', dataIndex: 'product' },
              { title: 'Версия', dataIndex: 'version', width: 160 },
              { title: 'Тип', dataIndex: 'kind', width: 160 },
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
              },
              {
                title: 'Ограничение',
                dataIndex: 'limitations',
                render: (value) => <Text type="secondary">{value ?? '—'}</Text>
              }
            ]}
          />
        )}
      </Card>
    </Space>
  )
}
