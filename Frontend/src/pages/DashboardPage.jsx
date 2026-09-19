import { Alert, Button, Card, Col, Row, Space, Table, Tag, Typography } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { formatNumber } from '../constants'

const { Title, Paragraph, Text } = Typography

const roleColor = (role) => {
  if (role?.includes('контрольный')) return 'green'
  if (role?.includes('пожар')) return 'red'
  if (role?.includes('потер')) return 'orange'
  return 'blue'
}

export default function DashboardPage() {
  const navigate = useNavigate()
  const { data: areas = [] } = useQuery({ queryKey: ['areas'], queryFn: api.listAreas })
  const { data: sources = [] } = useQuery({ queryKey: ['sources'], queryFn: api.listSources })

  const openAnalysis = (aoiId) =>
    navigate(`/analysis?aoiId=${aoiId}&start=2019&end=2024`)

  return (
    <Space direction="vertical" size="large" style={{ width: '100%' }}>
      <div>
        <Title level={2} style={{ marginBottom: 4 }}>
          Дашборд
        </Title>
        <Paragraph type="secondary" style={{ marginBottom: 0 }}>
          Спутниковая верификация «зелёных» инвестиций. Учитываемый пул — живая надземная древесная
          биомасса. Период 2019–2024.
        </Paragraph>
      </div>

      <Alert
        type="info"
        showIcon
        message="Потенциальные, не сертифицированные единицы"
        description="Сервис не выпускает кредиты, не доказывает дополнительность и не заменяет наземную инвентаризацию."
      />

      <Row gutter={[16, 16]}>
        {areas.map((area) => (
          <Col key={area.aoi_id} xs={24} sm={12} lg={6}>
            <Card
              title={area.aoi_id}
              extra={<Tag color={roleColor(area.selection_role)}>{area.selection_role}</Tag>}
              actions={[
                <Button key="open" type="link" onClick={() => openAnalysis(area.aoi_id)}>
                  Открыть проверку
                </Button>
              ]}
            >
              <Paragraph style={{ marginBottom: 8 }}>{area.name}</Paragraph>
              <Text type="secondary" style={{ display: 'block' }}>
                {area.region}
              </Text>
              <Text strong>{formatNumber(area.area_ha)} га</Text>
            </Card>
          </Col>
        ))}
      </Row>

      <Card title="Каталог источников" size="small">
        <Table
          size="small"
          rowKey="source_id"
          pagination={false}
          dataSource={sources}
          columns={[
            { title: 'source_id', dataIndex: 'source_id', width: 180 },
            { title: 'Продукт', dataIndex: 'product' },
            { title: 'Версия', dataIndex: 'version', width: 180 },
            {
              title: 'Ограничение',
              dataIndex: 'limitations',
              render: (value) => <Text type="secondary">{value}</Text>
            }
          ]}
        />
      </Card>
    </Space>
  )
}
