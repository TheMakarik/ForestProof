import ReactECharts from 'echarts-for-react'
import { formatNumber } from '../constants'

export default function YearlyChart({ series = [], height = 320 }) {
  const years = series.map((item) => item.year)
  const totalCarbon = series.map((item) => Number(item.totalCarbon.toFixed(2)))
  const meanCarbon = series.map((item) => Number(item.meanCarbonPerHectare.toFixed(3)))
  const coverage = series.map((item) => Number((item.coverage * 100).toFixed(1)))

  const option = {
    tooltip: { trigger: 'axis' },
    legend: { data: ['C_t, т C', 'c̄_t, т C/га', 'coverage, %'] },
    grid: { left: 64, right: 64, top: 48, bottom: 40 },
    xAxis: { type: 'category', data: years },
    yAxis: [
      { type: 'value', name: 'т C', position: 'left' },
      { type: 'value', name: 'т C/га', position: 'right', max: 100 }
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
        name: 'coverage, %',
        type: 'bar',
        yAxisIndex: 1,
        barWidth: 14,
        data: coverage,
        itemStyle: { color: '#c8e6c9' },
        tooltip: { valueFormatter: (value) => `${formatNumber(value, 1)} %` }
      }
    ]
  }

  return <ReactECharts option={option} style={{ height }} notMerge lazyUpdate />
}
