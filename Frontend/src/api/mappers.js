// Нормализация ответов backend в единую UI-модель.
// Backend отдаёт camelCase (C#/Go), но поля /areas и /sources частично
// snake_case — приводим всё к camelCase.

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

const lower = (value) => String(value ?? '').toLowerCase()

export const mapRunStatus = (value) => RUN_STATUS_MAP[value] ?? lower(value)
export const mapUnitStatus = (value) => UNIT_STATUS_MAP[value] ?? lower(value)

export const mapArea = (raw) => {
  const aoiId = raw.aoiId ?? raw.aoi_id ?? raw.id
  const bbox = raw.bbox ?? [
    raw.bbox_west ?? raw.bboxWest,
    raw.bbox_south ?? raw.bboxSouth,
    raw.bbox_east ?? raw.bboxEast,
    raw.bbox_north ?? raw.bboxNorth
  ].map((value) => (value === undefined || value === null ? undefined : Number(value)))

  return {
    aoiId,
    id: aoiId,
    name: raw.name ?? aoiId,
    region: raw.region ?? null,
    areaHa: Number(raw.area_ha ?? raw.areaHa ?? 0),
    selectionRole: raw.selection_role ?? raw.selectionRole ?? null,
    projectStatus: raw.project_status ?? raw.projectStatus ?? null,
    baselineId: raw.baseline_id ?? raw.baselineId ?? null,
    startYear: raw.analysis_start_year ?? raw.startYear ?? null,
    endYear: raw.analysis_end_year ?? raw.endYear ?? null,
    bbox: bbox.every((value) => Number.isFinite(value)) ? bbox : null,
    geometry: raw.geometry ?? null
  }
}

export const mapAreas = (raw) => (Array.isArray(raw) ? raw.map(mapArea) : [])

export const mapSource = (raw) => ({
  sourceId: raw.sourceId ?? raw.source_id,
  product: raw.product ?? '',
  version: raw.version ?? '',
  kind: raw.kind ?? '',
  licenseUrl: raw.licenseUrl ?? raw.license_url ?? '',
  requiredAttribution: raw.requiredAttribution ?? raw.required_attribution ?? '',
  limitations: raw.limitations ?? '',
  files: (raw.files ?? []).map((file) => ({
    relativePath: file.relativePath ?? file.relative_path,
    aoiId: file.aoiId ?? file.aoi_id ?? null,
    sha256Recorded: file.sha256Recorded ?? file.sha256_recorded ?? null,
    sha256Actual: file.sha256Actual ?? file.sha256_actual ?? null,
    verified: file.verified ?? 'pending',
    sizeBytes: file.sizeBytes ?? file.size_bytes ?? null
  }))
})

export const mapSources = (raw) => (Array.isArray(raw) ? raw.map(mapSource) : [])

export const mapProject = (raw) => ({
  id: raw.id,
  name: raw.name,
  aoiId: raw.aoiId ?? raw.aoi_id ?? null,
  claimedResult: raw.claimedResult ?? raw.claimed_result ?? null
})

export const mapStatus = (raw) => ({
  id: raw.id,
  status: mapRunStatus(raw.status),
  phase: raw.phase ?? '',
  progress: Number(raw.progress ?? 0),
  errorMessage: raw.errorMessage ?? raw.error_message ?? null,
  warnings: raw.warnings ?? [],
  methodVersion: raw.methodVersion ?? raw.method_version ?? null,
  dataVersion: raw.dataVersion ?? raw.data_version ?? null,
  createdAt: raw.createdAt ?? raw.created_at ?? null,
  updatedAt: raw.updatedAt ?? raw.updated_at ?? null
})

