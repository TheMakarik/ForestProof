import { Table, Tag, Typography } from 'antd'
import { CAUSE_STATUS, EVIDENCE_LABELS, formatNumber } from '../constants'
import StatusTag from './StatusTag'

const { Text, Paragraph } = Typography

export default function ZonesTable({ zones = [], onSelect, selectedId }) {
  if (!Array.isArray(zones) || zones.length === 0) {
    return <Text type="secondary">Зоны изменений не выявлены.</Text>
  }

  return (
    <Table
      size="small"
      rowKey="id"
      pagination={false}
      scroll={{ x: 720 }}
      dataSource={zones}
      onRow={(zone) => ({
        onClick: () => onSelect?.(zone),
        style: {
          cursor: onSelect ? 'pointer' : 'default',
          background: zone.id === selectedId ? '#e8f5e9' : undefined
        }
      })}
      columns={[
        { title: 'Id', dataIndex: 'id', width: 72 },
        {
          title: 'Площадь (га)',
          dataIndex: 'areaHectares',
          width: 120,
          render: (value) => formatNumber(value)
        },
        {
          title: 'Вклад в ΔC (т C)',
          dataIndex: 'contributionToDeltaCarbon',
          width: 140,
          render: (value) => (
            <Text type={value < 0 ? 'danger' : 'success'}>{formatNumber(value)}</Text>
          )
        },
        {
          title: 'Подтверждения',
          dataIndex: 'evidenceTypes',
          width: 200,
          render: (types) =>
            Array.isArray(types) && types.length > 0
              ? types.map((type) => (
                  <Tag key={type} color="geekblue">
                    {EVIDENCE_LABELS[type] ?? type}
                  </Tag>
                ))
              : '—'
        },
        {
          title: 'Статус причины',
          dataIndex: 'causeStatus',
          width: 150,
          render: (value) => (
            <StatusTag type="cause" value={value} tooltip={CAUSE_STATUS[value]?.label} />
          )
        },
        {
          title: 'Интерпретация',
          dataIndex: 'interpretation',
          render: (value) =>
            value ? (
              <Paragraph ellipsis={{ rows: 2, tooltip: value }} style={{ marginBottom: 0 }}>
                {value}
              </Paragraph>
            ) : (
              '—'
            )
        }
      ]}
    />
  )
}
