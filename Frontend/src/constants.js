export const PRICE_SCENARIOS_RUB = [500, 1500, 4000]

export const MIN_YEAR = 2019
export const MAX_YEAR = 2024
export const YEARS = Array.from(
  { length: MAX_YEAR - MIN_YEAR + 1 },
  (_, index) => MIN_YEAR + index
)

export const RUN_STATUS = {
  draft: { color: 'default', label: 'Черновик' },
  validating: { color: 'processing', label: 'Проверка данных' },
  running: { color: 'processing', label: 'Расчёт' },
  complete: { color: 'success', label: 'Готово' },
  partial: { color: 'warning', label: 'Частично' },
  units_unavailable: { color: 'error', label: 'Единицы недоступны' },
  failed: { color: 'error', label: 'Ошибка' }
}

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

export const LAYER_DEFINITIONS = [
  { key: 'aoi', label: 'Территория (AOI)' },
  { key: 'zones', label: 'Зоны изменений' },
  { key: 'gfc', label: 'GFC (потеря покрова)' },
  { key: 'modis', label: 'MODIS (гарь)' },
  { key: 'coverage', label: 'Пригодное покрытие' }
]

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

export const formatMoney = (value) =>
  new Intl.NumberFormat('ru-RU', { style: 'currency', currency: 'RUB', maximumFractionDigits: 0 }).format(value)
