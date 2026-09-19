// Клиентский HTML-отчёт ForestProof. Используется как резервный экспорт,
// когда серверный отчёт (POST /analyses/{id}/reports) недоступен.
// Разделы соответствуют ТЗ 7.6; все интерполируемые строки экранируются.

import {
  BLOCK_REASON_LABELS,
  CAUSE_STATUS,
  DISCLAIMERS,
  EVIDENCE_LABELS,
  RUN_STATUS,
  formatDate,
  formatMoney,
  formatNumber,
  formatPercent,
  formatUnits
} from '../constants'

const escapeHtml = (value) => {
  if (value === null || value === undefined) return ''
  return String(value)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;')
}

const text = (value, fallback = '—') =>
  escapeHtml(value === null || value === undefined || value === '' ? fallback : value)

const n = (value, digits = 2) => escapeHtml(formatNumber(value, digits))
const cnt = (value) => escapeHtml(formatUnits(value))
const rub = (value) => escapeHtml(formatMoney(value))
const pc = (value, digits = 1) => escapeHtml(formatPercent(value, digits))
const dt = (value) => escapeHtml(formatDate(value))

const statusLabel = (value) => RUN_STATUS[value]?.label ?? value
const causeLabel = (value) => CAUSE_STATUS[value]?.label ?? value
const evidenceLabel = (value) => EVIDENCE_LABELS[value] ?? value
const reasonLabel = (value) => BLOCK_REASON_LABELS[value] ?? value

const kv = (label, value, unit = '') =>
  `<tr><th>${escapeHtml(label)}</th><td>${value}${unit ? ` <span class="unit">${escapeHtml(unit)}</span>` : ''}</td></tr>`

const th = (label, extra = '') => `<th${extra}>${escapeHtml(label)}</th>`

