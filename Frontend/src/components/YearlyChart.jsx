import ReactECharts from 'echarts-for-react'
import { Typography } from 'antd'
import { formatNumber } from '../constants'

const { Text } = Typography

export default function YearlyChart({ series = [], height = 320 }) {
  if (!Array.isArray(series) || series.length === 0) {
    return <Text type="secondary">Нет данных годовой динамики.</Text>
  }

  const years = series.map((item) => item.year)
  const totalCarbon = series.map((item) => Number((item.totalCarbon ?? 0).toFixed(2)))
  const meanCarbon = series.map((item) => Number((item.meanCarbonPerHectare ?? 0).toFixed(3)))
  const coverage = series.map((item) => Number(((item.coverage ?? 0) * 100).toFixed(1)))

  const option = {
    tooltip: {
      trigger: 'axis',
      valueFormatter: (value) => formatNumber(value, 2)
    },
    legend: { data: ['C_t, т C', 'c̄_t, т C/га', 'Покрытие, %'] },
    grid: { left: 72, right: 104, top: 48, bottom: 40 },
    xAxis: { type: 'category', data: years, name: 'Год' },
    yAxis: [
      { type: 'value', name: 'C_t, т C', position: 'left' },
      { type: 'value', name: 'c̄_t, т C/га', position: 'right' },
      {
        type: 'value',
        name: 'Покрытие, %',
        position: 'right',
        offset: 64,
        min: 0,
        max: 100
      }
    ],
    series: [
      {
        name: 'C_t, т C',
        type: 'line',
        smooth: true,
        symbolSize: 7,
        data: totalCarbon,
        itemStyle: { color: '#2f7d32' },
        areaStyle: { opacity: 0.08 }
      },
      {
        name: 'c̄_t, т C/га',
        type: 'line',
        smooth: true,
        yAxisIndex: 1,
        symbolSize: 7,
        data: meanCarbon,
        itemStyle: { color: '#1565c0' }
      },
      {
        name: 'Покрытие, %',
        type: 'bar',
        yAxisIndex: 2,
        barWidth: 14,
        data: coverage,
        itemStyle: { color: '#c8e6c9' },
        tooltip: { valueFormatter: (value) => `${formatNumber(value, 1)} %` }
      }
    ]
  }

  return <ReactECharts option={option} style={{ height }} notMerge lazyUpdate />
}
