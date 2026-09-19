import { Tag, Tooltip } from 'antd'
import { CAUSE_STATUS, RUN_STATUS, UNIT_STATUS } from '../constants'

const STATUS_MAPS = {
  run: RUN_STATUS,
  unit: UNIT_STATUS,
  cause: CAUSE_STATUS
}

const humanize = (value) =>
  value === null || value === undefined || value === '' ? '—' : String(value)

export default function StatusTag({ type = 'run', value, tooltip }) {
  const meta = STATUS_MAPS[type]?.[value] ?? { color: 'default', label: humanize(value) }
  const tag = <Tag color={meta.color}>{meta.label}</Tag>
  return tooltip ? <Tooltip title={tooltip}>{tag}</Tooltip> : tag
}
