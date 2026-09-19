import areasGeoJson from '../data/areas.json'

export const AREAS = areasGeoJson.features.map((feature) => ({
  ...feature.properties,
  bbox: [
    feature.properties.bbox_west,
    feature.properties.bbox_south,
    feature.properties.bbox_east,
    feature.properties.bbox_north
  ],
  geometry: feature.geometry
}))

export const AREAS_GEOJSON = areasGeoJson

export const SOURCES = [
  {
    source_id: 'CCI_V7',
    product: 'ESA CCI Biomass — биомасса, неопределённость и изменение',
    version: '7.0',
    kind: 'внешняя модельная оценка',
    doi: '10.5285/6429d1aafe1e43b9b414e4a5a7f8b903',
    license_url: 'https://artefacts.ceda.ac.uk/licences/specific_licences/esacci_biomass_terms_and_conditions_v2.pdf',
    limitations: 'Годовые модельные оценки. Возможны зависимые пространственные и временные ошибки.'
  },
  {
    source_id: 'S2_L2A',
    product: 'Copernicus Sentinel-2 L2A через каталог Earth Search',
    version: 'по сценам (scenes.csv)',
    kind: 'спутниковое наблюдение отражения поверхности',
    doi: '',
    license_url: 'https://sentinels.copernicus.eu/documents/247904/690755/Sentinel_Data_Legal_Notice',
    limitations: 'Остаточные эффекты облаков и теней. SCL — автоматическая классификация.'
  },
  {
    source_id: 'GFC_2025_V113',
    product: 'Hansen Global Forest Change — изменения древесного покрова',
    version: '2025 v1.13',
    kind: 'внешний продукт изменений',
    doi: '10.1126/science.1244693',
    license_url: 'https://creativecommons.org/licenses/by/4.0/',
    limitations: 'Причина потери и точное изменение биомассы продуктом не определяются.'
  },
  {
    source_id: 'MODIS_MCD64A1_061',
    product: 'MODIS MCD64A1 — месячный продукт гарей',
    version: '6.1',
    kind: 'внешний продукт гарей',
    doi: '10.5067/MODIS/MCD64A1.061',
    license_url: 'https://developers.google.com/earth-engine/datasets/catalog/MODIS_061_MCD64A1',
    limitations: 'Шаг сетки около 463 м. Положение и дата горения имеют погрешность.'
  },
  {
    source_id: 'IPCC_FOREST_2006',
    product: 'Руководящие принципы МГЭИК 2006 — лесные земли',
    version: 'Том 4, глава 4, таблица 4.3',
    kind: 'справочный коэффициент',
    doi: '',
    license_url: 'https://www.ipcc.ch/copyright/',
    limitations: 'Общее значение CF = 0,47 для лесной биомассы.'
  },
  {
    source_id: 'CASE_RULES_V1',
    product: 'Правила углеродного расчёта для кейса SR Data',
    version: '1.0',
    kind: 'сценарные правила кейса',
    doi: '',
    license_url: '',
    limitations: 'Базовая линия и параметры — условия сценарного расчёта, не сертификация.'
  }
]

