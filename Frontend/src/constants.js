// Общие константы, статусы, слои и форматтеры ForestProof.
// Единый источник текстов ограничений, чтобы формулировки не расходились
// между экранами и отчётом.

export const MIN_YEAR = 2019
export const MAX_YEAR = 2024
export const DEFAULT_START_YEAR = 2019
export const DEFAULT_END_YEAR = 2024
export const YEARS = Array.from(
  { length: MAX_YEAR - MIN_YEAR + 1 },
  (_, index) => MIN_YEAR + index
)

export const MAX_AREA_HA = 2000 // 20 км² — предел ТЗ

export const RUN_STATUS = {
  draft: { color: 'default', label: 'Черновик' },
  validating: { color: 'processing', label: 'Проверка данных' },
  running: { color: 'processing', label: 'Расчёт' },
  complete: { color: 'success', label: 'Готово' },
  partial: { color: 'warning', label: 'Частично' },
  units_unavailable: { color: 'error', label: 'Единицы недоступны' },
  failed: { color: 'error', label: 'Ошибка' }
}

export const TERMINAL_RUN_STATUSES = ['complete', 'partial', 'units_unavailable', 'failed']
export const SUCCESS_RUN_STATUSES = ['complete', 'partial', 'units_unavailable']

export const isRunTerminal = (status) => TERMINAL_RUN_STATUSES.includes(status)
export const isRunSuccess = (status) => SUCCESS_RUN_STATUSES.includes(status)

export const UNIT_STATUS = {
  computed: { color: 'success', label: 'Рассчитаны' },
  available: { color: 'success', label: 'Рассчитаны' },
  zero: { color: 'warning', label: 'Ноль единиц' },
  unavailable: { color: 'error', label: 'Недоступны' }
}

export const CAUSE_STATUS = {
  confirmed: { color: 'error', label: 'Подтверждено' },
  likely: { color: 'warning', label: 'Вероятно' },
  probable: { color: 'warning', label: 'Вероятно' },
  undetermined: { color: 'default', label: 'Не установлено' },
  unknown: { color: 'default', label: 'Не установлено' }
}

export const EVIDENCE_LABELS = {
  gfc: 'GFC',
  sentinel2: 'Sentinel-2',
  modis: 'MODIS',
  cci_change: 'CCI Change'
}

export const BLOCK_REASON_LABELS = {
  None: 'Блокировки нет',
  ZeroArea: 'Расчётная площадь равна нулю',
  NonPositivePeriod: 'Период не положителен',
  MissingData: 'Отсутствуют обязательные данные',
  IncompleteCoverage: 'Неполное покрытие',
  MissingBaseline: 'Нет базовой линии',
  NonPositiveResult: 'R ≤ 0 — результата относительно baseline нет',
  UncertaintyExceedsResult: 'H/R ≥ 1 — неопределённость блокирует единицы'
}

export const METHOD_PROFILES = [
  { value: 'default', label: 'Базовый (SCL 4–5, k = 1)' },
  { value: 'extended_scl', label: 'Расширенная маска (SCL 4–7)' },
  { value: 'k2', label: 'Повышенная неопределённость (k = 2)' }
]