export const mapSummary = (raw) => {
  const units = raw.units ?? {}
  const unitCount = units.units ?? null

  return {
    runId: raw.runId,
    methodVersion: raw.methodVersion,
    dataVersion: raw.dataVersion,
    createdAt: raw.createdAt,
    inputHash: raw.inputHash ?? null,
    status: mapRunStatus(raw.status),
    aoiId: raw.aoiId ?? '',
    startYear: raw.startYear,
    endYear: raw.endYear,
    polygonAreaHectares: Number(raw.polygonAreaHectares ?? 0),
    yearlySeries: (raw.yearlySeries ?? []).map((item) => ({
      year: item.year,
      areaHectares: Number(item.areaHectares ?? 0),
      totalCarbon: Number(item.totalCarbon ?? 0),
      meanCarbonPerHectare: Number(item.meanCarbonPerHectare ?? 0),
      coverage: Number(item.coverage ?? 0),
      hasCompleteCoverage: Number(item.coverage ?? 0) >= 1
    })),
    change: {
      deltaCarbon: Number(raw.change?.deltaCarbon ?? 0),
      projectEmission: Number(raw.change?.projectEmission ?? 0),
      emissionPerHectarePerYear: Number(raw.change?.emissionPerHectarePerYear ?? 0)
    },
    uncertainty: {
      lower: Number(raw.uncertainty?.lower ?? 0),
      upper: Number(raw.uncertainty?.upper ?? 0),
      halfWidth: Number(raw.uncertainty?.halfWidth ?? 0)
    },
    baseline: {
      baselineEmission: Number(raw.baseline?.baselineEmission ?? 0)
    },
    units: {
      status: mapUnitStatus(units.status),
      reason: units.reason ?? 'None',
      resultRelativeToBaseline: Number(units.resultRelativeToBaseline ?? 0),
      uncertaintyDeduction: Number(units.uncertaintyDeduction ?? 0),
      adjustedResult: Number(units.adjustedResult ?? 0),
      reserve: Number(units.reserve ?? 0),
      units: unitCount,
      priceScenarios: (units.priceScenarios ?? []).map((item) => ({
        pricePerUnit: Number(item.pricePerUnit ?? 0),
        value: Number(item.value ?? 0)
      }))
    },
    changeZones: (raw.changeZones ?? []).map((zone) => ({
      id: zone.id,
      areaHectares: Number(zone.areaHectares ?? 0),
      contributionToDeltaCarbon: Number(zone.contributionToDeltaCarbon ?? 0)
    })),
    changeZoneEvidence: (raw.changeZoneEvidence ?? []).map((evidence) => ({
      zoneId: evidence.zoneId,
      evidenceTypes: (evidence.evidenceTypes ?? []).map((type) => EVIDENCE_MAP[type] ?? lower(type)),
      causeStatus: CAUSE_MAP[evidence.causeStatus] ?? lower(evidence.causeStatus)
    })),
    cciChangeMeanTonnesPerHectare:
      raw.cciChangeMeanTonnesPerHectare === null || raw.cciChangeMeanTonnesPerHectare === undefined
        ? null
        : Number(raw.cciChangeMeanTonnesPerHectare),
    warnings: raw.warnings ?? []
  }
}

// /changes -> массив зон, совместимый с таблицами и картой.
export const mapChangesToZones = (geojson) => {
  const features = geojson?.features ?? []
  return features.map((feature) => {
    const properties = feature.properties ?? {}
    return {
      id: properties.id,
      areaHectares: Number(properties.areaHectares ?? 0),
      contributionToDeltaCarbon: Number(properties.contributionToDeltaCarbon ?? 0),
      pixelCount: properties.pixelCount ?? null,
      evidenceTypes: (properties.evidenceTypes ?? []).map((type) => EVIDENCE_MAP[type] ?? lower(type)),
      causeStatus: CAUSE_MAP[properties.causeStatus] ?? 'undetermined',
      interpretation: properties.interpretation ?? null,
      geometry: feature.geometry ?? null
    }
  })
}

export const mapLayers = (raw) => ({
  aoiId: raw?.aoiId ?? '',
  layers: (raw?.layers ?? []).map((layer) => ({
    key: layer.key,
    available: Boolean(layer.available),
    reason: layer.reason ?? null,
    description: layer.description ?? null,
    legend: layer.legend ?? null,
    sourceId: layer.sourceId ?? null,
    licenseUrl: layer.licenseUrl ?? null
  }))
})

export const mapSensitivity = (raw) => ({
  aoiId: raw?.aoiId,
  startYear: raw?.startYear,
  endYear: raw?.endYear,
  k1: raw?.k1 ?? null,
  k2: raw?.k2 ?? null,
  sclStrict: raw?.sclStrict ?? null,
  sclExtended: raw?.sclExtended ?? null,
  sclStrictValidPixels: raw?.sclStrictValidPixels ?? null,
  sclExtendedValidPixels: raw?.sclExtendedValidPixels ?? null,
  cciChangeMean: raw?.cciChangeMean ?? null,
  selfDifferenceMean: raw?.selfDifferenceMean ?? null
})
