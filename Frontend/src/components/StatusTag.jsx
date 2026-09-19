import { Tag, Tooltip } from 'antd'
import { CAUSE_STATUS, RUN_STATUS, UNIT_STATUS } from '../constants'

const maps = {
  run: RUN_STATUS,
  unit: UNIT_STATUS,
  cause: CAUSE_STATUS
}

export default function StatusTag({ type = 'run', value, tooltip }) {
  const meta = maps[type]?.[value] ?? { color: 'default', label: value ?? '—' }
  const tag = <Tag color={meta.color}>{meta.label}</Tag>
  return tooltip ? <Tooltip title={tooltip}>{tag}</Tooltip> : tag
}