const AOI_PROFILES = {
  RU_TVER_01: {
    carbon: [85.13, 85.47, 85.81, 86.16, 86.5, 86.85],
    standardDeviation: 0.3,
    baselineEmission: -11050,
    coverage: 1,
    cciChange: -0.8,
    warnings: ['Контрольный участок: выраженного нарушения не выявлено.'],
    zones: []
  },
  RU_VOLOGDA_02: {
    carbon: [95.1, 94.2, 93.4, 92.6, 91.8, 91.1],
    standardDeviation: 0.42,
    baselineEmission: -10084,
    coverage: 1,
    cciChange: -3.4,
    warnings: [
      'GFC подтверждает факт и год потери покрова, но не причину.',
      'Часть потерь имеет неустановленную причину.'
    ],
    zones: [
      {
        areaHectares: 34.5,
        contributionToDeltaCarbon: -260,
        fractions: [0.15, 0.55, 0.4, 0.9],
        evidence: ['gfc'],
        cause: 'undetermined'
      },
      {
        areaHectares: 12.1,
        contributionToDeltaCarbon: -95,
        fractions: [0.55, 0.2, 0.8, 0.5],
        evidence: ['gfc', 'sentinel2'],
        cause: 'likely'
      }
    ]
  },
  RU_MORDOVIA_03: {
    carbon: [88.0, 86.5, 79.0, 78.2, 77.6, 77.0],
    standardDeviation: 0.55,
    baselineEmission: -5200,
    coverage: 0.97,
    cciChange: -5.2,
    warnings: [
      'Неполное обязательное покрытие: часть пикселей исключена.',
      'Пожарный сигнал MODIS не учитывается повторно в запасе углерода.'
    ],
    zones: [
      {
        areaHectares: 210.4,
        contributionToDeltaCarbon: -1840,
        fractions: [0.1, 0.1, 0.75, 0.85],
        evidence: ['modis', 'sentinel2', 'gfc'],
        cause: 'confirmed'
      },
      {
        areaHectares: 48.2,
        contributionToDeltaCarbon: -410,
        fractions: [0.15, 0.55, 0.45, 0.95],
        evidence: ['gfc'],
        cause: 'undetermined'
      },
      {
        areaHectares: 22.7,
        contributionToDeltaCarbon: -180,
        fractions: [0.55, 0.15, 0.9, 0.4],
        evidence: ['modis'],
        cause: 'likely'
      }
    ]
  },
  RU_MORDOVIA_04: {
    carbon: [80.0, 81.2, 82.6, 85.1, 87.8, 90.2],
    standardDeviation: 0.5,
    baselineEmission: -10000,
    coverage: 1,
    cciChange: 1.1,
    warnings: ['Сценарий ранней потери и последующего восстановления.'],
    zones: [
      {
        areaHectares: 61.3,
        contributionToDeltaCarbon: -520,
        fractions: [0.2, 0.6, 0.5, 0.95],
        evidence: ['gfc', 'sentinel2'],
        cause: 'likely'
      },
      {
        areaHectares: 18.9,
        contributionToDeltaCarbon: -160,
        fractions: [0.6, 0.2, 0.9, 0.55],
        evidence: ['modis'],
        cause: 'likely'
      }
    ]
  }
}

const DEFAULT_PROFILE = AOI_PROFILES.RU_TVER_01

const CO2_PER_CARBON = 44 / 12
const CARBON_FRACTION = 0.47
const UNCERTAINTY_THRESHOLD = 0.1
const RESERVE_FRACTION = 0.15
const PRICE_SCENARIOS = [500, 1500, 4000]

const stableId = (aoiId, startYear, endYear, k) => {
  const seed = `${aoiId}|${startYear}|${endYear}|${k}`
  let hash = 0
  for (let index = 0; index < seed.length; index += 1) {
    hash = (hash * 31 + seed.charCodeAt(index)) >>> 0
  }
  return hash.toString(16).padStart(8, '0').repeat(4).slice(0, 32)
}

const zoneGeometry = (bbox, fractions) => {
  const [west, south, east, north] = bbox
  const [fx0, fy0, fx1, fy1] = fractions
  const x0 = west + (east - west) * fx0
  const x1 = west + (east - west) * fx1
  const y0 = south + (north - south) * fy0
  const y1 = south + (north - south) * fy1
  return {
    type: 'Polygon',
    coordinates: [
      [
        [x0, y0],
        [x1, y0],
        [x1, y1],
        [x0, y1],
        [x0, y0]
      ]
    ]
  }
}

const buildUnits = ({ areaHectares, baselineEmission, projectEmission, halfWidth }) => {
  const result = baselineEmission - projectEmission

  if (result <= 0) {
    return {
      status: 'zero',
      reason: 'NonPositiveResult',
      resultRelativeToBaseline: result,
      uncertaintyDeduction: 0,
      adjustedResult: 0,
      reserve: 0,
      units: 0,
      priceScenarios: PRICE_SCENARIOS.map((price) => ({ pricePerUnit: price, value: 0 }))
    }
  }

  const ratio = halfWidth / result
  if (ratio >= 1) {
    return {
      status: 'zero',
      reason: 'UncertaintyExceedsResult',
      resultRelativeToBaseline: result,
      uncertaintyDeduction: 0,
      adjustedResult: 0,
      reserve: 0,
      units: 0,
      priceScenarios: PRICE_SCENARIOS.map((price) => ({ pricePerUnit: price, value: 0 }))
    }
  }

  const deduction = Math.min(1, Math.max(0, ratio - UNCERTAINTY_THRESHOLD))
  const adjusted = result * (1 - deduction)
  const reserve = adjusted * RESERVE_FRACTION
  const units = Math.floor(adjusted * (1 - RESERVE_FRACTION))

  return {
    status: 'computed',
    reason: 'None',
    resultRelativeToBaseline: result,
    uncertaintyDeduction: deduction,
    adjustedResult: adjusted,
    reserve,
    units,
    priceScenarios: PRICE_SCENARIOS.map((price) => ({ pricePerUnit: price, value: units * price }))
  }
}