export const buildReportHtml = (
  summary,
  { zones = [], sources = [], request = null, status = null } = {}
) => {
  if (!summary) {
    return `<!doctype html>
<html lang="ru">
<head><meta charset="utf-8"><title>ForestProof — отчёт</title></head>
<body><p>Нет данных для формирования отчёта.</p></body>
</html>`
  }

  const first = summary.yearlySeries?.[0] ?? {}
  const last = summary.yearlySeries?.[summary.yearlySeries.length - 1] ?? {}
  const change = summary.change ?? {}
  const uncertainty = summary.uncertainty ?? {}
  const baseline = summary.baseline ?? {}
  const unitsInfo = summary.units ?? {}
  const runStatus = status?.status ?? summary.status

  const qValue = unitsInfo.units
  const qUnavailable = qValue === null || qValue === undefined
  const qZero = qValue === 0
  const qText = qUnavailable
    ? '<span class="muted">недоступно</span>'
    : escapeHtml(formatUnits(qValue))

  const zoneList =
    Array.isArray(zones) && zones.length > 0
      ? zones
      : (summary.changeZones ?? []).map((zone) => {
          const evidence = (summary.changeZoneEvidence ?? []).find(
            (item) => item.zoneId === zone.id
          )
          return {
            ...zone,
            evidenceTypes: evidence?.evidenceTypes ?? [],
            causeStatus: evidence?.causeStatus ?? 'undetermined'
          }
        })

  const yearlyRows = (summary.yearlySeries ?? [])
    .map(
      (item) => `<tr>
        <td>${text(item.year)}</td>
        <td>${n(item.areaHectares)}</td>
        <td>${n(item.totalCarbon)}</td>
        <td>${n(item.meanCarbonPerHectare, 3)}</td>
        <td>${pc(item.coverage)}</td>
      </tr>`
    )
    .join('')

  const zoneRows =
    zoneList.length === 0
      ? '<tr><td colspan="6">Зоны изменений не выявлены.</td></tr>'
      : zoneList
          .map((zone) => {
            const evidence =
              (zone.evidenceTypes ?? []).map(evidenceLabel).join(', ') || '—'
            return `<tr>
              <td>${text(zone.id)}</td>
              <td>${n(zone.areaHectares)}</td>
              <td>${n(zone.contributionToDeltaCarbon)}</td>
              <td>${escapeHtml(evidence)}</td>
              <td>${text(causeLabel(zone.causeStatus))}</td>
              <td>${text(zone.interpretation)}</td>
            </tr>`
          })
          .join('')

  const priceRows = (unitsInfo.priceScenarios ?? [])
    .map(
      (scenario) => `<tr>
        <td>${cnt(scenario.pricePerUnit)}</td>
        <td>${rub(scenario.value)}</td>
      </tr>`
    )
    .join('')

  const sourceRows =
    sources.length === 0
      ? '<tr><td colspan="4">Каталог источников пуст.</td></tr>'
      : sources
          .map(
            (source) => `<tr>
              <td>${text(source.sourceId)}</td>
              <td>${text(source.product)}</td>
              <td>${text(source.version)}</td>
              <td>${
                source.licenseUrl
                  ? `<a href="${escapeHtml(source.licenseUrl)}">${escapeHtml(source.licenseUrl)}</a>`
                  : '—'
              }</td>
            </tr>`
          )
          .join('')

  const sourceFiles = sources.flatMap((source) =>
    (source.files ?? []).map((file) => ({ ...file, sourceId: source.sourceId }))
  )

  const sourceHashRows =
    sourceFiles.length === 0
      ? '<tr><td colspan="5">Файлы источников не зарегистрированы.</td></tr>'
      : sourceFiles
          .map(
            (file) => `<tr>
              <td>${text(file.sourceId)}</td>
              <td>${text(file.relativePath)}</td>
              <td class="hash">${text(file.sha256Recorded)}</td>
              <td class="hash">${text(file.sha256Actual)}</td>
              <td>${text(file.verified)}</td>
            </tr>`
          )
          .join('')

  const warnings = Array.isArray(summary.warnings) ? summary.warnings : []
  const limitationItems = [
    ...warnings.map((warning) => text(warning)),
    ...Object.values(DISCLAIMERS).map((item) => text(item))
  ]
  const limitations = limitationItems.map((item) => `<li>${item}</li>`).join('')

  const methodProfile = request?.methodProfile ?? 'default'
  const aoiLabel = summary.aoiId ? summary.aoiId : 'Произвольный контур'

  return `<!doctype html>
<html lang="ru">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>ForestProof — отчёт ${text(summary.runId)}</title>
<style>
  :root { color-scheme: light; }
  * { box-sizing: border-box; }
  body { font-family: 'Segoe UI', Roboto, Arial, sans-serif; color: #1f2d3d; margin: 0; padding: 24px; line-height: 1.5; background: #fff; }
  h1 { font-size: 22px; margin: 0 0 4px; }
  h2 { font-size: 16px; margin: 24px 0 8px; padding-bottom: 4px; border-bottom: 1px solid #d9d9d9; }
  p { margin: 4px 0; }
  table { border-collapse: collapse; width: 100%; margin: 8px 0 16px; font-size: 13px; }
  th, td { border: 1px solid #d9d9d9; padding: 6px 8px; text-align: left; vertical-align: top; }
  th { background: #f5f5f5; font-weight: 600; }
  tbody th { background: #fafafa; width: 320px; }
  .meta { color: #5a6673; font-size: 12px; }
  .unit { color: #5a6673; font-size: 12px; }
  .muted { color: #8c8c8c; }
  .hash { font-family: 'Consolas', 'Menlo', monospace; font-size: 11px; word-break: break-all; }
  .signature { margin-top: 24px; padding: 12px; border: 1px solid #d9d9d9; background: #fffbe6; font-weight: 600; }
  .disclaimer { font-size: 12px; color: #5a6673; }
  ul { margin: 4px 0 12px; padding-left: 20px; }
  a { color: #2f7d32; }
  @media print {
    body { padding: 0; }
    h2 { break-after: avoid; }
    tr { break-inside: avoid; }
  }
</style>
</head>
<body>
  <h1>ForestProof — отчёт о проверке</h1>
  <p class="meta">
    run_id: <strong>${text(summary.runId)}</strong> ·
    статус: <strong>${text(statusLabel(runStatus))}</strong> ·
    сформирован: ${dt(summary.createdAt)} ·
    method_version: ${text(summary.methodVersion)} ·
    data_version: ${text(summary.dataVersion)}
  </p>

  <h2>1. Территория и период</h2>
  <table>
    <tbody>
      ${kv('Территория', text(aoiLabel))}
      ${kv('Период', `${text(summary.startYear)}–${text(summary.endYear)}`)}
      ${kv('Площадь полигона', n(summary.polygonAreaHectares), 'га')}
      ${kv('Метод', text(summary.methodVersion))}
      ${kv('Версия данных', text(summary.dataVersion))}
      ${kv('Дата расчёта', dt(summary.createdAt))}
      ${kv('input_hash', `<span class="hash">${text(summary.inputHash)}</span>`)}
    </tbody>
  </table>

  <h2>2. Итоговая таблица</h2>
  <table>
    <tbody>
      ${kv(`C_t0 (${text(summary.startYear)})`, n(first.totalCarbon), 'т C')}
      ${kv(`C_t1 (${text(summary.endYear)})`, n(last.totalCarbon), 'т C')}
      ${kv('ΔC', n(change.deltaCarbon), 'т C')}
      ${kv('Eproj', n(change.projectEmission), 'т CO₂-экв.')}
      ${kv('e', n(change.emissionPerHectarePerYear, 3), 'т CO₂-экв./га/год')}
      ${kv('L', n(uncertainty.lower), 'т CO₂-экв.')}
      ${kv('U', n(uncertainty.upper), 'т CO₂-экв.')}
      ${kv('H', n(uncertainty.halfWidth), 'т CO₂-экв.')}
      ${kv(`Покрытие t0 (${text(summary.startYear)})`, pc(first.coverage))}
      ${kv(`Покрытие t1 (${text(summary.endYear)})`, pc(last.coverage))}
      ${kv('Ebase', n(baseline.baselineEmission), 'т CO₂-экв.')}
      ${kv('R (результат относительно baseline)', n(unitsInfo.resultRelativeToBaseline), 'т CO₂-экв.')}
      ${kv('UNC (вычет неопределённости)', n(unitsInfo.uncertaintyDeduction, 4))}
      ${kv('Radj', n(unitsInfo.adjustedResult), 'т CO₂-экв.')}
      ${kv('B (резерв 15%)', n(unitsInfo.reserve), 'т CO₂-экв.')}
      ${kv('Q (потенциальные единицы)', qText, qUnavailable ? '' : 'ед.')}
    </tbody>
  </table>
  ${
    qZero
      ? '<p class="disclaimer">Q = 0: условия расчёта выполнены, положительного результата относительно baseline нет.</p>'
      : ''
  }
  ${
    qUnavailable
      ? `<p class="disclaimer">Q недоступен: ${text(reasonLabel(unitsInfo.reason))}.</p>`
      : ''
  }

  <h2>3. Ценовые сценарии</h2>
  ${
    priceRows
      ? `<table>
    <thead><tr>${th('Цена, руб./ед.')}${th('Сценарная стоимость')}</tr></thead>
    <tbody>${priceRows}</tbody>
  </table>`
      : '<p class="disclaimer">Ценовые сценарии не заданы.</p>'
  }

  <h2>4. Годовая динамика</h2>
  <table>
    <thead>
      <tr>
        ${th('Год')}
        ${th('Площадь, га')}
        ${th('C, т C')}
        ${th('c̄, т C/га')}
        ${th('Покрытие')}
      </tr>
    </thead>
    <tbody>${yearlyRows || '<tr><td colspan="5">Нет данных годовой динамики.</td></tr>'}</tbody>
  </table>

  <h2>5. Зоны изменений</h2>
  <table>
    <thead>
      <tr>
        ${th('Id')}
        ${th('Площадь, га')}
        ${th('Вклад в ΔC, т C')}
        ${th('Подтверждения')}
        ${th('Статус причины')}
        ${th('Интерпретация')}
      </tr>
    </thead>
    <tbody>${zoneRows}</tbody>
  </table>

  <h2>6. Источники</h2>
  <table>
    <thead>
      <tr>
        ${th('source_id')}
        ${th('Продукт')}
        ${th('Версия')}
        ${th('Лицензия')}
      </tr>
    </thead>
    <tbody>${sourceRows}</tbody>
  </table>

  <h2>7. Метод</h2>
  <table>
    <tbody>
      ${kv('method_version', text(summary.methodVersion))}
      ${kv('data_version', text(summary.dataVersion))}
      ${kv('Метод (профиль)', text(methodProfile))}
    </tbody>
  </table>

  <h2>8. Baseline</h2>
  <table>
    <tbody>
      ${kv('Ebase', n(baseline.baselineEmission), 'т CO₂-экв.')}
      ${kv('R (результат относительно baseline)', n(unitsInfo.resultRelativeToBaseline), 'т CO₂-экв.')}
      ${kv('Статус единиц', text(reasonLabel(unitsInfo.reason)))}
    </tbody>
  </table>

  <h2>9. Неопределённость</h2>
  <table>
    <tbody>
      ${kv('L', n(uncertainty.lower), 'т CO₂-экв.')}
      ${kv('U', n(uncertainty.upper), 'т CO₂-экв.')}
      ${kv('H', n(uncertainty.halfWidth), 'т CO₂-экв.')}
    </tbody>
  </table>
  <p class="disclaimer">${escapeHtml(DISCLAIMERS.uncertainty)}</p>

  <h2>10. Ограничения</h2>
  <ul>${limitations}</ul>

  <h2>11. Параметры</h2>
  <table>
    <tbody>
      ${kv('Начальный год', text(summary.startYear))}
      ${kv('Конечный год', text(summary.endYear))}
      ${kv('Профиль метода', text(methodProfile))}
      ${kv('Территория', text(aoiLabel))}
    </tbody>
  </table>

  <h2>12. Хэши</h2>
  <table>
    <tbody>
      ${kv('input_hash', `<span class="hash">${text(summary.inputHash)}</span>`)}
    </tbody>
  </table>
  <table>
    <thead>
      <tr>
        ${th('source_id')}
        ${th('Файл')}
        ${th('sha256 (запись)')}
        ${th('sha256 (факт)')}
        ${th('Проверка')}
      </tr>
    </thead>
    <tbody>${sourceHashRows}</tbody>
  </table>

  <div class="signature">Потенциальные, не сертифицированные единицы.</div>
  <p class="disclaimer">${escapeHtml(DISCLAIMERS.units)}</p>
</body>
</html>`
}
