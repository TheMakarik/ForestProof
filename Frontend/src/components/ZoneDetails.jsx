import { Alert, Card, Descriptions, Divider, Empty, Space, Tag, Typography } from 'antd'
import { CAUSE_STATUS, DISCLAIMERS, EVIDENCE_LABELS, formatNumber } from '../constants'

const { Paragraph, Text, Title } = Typography

const causeMeta = (status) => CAUSE_STATUS[status] ?? { color: 'default', label: status ?? '—' }

const periodOf = (zone) => {
  if (zone.period) {
    if (typeof zone.period === 'string') return zone.period
    if (zone.period.startYear || zone.period.endYear) {
      return `${zone.period.startYear ?? '—'}–${zone.period.endYear ?? '—'}`
    }
  }
  if (zone.startYear || zone.endYear) {
    return `${zone.startYear ?? '—'}–${zone.endYear ?? '—'}`
  }
  return null
}

function Interpretation({ interpretation }) {
  if (!interpretation) return null

  if (typeof interpretation === 'string') {
    return <Paragraph style={{ marginBottom: 0 }}>{interpretation}</Paragraph>
  }

  if (typeof interpretation === 'object') {
    const can =
      interpretation.canAssert ?? interpretation.can ?? interpretation.claimable ?? null
    const cannot =
      interpretation.cannotAssert ?? interpretation.cannot ?? interpretation.notClaimable ?? null

    if (can || cannot) {
      return (
        <Space direction="vertical" size={4} style={{ width: '100%' }}>
          {can && (
            <div>
              <Text strong>Можно утверждать: </Text>
              <Text>{can}</Text>
            </div>
          )}
          {cannot && (
            <div>
              <Text strong>Нельзя утверждать: </Text>
              <Text>{cannot}</Text>
            </div>
          )}
        </Space>
      )
    }

    const text = Object.values(interpretation)
      .filter((value) => value !== null && value !== undefined && value !== '')
      .join(' ')

    return text ? (
      <Paragraph style={{ marginBottom: 0 }}>{text}</Paragraph>
    ) : (
      <Paragraph style={{ marginBottom: 0 }}>{JSON.stringify(interpretation)}</Paragraph>
    )
  }

  return <Paragraph style={{ marginBottom: 0 }}>{String(interpretation)}</Paragraph>
}

export default function ZoneDetails({ zone }) {
  if (!zone) {
    return (
      <Card size="small" title="Зона">
        <Empty description="Выберите зону на карте или в таблице" />
      </Card>
    )
  }

  const cause = causeMeta(zone.causeStatus)
  const contribution = Number(zone.contributionToDeltaCarbon ?? 0)
  const period = periodOf(zone)
  const evidence = zone.evidenceTypes ?? []

  return (
    <Card
      size="small"
      title={`Зона ${zone.id}`}
      extra={<Tag color={cause.color}>{cause.label}</Tag>}
    >
      <Descriptions size="small" column={1}>
        <Descriptions.Item label="Площадь">{formatNumber(zone.areaHectares)} га</Descriptions.Item>
        <Descriptions.Item label="Вклад в ΔC">
          <Text type={contribution < 0 ? 'danger' : 'success'}>
            {formatNumber(contribution)} т C
          </Text>
        </Descriptions.Item>
        {period && <Descriptions.Item label="Период">{period}</Descriptions.Item>}
        <Descriptions.Item label="Подтверждения">
          {evidence.length === 0
            ? '—'
            : evidence.map((type) => (
                <Tag key={type} color="geekblue">
                  {EVIDENCE_LABELS[type] ?? type}
                </Tag>
              ))}
        </Descriptions.Item>
      </Descriptions>

      <Divider style={{ margin: '12px 0' }} />

      <Title level={5} style={{ marginTop: 0, marginBottom: 8 }}>
        Что можно утверждать / чего утверждать нельзя
      </Title>

      {zone.interpretation ? (
        <div style={{ marginBottom: 12 }}>
          <Interpretation interpretation={zone.interpretation} />
        </div>
      ) : (
        <Paragraph type="secondary" style={{ marginBottom: 12 }}>
          Явная интерпретация для зоны не сформирована: причина не установлена однозначно.
        </Paragraph>
      )}

      <Alert type="warning" showIcon message={DISCLAIMERS.cause} />
    </Card>
  )
}