export const buildSummary = ({ aoiId, startYear, endYear, sensitivityCoefficient = 1, geometry, polygonAreaHectares }) => {
  const profile = AOI_PROFILES[aoiId] ?? DEFAULT_PROFILE
  const area = AREAS.find((item) => item.aoi_id === aoiId)
  const areaHectares = polygonAreaHectares ?? area?.area_ha ?? 1600
  const bbox = area?.bbox ?? [40.66, 59.43, 40.73, 59.47]

  const yearCount = endYear - startYear + 1
  const carbonWindow = profile.carbon.slice(startYear - 2019, startYear - 2019 + yearCount)

  const yearlySeries = carbonWindow.map((meanCarbonPerHectare, index) => {
    const year = startYear + index
    const coverage = profile.coverage
    return {
      year,
      areaHectares: areaHectares * coverage,
      totalCarbon: meanCarbonPerHectare * areaHectares * coverage,
      meanCarbonPerHectare,
      coverage,
      hasCompleteCoverage: coverage >= 1
    }
  })

  const startCarbon = yearlySeries[0].totalCarbon
  const endCarbon = yearlySeries[yearlySeries.length - 1].totalCarbon
  const deltaCarbon = endCarbon - startCarbon
  const durationYears = endYear - startYear
  const projectEmission = -deltaCarbon * CO2_PER_CARBON
  const emissionPerHectarePerYear =
    areaHectares > 0 ? projectEmission / (areaHectares * durationYears) : 0

  const standardDeviationEffect =
    profile.standardDeviation * areaHectares * CARBON_FRACTION * CO2_PER_CARBON * 2
  const halfWidth = standardDeviationEffect * sensitivityCoefficient
  const lower = projectEmission - halfWidth
  const upper = projectEmission + halfWidth

  const units =
    profile.coverage < 1
      ? {
          status: 'unavailable',
          reason: 'IncompleteCoverage',
          resultRelativeToBaseline: 0,
          uncertaintyDeduction: 0,
          adjustedResult: 0,
          reserve: 0,
          units: null,
          priceScenarios: []
        }
      : buildUnits({
          areaHectares,
          baselineEmission: profile.baselineEmission,
          projectEmission,
          halfWidth
        })

  const changeZones = profile.zones.map((zone, index) => ({
    id: index + 1,
    areaHectares: zone.areaHectares,
    contributionToDeltaCarbon: zone.contributionToDeltaCarbon,
    period: `${startYear}–${endYear}`,
    evidenceTypes: zone.evidence,
    causeStatus: zone.cause,
    geometry: zoneGeometry(bbox, zone.fractions)
  }))

  const status =
    units.status === 'unavailable' ? 'units_unavailable' : profile.warnings.length > 0 ? 'partial' : 'complete'

  return {
    runId: stableId(aoiId ?? 'CUSTOM', startYear, endYear, sensitivityCoefficient),
    methodVersion: '1.0',
    dataVersion: 'CCI-Biomass-v7.0',
    createdAt: new Date().toISOString(),
    status,
    aoiId: aoiId ?? 'CUSTOM',
    startYear,
    endYear,
    polygonAreaHectares: areaHectares,
    geometry: geometry ?? area?.geometry ?? null,
    yearlySeries,
    change: {
      deltaCarbon,
      projectEmission,
      emissionPerHectarePerYear
    },
    uncertainty: { lower, upper, halfWidth },
    baseline: { baselineEmission: profile.baselineEmission },
    units,
    changeZones,
    changeZoneEvidence: changeZones.map((zone) => ({
      zoneId: zone.id,
      evidenceTypes: zone.evidenceTypes,
      causeStatus: zone.causeStatus
    })),
    cciChangeMeanTonnesPerHectare: profile.cciChange,
    warnings: profile.warnings
  }
}

export const buildSensitivity = ({ aoiId, startYear, endYear }) => {
  const k1 = buildSummary({ aoiId, startYear, endYear, sensitivityCoefficient: 1 })
  const k2 = buildSummary({ aoiId, startYear, endYear, sensitivityCoefficient: 2 })

  return {
    aoiId,
    startYear,
    endYear,
    k1: {
      lower: k1.uncertainty.lower,
      upper: k1.uncertainty.upper,
      halfWidth: k1.uncertainty.halfWidth,
      units: k1.units.units
    },
    k2: {
      lower: k2.uncertainty.lower,
      upper: k2.uncertainty.upper,
      halfWidth: k2.uncertainty.halfWidth,
      units: k2.units.units
    },
    sclStrictValidPixels: 184320,
    sclExtendedValidPixels: 201744
  }
}
