import ReactECharts from 'echarts-for-react'
import { formatNumber } from '../constants'

export default function UncertaintyChart({ uncertainty, projectEmission, height = 220 }) {
  const { lower, upper, halfWidth } = uncertainty

  const option = {
    tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
    grid: { left: 90, right: 32, top: 24, bottom: 32 },
    xAxis: { type: 'value', name: 'т CO₂-экв.' },
    yAxis: {
      type: 'category',
      data: ['Нижняя L', 'Проект Eproj', 'Верхняя U']
    },
    series: [
      {
        type: 'bar',
        data: [
          { value: Number(lower.toFixed(2)), itemStyle: { color: '#90a4ae' } },
          {
            value: Number(projectEmission.toFixed(2)),
            itemStyle: { color: projectEmission >= 0 ? '#c62828' : '#2e7d32' }
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
