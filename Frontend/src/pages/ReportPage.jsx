import { useState } from 'react'
import { useSearchParams, useNavigate } from 'react-router-dom'
import {
  Alert,
  Button,
  Card,
  Descriptions,
  Progress,
  Result,
  Space,
  Spin,
  Steps,
  Typography,
  message
} from 'antd'
import {
  DownloadOutlined,
  FileExcelOutlined,
  FilePdfOutlined,
  FileTextOutlined,
  PrinterOutlined,
  ReloadOutlined
} from '@ant-design/icons'
import { api } from '../api/client'
import { useAnalysisRun } from '../hooks/useAnalysisRun'
import { useSources } from '../hooks/queries'
import { getRunContext } from '../utils/runContext'
import { buildReportHtml } from '../utils/report'
import { formatDate } from '../constants'
import ReportView from '../components/ReportView'
import StatusTag from '../components/StatusTag'

const { Title, Text } = Typography

const PHASES = [
  { key: 'draft', title: 'Черновик' },
  { key: 'validating', title: 'Проверка данных' },
  { key: 'calling_summary', title: 'Сводка' },
  { key: 'calling_changes', title: 'Зоны изменений' },
  { key: 'calling_report', title: 'Формирование отчёта' },
  { key: 'complete', title: 'Готово' }
]

const downloadBlob = (blob, filename) => {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}

