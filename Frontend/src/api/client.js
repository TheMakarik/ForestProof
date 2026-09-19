import { AREAS, AREAS_GEOJSON, SOURCES, buildSummary, buildSensitivity } from './mockData'

const USE_MOCK = import.meta.env.VITE_USE_MOCK !== 'false'
const API_BASE = import.meta.env.VITE_API_BASE ?? '/api/v1'

const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms))

const RUN_STATUS_MAP = {
  Complete: 'complete',
  Partial: 'partial',
  UnitsUnavailable: 'units_unavailable',
  Draft: 'draft',
  Validating: 'validating',
  Running: 'running',
  Failed: 'failed'
}

const UNIT_STATUS_MAP = {
  Available: 'computed',
  Zero: 'zero',
  Unavailable: 'unavailable'
}

const EVIDENCE_MAP = {
  Gfc: 'gfc',
  Modis: 'modis',
  Sentinel2: 'sentinel2',
  CciChange: 'cci_change'
}

const CAUSE_MAP = {
  Confirmed: 'confirmed',
  Probable: 'likely',
  Unknown: 'undetermined'
}

const PRICE_SCENARIOS = [500, 1500, 4000]

const normalizeSummary = (raw) => {
  const units = raw.units ?? {}
  const unitCount = units.units
  const priceScenarios =
    units.priceScenarios?.map((item) => ({ pricePerUnit: item.pricePerUnit, value: item.value })) ??
    PRICE_SCENARIOS.map((price) => ({ pricePerUnit: price, value: (unitCount ?? 0) * price }))

  const area = AREAS.find((item) => item.aoi_id === raw.aoiId)

  return {
    runId: raw.runId,
    methodVersion: raw.methodVersion,
    dataVersion: raw.dataVersion,
    createdAt: raw.createdAt,
    status: RUN_STATUS_MAP[raw.status] ?? String(raw.status ?? '').toLowerCase(),
    aoiId: raw.aoiId,
    startYear: raw.startYear,
    endYear: raw.endYear,
    polygonAreaHectares: raw.polygonAreaHectares,
    geometry: area?.geometry ?? null,
    yearlySeries: (raw.yearlySeries ?? []).map((item) => ({
      ...item,
      coverage: item.coverage,
      hasCompleteCoverage: item.coverage >= 1
    })),
    change: raw.change,
    uncertainty: raw.uncertainty,
    baseline: raw.baseline,
    units: {
      status: UNIT_STATUS_MAP[units.status] ?? String(units.status ?? '').toLowerCase(),
      reason: units.reason ?? 'None',
      resultRelativeToBaseline: units.resultRelativeToBaseline ?? 0,
      uncertaintyDeduction: units.uncertaintyDeduction ?? 0,
      adjustedResult: units.adjustedResult ?? 0,
      reserve: units.reserve ?? 0,
      units: unitCount,
      priceScenarios
    },
    changeZones: (raw.changeZones ?? []).map((zone) => {
      const evidence = (raw.changeZoneEvidence ?? []).find((item) => item.zoneId === zone.id)
      return {
        id: zone.id,
        areaHectares: zone.areaHectares,
        contributionToDeltaCarbon: zone.contributionToDeltaCarbon,
        period: `${raw.startYear}–${raw.endYear}`,
        evidenceTypes: (evidence?.evidenceTypes ?? []).map((type) => EVIDENCE_MAP[type] ?? type),
        causeStatus: CAUSE_MAP[evidence?.causeStatus] ?? 'undetermined',
        geometry: zone.geometry ?? null
      }
    }),
    changeZoneEvidence: raw.changeZoneEvidence ?? [],
    cciChangeMeanTonnesPerHectare: raw.cciChangeMeanTonnesPerHectare,
    warnings: raw.warnings ?? []
  }
}

