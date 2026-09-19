import { Alert, Descriptions, Table, Tag, Typography } from 'antd'
import {
  BLOCK_REASON_LABELS,
  DISCLAIMERS,
  formatMoney,
  formatNumber,
  formatUnits
} from '../constants'
import StatusTag from './StatusTag'

const { Text } = Typography

export default function UnitsPanel({ units, baseline }) {
  if (!units) return null

  const isUnavailable = units.status === 'unavailable'
  const isZero = units.status === 'zero'
  const reason = BLOCK_REASON_LABELS[units.reason] ?? units.reason ?? 'Причина не указана'

  return (
    <div>
      <div style={{ marginBottom: 12 }}>
        <StatusTag
          type="unit"
          value={units.status}
          tooltip={isUnavailable ? 'Условия расчёта не выполнены' : undefined}
        />
        {isUnavailable && <Text type="secondary" style={{ marginLeft: 8 }}>Q недоступен</Text>}
        {isZero && (
          <Text type="secondary" style={{ marginLeft: 8 }}>
            Q = 0, расчёт допустим, результата нет
          </Text>
        )}
      </div>

      {isUnavailable && (
        <Alert
          type="error"
          showIcon
          style={{ marginBottom: 12 }}
          message="Q недоступен"
          description={reason}
        />
      )}
      {isZero && (
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 12 }}
          message="Q = 0, расчёт допустим, результата нет"
          description={reason}
        />
      )}

      <Descriptions
        size="small"
        column={{ xs: 1, sm: 2, md: 3 }}
        bordered
        style={{ marginBottom: 12 }}
      >
        <Descriptions.Item label="Ebase, т CO₂-экв.">
          {formatNumber(baseline?.baselineEmission)}
        </Descriptions.Item>
        <Descriptions.Item label="R, т CO₂-экв.">
          {formatNumber(units.resultRelativeToBaseline)}
        </Descriptions.Item>
        <Descriptions.Item label="UNC">{formatNumber(units.uncertaintyDeduction, 4)}</Descriptions.Item>
        <Descriptions.Item label="Radj, т CO₂-экв.">
          {formatNumber(units.adjustedResult)}
        </Descriptions.Item>
        <Descriptions.Item label="B (резерв 15%), т CO₂-экв.">
          {formatNumber(units.reserve)}
        </Descriptions.Item>
        <Descriptions.Item label="Q (потенциальные единицы)">
          <Text strong>{formatUnits(units.units)}</Text>
        </Descriptions.Item>
      </Descriptions>

      {units.priceScenarios?.length > 0 && (
        <Table
          size="small"
          rowKey="pricePerUnit"
          pagination={false}
          dataSource={units.priceScenarios}
          columns={[
            {
              title: 'Цена, руб./ед.',
              dataIndex: 'pricePerUnit',
              render: (value) => <Tag color="blue">{formatUnits(value)}</Tag>
            },
            {
              title: 'Сценарная стоимость',
              dataIndex: 'value',
              render: (value) => formatMoney(value)
            }
          ]}
        />
      )}

      <Text type="secondary" style={{ display: 'block', marginTop: 12, fontSize: 12 }}>
        {DISCLAIMERS.units} Цены 500 / 1 500 / 4 000 руб. за единицу — сценарные и не являются
        прогнозом рыночной стоимости.
      </Text>
    </div>
  )
}
