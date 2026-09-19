import ReactECharts from 'echarts-for-react'
import { Typography } from 'antd'
import { formatNumber } from '../constants'

const { Text } = Typography

export default function UncertaintyChart({ uncertainty, projectEmission, height = 220 }) {
  if (!uncertainty) {
    return <Text type="secondary">Неопределённость не рассчитана.</Text>
  }

  const lower = Number(uncertainty.lower ?? 0)
  const upper = Number(uncertainty.upper ?? 0)
  const halfWidth = Number(uncertainty.halfWidth ?? 0)
  const emission = Number(projectEmission ?? 0)

  const option = {
    tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
    grid: { left: 110, right: 48, top: 24, bottom: 32 },
    xAxis: { type: 'value', name: 'т CO₂-экв.' },
    yAxis: {
      type: 'category',
      data: ['Нижняя L', 'Проект Eproj', 'Верхняя U']
    },
    series: [
      {
        type: 'bar',
        barWidth: 22,
        data: [
          { value: Number(lower.toFixed(2)), itemStyle: { color: '#90a4ae' } },
          {
            value: Number(emission.toFixed(2)),
            itemStyle: { color: emission >= 0 ? '#c62828' : '#2e7d32' }
          },
          { value: Number(upper.toFixed(2)), itemStyle: { color: '#90a4ae' } }
        ],
        label: {
          show: true,
          position: 'right',
          formatter: ({ value }) => formatNumber(value)
        }
      }
    ]
  }

  return (
    <div>
      <ReactECharts option={option} style={{ height }} notMerge lazyUpdate />
      <div style={{ color: '#7b8794', fontSize: 12 }}>
        H = {formatNumber(halfWidth)} т CO₂-экв. · сценарный диапазон, не статистический доверительный
        интервал.
      </div>
    </div>
  )
}
