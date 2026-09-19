import { Table, Tag, Typography } from 'antd'
import { CAUSE_STATUS, EVIDENCE_LABELS, formatNumber } from '../constants'
import StatusTag from './StatusTag'

const { Text } = Typography

export default function ZonesTable({ zones = [], onSelect }) {
  if (zones.length === 0) {
    return <Text type="secondary">Зоны изменений не выявлены.</Text>
  }

  return (
    <Table
      size="small"
      rowKey="id"
      pagination={false}
      dataSource={zones}
      onRow={(zone) => ({
        onClick: () => onSelect?.(zone),
        style: { cursor: onSelect ? 'pointer' : 'default' }
      })}
      columns={[
        { title: 'Id', dataIndex: 'id', width: 56 },
        {
          title: 'Площадь, га',
          dataIndex: 'areaHectares',
          render: (value) => formatNumber(value)
        },
        {
          title: 'Вклад в ΔC, т C',
          dataIndex: 'contributionToDeltaCarbon',
          render: (value) => (
            <Text type={value < 0 ? 'danger' : 'success'}>{formatNumber(value)}</Text>
          )
        },
        {
          title: 'Подтверждения',
          dataIndex: 'evidenceTypes',
          render: (types) =>
            types.length === 0
              ? '—'
              : types.map((type) => (
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
        }
      ]}
    />
  )
}
