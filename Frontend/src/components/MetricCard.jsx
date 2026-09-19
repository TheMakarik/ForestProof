import { Card, Statistic, Tooltip, Typography } from 'antd'
import { InfoCircleOutlined } from '@ant-design/icons'

const { Text } = Typography

export default function MetricCard({ title, value, unit, hint, tone = 'default', extra }) {
  const valueColor =
    tone === 'loss' ? '#c62828' : tone === 'gain' ? '#2e7d32' : tone === 'muted' ? '#7b8794' : undefined

  return (
    <Card size="small" style={{ height: '100%' }}>
      <Statistic
        title={
          <span>
            {title}
            {hint && (
              <Tooltip title={hint}>
                <InfoCircleOutlined style={{ marginLeft: 6, color: '#8c8c8c' }} />
              </Tooltip>
            )}
          </span>
        }
        value={value}
        suffix={unit ? <Text type="secondary" style={{ fontSize: 13 }}>{unit}</Text> : null}
        valueStyle={{ color: valueColor, fontSize: 22 }}
      />
      {extra}
    </Card>
  )
}