// Ключи слоёв совпадают с Go-шлюзом (GET /analyses/{id}/layers).
// kind: vector — GeoJSON, raster — GeoTIFF (рендерится на клиенте), computed — только в эксперименте.
export const LAYER_DEFINITIONS = [
  { key: 'aoi', label: 'Территория (AOI)', kind: 'vector' },
  { key: 'zones', label: 'Зоны изменений', kind: 'vector' },
  { key: 'agb_start', label: 'AGB на начало', kind: 'raster', ramp: 'biomass' },
  { key: 'agb_end', label: 'AGB на конец', kind: 'raster', ramp: 'biomass' },
  { key: 'gfc', label: 'GFC (потеря покрова)', kind: 'raster', ramp: 'lossYear' },
  { key: 'cci_change', label: 'CCI Change 2019–2020', kind: 'raster', ramp: 'diverging' },
  { key: 'modis_burn', label: 'MODIS (гарь)', kind: 'raster', ramp: 'burn' },
  { key: 'coverage', label: 'Пригодное покрытие (SCL)', kind: 'raster', ramp: 'scl' },
  { key: 'sentinel2_before', label: 'Sentinel-2 до', kind: 'raster', ramp: 'rgb' },
  { key: 'sentinel2_after', label: 'Sentinel-2 после', kind: 'raster', ramp: 'rgb' },
  { key: 'ndvi', label: 'NDVI (вычисляемый)', kind: 'computed' },
  { key: 'nbr', label: 'NBR (вычисляемый)', kind: 'computed' }
]

export const LAYER_LABELS = Object.fromEntries(
  LAYER_DEFINITIONS.map((layer) => [layer.key, layer.label])
)

// Слои, которые реально запрашиваются у backend как GeoTIFF.
export const RASTER_LAYER_KEYS = LAYER_DEFINITIONS.filter((l) => l.kind === 'raster').map(
  (l) => l.key
)

// Роли участков из ТЗ: контроль, потери, пожар, ранняя потеря.
export const ROLE_META = [
  { key: 'control', color: 'green', label: 'Контрольный участок', match: ['контроль'] },
  { key: 'fire', color: 'red', label: 'Пожар', match: ['пожар'] },
  { key: 'early', color: 'geekblue', label: 'Ранняя потеря', match: ['ранн'] },
  { key: 'loss', color: 'orange', label: 'Потери покрова', match: ['потер'] }
]

export const roleMeta = (role) => {
  const value = String(role ?? '').toLowerCase()
  return ROLE_META.find((meta) => meta.match.some((token) => value.includes(token))) ?? {
    key: 'other',
    color: 'blue',
    label: role ?? 'Участок'
  }
}

export const REPORT_FORMATS = [
  { value: 'html', label: 'HTML', contentType: 'text/html' },
  { value: 'pdf', label: 'PDF', contentType: 'application/pdf' },
  { value: 'json', label: 'JSON', contentType: 'application/json' }
]

export const DISCLAIMERS = {
  units: 'Потенциальные единицы по условиям кейса, не сертифицированы.',
  emission:
    'Положительный Eproj означает потерю углерода из учитываемого пула, но не доказывает немедленный выброс в атмосферу.',
  uncertainty:
    'Диапазон L–U имеет сценарный статус и не является статистическим доверительным интервалом.',
  cause:
    'GFC подтверждает факт и год потери покрова, но не причину. MODIS — сигнал возможной гари, а не доказательство пожара.',
  pool: 'Учитывается только живая надземная древесная биомасса: корни, мёртвая древесина, подстилка, почва и продукция не входят в основной расчёт.',
  validation: 'Сравнение спутниковых продуктов не является наземной валидацией.'
}

export const formatNumber = (value, digits = 2) => {
  if (value === null || value === undefined || Number.isNaN(value)) return '—'
  return new Intl.NumberFormat('ru-RU', {
    minimumFractionDigits: digits,
    maximumFractionDigits: digits
  }).format(value)
}

export const formatUnits = (value) => {
  if (value === null || value === undefined) return 'недоступно'
  return new Intl.NumberFormat('ru-RU').format(value)
}

export const formatMoney = (value) => {
  if (value === null || value === undefined || Number.isNaN(value)) return '—'
  return new Intl.NumberFormat('ru-RU', {
    style: 'currency',
    currency: 'RUB',
    maximumFractionDigits: 0
  }).format(value)
}

export const formatPercent = (value, digits = 1) => {
  if (value === null || value === undefined || Number.isNaN(value)) return '—'
  return `${formatNumber(value * 100, digits)} %`
}

export const formatDate = (value) => {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleString('ru-RU')
}
