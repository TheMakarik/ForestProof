import { Alert, Progress, Space, Steps, Typography } from 'antd'
import { RUN_STATUS } from '../constants'

const { Text } = Typography

const PHASES = ['Проверка данных', 'Расчёт', 'Готово']
const SUCCESS_STATUSES = ['complete', 'partial', 'units_unavailable']

const resolveCurrent = (status) => {
  if (status === 'running') return 1
  if (SUCCESS_STATUSES.includes(status) || status === 'failed') return 2
  return 0
}

const resolveStepStatus = (status) => {
  if (status === 'failed') return 'error'
  if (SUCCESS_STATUSES.includes(status)) return 'finish'
  return 'process'
}

export default function ProgressPanel({ status, progress, phase, errorMessage, warnings }) {
  const normalized =
    progress === null || progress === undefined
      ? 0
      : progress > 1
        ? progress
        : progress * 100
  const percent = Math.max(0, Math.min(100, Math.round(normalized)))
  const isFailed = status === 'failed'
  const isDone = SUCCESS_STATUSES.includes(status)

  return (
    <Space direction="vertical" size="middle" style={{ width: '100%' }}>
      <Steps
        size="small"
        current={resolveCurrent(status)}
        status={resolveStepStatus(status)}
        items={PHASES.map((title) => ({ title }))}
      />

      <div>
        <Space align="center" style={{ justifyContent: 'space-between', width: '100%' }}>
          <Text strong>{RUN_STATUS[status]?.label ?? 'Статус неизвестен'}</Text>
          <Text type="secondary">{percent} %</Text>
        </Space>
        <Progress
          percent={percent}
          status={isFailed ? 'exception' : isDone ? 'success' : 'active'}
          showInfo={false}
        />
        {phase ? <Text type="secondary">{phase}</Text> : null}
      </div>

      {errorMessage ? (
        <Alert type="error" showIcon message="Ошибка расчёта" description={errorMessage} />
      ) : null}

      {Array.isArray(warnings) && warnings.length > 0 ? (
        <Alert
          type="warning"
          showIcon
          message="Предупреждения"
          description={
            <ul style={{ margin: 0, paddingLeft: 18 }}>
              {warnings.map((warning, index) => (
                <li key={`${warning}-${index}`}>{warning}</li>
              ))}
            </ul>
          }
        />
      ) : null}
    </Space>
  )
}