export default function ReportPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const run = searchParams.get('run') ?? ''

  const { status, summary, zones, ready, failed, isError, error, progress, phase, refetch } =
    useAnalysisRun(run)
  const { data: sources = [] } = useSources()
  const request = run ? getRunContext(run) : null

  const [busy, setBusy] = useState({ html: false, pdf: false, json: false, csv: false })
  const [toolError, setToolError] = useState(null)

  const setBusyFlag = (key, value) => setBusy((prev) => ({ ...prev, [key]: value }))

  const fileBase = `forestproof-${summary?.aoiId || 'polygon'}-${summary?.startYear ?? ''}-${summary?.endYear ?? ''}`

  const failTool = (err, fallbackMessage) => {
    const text = err?.message ?? fallbackMessage
    setToolError(text)
    message.error(text)
  }

  const downloadClientHtml = () => {
    if (!summary) return
    const html = buildReportHtml(summary, { zones, sources, request, status })
    downloadBlob(new Blob([html], { type: 'text/html;charset=utf-8' }), `${fileBase}.html`)
  }

  const openHtml = async () => {
    setBusyFlag('html', true)
    setToolError(null)
    try {
      const text = await api.getReportText(run, 'html')
      const tab = window.open('', '_blank')
      if (!tab) throw new Error('Браузер заблокировал новое окно. Разрешите всплывающие окна.')
      tab.document.write(text)
      tab.document.close()
    } catch (err) {
      failTool(err, 'Не удалось открыть серверный HTML-отчёт')
      downloadClientHtml()
    } finally {
      setBusyFlag('html', false)
    }
  }

  const downloadPdf = async () => {
    setBusyFlag('pdf', true)
    setToolError(null)
    try {
      const blob = await api.getReport(run, 'pdf')
      downloadBlob(blob, `${fileBase}.pdf`)
    } catch (err) {
      failTool(err, 'Не удалось скачать PDF-отчёт')
    } finally {
      setBusyFlag('pdf', false)
    }
  }

  const downloadJson = async () => {
    setBusyFlag('json', true)
    setToolError(null)
    try {
      const blob = await api.getReport(run, 'json')
      downloadBlob(blob, `${fileBase}.json`)
    } catch (err) {
      failTool(err, 'Не удалось скачать JSON-отчёт')
    } finally {
      setBusyFlag('json', false)
    }
  }

  const downloadCsv = async () => {
    setBusyFlag('csv', true)
    setToolError(null)
    try {
      const blob = await api.getYearlyCsv(run)
      downloadBlob(blob, `${fileBase}-yearly.csv`)
    } catch (err) {
      failTool(err, 'Не удалось скачать CSV годовой динамики')
    } finally {
      setBusyFlag('csv', false)
    }
  }

  if (!run) {
    return (
      <Result
        status="info"
        title="Отчёт недоступен"
        subTitle="Сначала откройте проверку, чтобы сформировать отчёт."
        extra={
          <Button type="primary" onClick={() => navigate('/new')}>
            Новая проверка
          </Button>
        }
      />
    )
  }

  if (isError && !status) {
    return (
      <Result
        status="error"
        title="Не удалось загрузить проверку"
        subTitle={error?.message ?? 'Ошибка запроса к шлюзу'}
        extra={
          <Button icon={<ReloadOutlined />} onClick={refetch}>
            Повторить
          </Button>
        }
      />
    )
  }

  if (failed) {
    return (
      <Result
        status="error"
        title="Расчёт завершился ошибкой"
        subTitle={status?.errorMessage ?? error?.message ?? 'Неизвестная ошибка'}
        extra={
          <Button icon={<ReloadOutlined />} onClick={refetch}>
            Повторить
          </Button>
        }
      />
    )
  }

  if (!ready) {
    const rawPercent = progress <= 1 ? progress * 100 : progress
    const percent = Math.max(0, Math.min(100, Math.round(rawPercent)))
    const activeIndex = PHASES.findIndex((item) => item.key === phase)
    return (
      <Space direction="vertical" size="large" style={{ width: '100%' }}>
        <div>
          <Title level={2} style={{ marginBottom: 4 }}>
            Отчёт
          </Title>
          <Text type="secondary">Проверка {run}</Text>
        </div>
        <Card size="small" title="Расчёт выполняется">
          <Progress percent={percent} status="active" />
          <Text type="secondary" style={{ display: 'block', marginTop: 8 }}>
            {phase ? `Текущая стадия: ${phase}` : 'Ожидание данных…'}
          </Text>
          <Steps
            direction="vertical"
            size="small"
            current={activeIndex >= 0 ? activeIndex : 0}
            items={PHASES.map((item) => ({ title: item.title }))}
            style={{ marginTop: 16 }}
          />
        </Card>
      </Space>
    )
  }

  if (!summary) {
    return (
      <Space direction="vertical" align="center" style={{ width: '100%', padding: 64 }}>
        <Spin size="large" />
        <Text type="secondary">Загрузка отчёта…</Text>
      </Space>
    )
  }

  const aoiLabel = summary.aoiId ? summary.aoiId : 'Произвольный контур'

  return (
    <Space direction="vertical" size="large" style={{ width: '100%' }}>
      <div>
        <Space align="center" wrap>
          <Title level={2} style={{ margin: 0 }}>
            {aoiLabel}
          </Title>
          <StatusTag type="run" value={status?.status ?? summary.status} />
          <Text type="secondary">
            {summary.startYear}–{summary.endYear}, {summary.endYear - summary.startYear} лет
          </Text>
        </Space>
        <Descriptions size="small" column={{ xs: 1, sm: 2, md: 4 }} style={{ marginTop: 8 }}>
          <Descriptions.Item label="run_id">{summary.runId}</Descriptions.Item>
          <Descriptions.Item label="method_version">{summary.methodVersion}</Descriptions.Item>
          <Descriptions.Item label="data_version">{summary.dataVersion}</Descriptions.Item>
          <Descriptions.Item label="Дата расчёта">
            {formatDate(summary.createdAt)}
          </Descriptions.Item>
        </Descriptions>
      </div>

      <Card size="small" title="Экспорт">
        <Space wrap>
          <Button icon={<FileTextOutlined />} loading={busy.html} onClick={openHtml}>
            Открыть HTML
          </Button>
          <Button icon={<FilePdfOutlined />} loading={busy.pdf} onClick={downloadPdf}>
            Скачать PDF
          </Button>
          <Button icon={<DownloadOutlined />} loading={busy.json} onClick={downloadJson}>
            Скачать JSON
          </Button>
          <Button icon={<FileExcelOutlined />} loading={busy.csv} onClick={downloadCsv}>
            Скачать CSV динамики
          </Button>
          <Button icon={<PrinterOutlined />} onClick={() => window.print()}>
            Печать
          </Button>
          <Button onClick={downloadClientHtml}>Резервный HTML</Button>
        </Space>
        {toolError && (
          <Alert
            type="error"
            showIcon
            style={{ marginTop: 12 }}
            message="Ошибка экспорта"
            description={toolError}
          />
        )}
      </Card>

      <ReportView
        summary={summary}
        zones={zones}
        sources={sources}
        request={request}
        status={status}
      />
    </Space>
  )
}