const request = async (path, options = {}) => {
  const response = await fetch(`${API_BASE}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options
  })

  if (!response.ok) {
    let message = `Ошибка запроса (${response.status})`
    try {
      const body = await response.json()
      message = body.message ?? message
    } catch {
      // тело не JSON
    }
    throw new Error(message)
  }

  return response.json()
}

export const api = {
  isMock: USE_MOCK,

  async listAreas() {
    if (USE_MOCK) {
      await delay(150)
      return AREAS
    }
    return request('/areas')
  },

  getAreasGeoJson() {
    return AREAS_GEOJSON
  },

  async listSources() {
    if (USE_MOCK) {
      await delay(150)
      return SOURCES
    }
    return request('/sources')
  },

  async createAnalysis(payload) {
    if (USE_MOCK) {
      await delay(700)
      return normalizeSummary(buildSummary(payload))
    }
    const raw = await request('/analyses', {
      method: 'POST',
      body: JSON.stringify(payload)
    })
    return normalizeSummary(raw)
  },

  async getSummary({ aoiId, startYear, endYear }) {
    if (USE_MOCK) {
      await delay(700)
      return normalizeSummary(buildSummary({ aoiId, startYear, endYear }))
    }
    const raw = await request(`/analyses/${aoiId}/summary?startYear=${startYear}&endYear=${endYear}`)
    return normalizeSummary(raw)
  },

  async getSensitivity({ aoiId, startYear, endYear }) {
    if (USE_MOCK) {
      await delay(900)
      return buildSensitivity({ aoiId, startYear, endYear })
    }
    return request(
      `/experiments/sensitivity?aoiId=${aoiId}&startYear=${startYear}&endYear=${endYear}`
    )
  }
}

const row = (label, value) => `<tr><th>${label}</th><td>${value}</td></tr>`

export const buildReportHtml = (summary) => {
  const first = summary.yearlySeries[0]
  const last = summary.yearlySeries[summary.yearlySeries.length - 1]
  const zones = summary.changeZones
    .map(
      (zone) =>
        `<tr><td>${zone.id}</td><td>${zone.areaHectares.toFixed(2)}</td>` +
        `<td>${zone.contributionToDeltaCarbon.toFixed(2)}</td>` +
        `<td>${zone.evidenceTypes.join(', ') || '—'}</td><td>${zone.causeStatus}</td></tr>`
    )
    .join('')

  return `<!doctype html>
<html lang="ru"><head><meta charset="utf-8"><title>ForestProof — отчёт ${summary.aoiId}</title>
<style>
body{font-family:Arial,Helvetica,sans-serif;margin:32px;color:#1f2933}
h1{font-size:22px}h2{font-size:16px;border-bottom:1px solid #cbd2d9;margin-top:24px}
table{border-collapse:collapse;margin-top:8px;width:100%}th,td{border:1px solid #cbd2d9;padding:4px 10px;text-align:left}
th{background:#f0f4f8}.note{margin-top:24px;font-style:italic;color:#7b8794}
</style></head><body>
<h1>Отчёт ForestProof</h1>
<p>run_id: ${summary.runId}<br>Дата расчёта: ${new Date(summary.createdAt).toLocaleString('ru-RU')}<br>
method_version: ${summary.methodVersion} · data_version: ${summary.dataVersion}</p>
<h2>Территория и период</h2>
<table>${row('Территория', summary.aoiId)}${row('Период', `${summary.startYear}–${summary.endYear}`)}
${row('Площадь полигона, га', summary.polygonAreaHectares.toFixed(2))}
${row('Рассчитанная площадь, га', last.areaHectares.toFixed(2))}</table>
<h2>Итоговая таблица</h2>
<table>${row(`C_t0, т C (${summary.startYear})`, first.totalCarbon.toFixed(2))}
${row(`C_t1, т C (${summary.endYear})`, last.totalCarbon.toFixed(2))}
${row('ΔC, т C', summary.change.deltaCarbon.toFixed(2))}
${row('Eproj, т CO₂-экв.', summary.change.projectEmission.toFixed(2))}
${row('e, т CO₂-экв./га/год', summary.change.emissionPerHectarePerYear.toFixed(3))}
${row('L, т CO₂-экв.', summary.uncertainty.lower.toFixed(2))}
${row('U, т CO₂-экв.', summary.uncertainty.upper.toFixed(2))}
${row('H, т CO₂-экв.', summary.uncertainty.halfWidth.toFixed(2))}
${row('Ebase, т CO₂-экв.', summary.baseline.baselineEmission.toFixed(2))}
${row('R', summary.units.resultRelativeToBaseline.toFixed(2))}
${row('UNC', summary.units.uncertaintyDeduction.toFixed(4))}
${row('Radj', summary.units.adjustedResult.toFixed(2))}
${row('B', summary.units.reserve.toFixed(2))}
${row('Q', summary.units.units ?? 'недоступно')}</table>
<h2>Годовая динамика</h2>
<table><tr><th>Год</th><th>C_t, т C</th><th>c̄_t, т C/га</th><th>coverage</th></tr>
${summary.yearlySeries
  .map(
    (item) =>
      `<tr><td>${item.year}</td><td>${item.totalCarbon.toFixed(2)}</td>` +
      `<td>${item.meanCarbonPerHectare.toFixed(3)}</td><td>${item.coverage.toFixed(3)}</td></tr>`
  )
  .join('')}</table>
<h2>Зоны изменений</h2>
<table><tr><th>Id</th><th>Площадь, га</th><th>Вклад в ΔC, т C</th><th>Evidence</th><th>Причина</th></tr>${zones}</table>
<h2>Ограничения</h2>
<ul>${summary.warnings.map((warning) => `<li>${warning}</li>`).join('')}
<li>Диапазон неопределённости имеет сценарный статус, не является доверительным интервалом.</li>
<li>GFC подтверждает факт потери, но не причину. MODIS — сигнал возможной гари.</li></ul>
<p class="note">Потенциальные, не сертифицированные единицы. Расчёт по условиям кейса.</p>
</body></html>`
}
