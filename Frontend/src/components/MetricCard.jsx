import { Card, Statistic, Tooltip, Typography } from 'antd'
import { InfoCircleOutlined } from '@ant-design/icons'

const { Text } = Typography

const TONE_COLORS = {
  default: undefined,
  loss: '#c62828',
  gain: '#2e7d32',
  muted: '#7b8794'
}

export default function MetricCard({ title, value, unit, hint, tone = 'default', extra }) {
  const display = value === null || value === undefined || value === '' ? '—' : value
  const valueColor = TONE_COLORS[tone] ?? TONE_COLORS.default

  return (
    <Card size="small" style={{ height: '100%' }}>
      <Statistic
        title={
          <span>
            {title}
            {hint ? (
              <Tooltip title={hint}>
                <InfoCircleOutlined style={{ marginLeft: 6, color: '#8c8c8c' }} />
              </Tooltip>
            ) : null}
          </span>
        }
        value={display}
        suffix={unit ? <Text type="secondary" style={{ fontSize: 13 }}>{unit}</Text> : null}
        valueStyle={{ color: valueColor, fontSize: 22 }}
      />
      {extra}
    </Card>
  )
}
