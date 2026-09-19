// Публичный клиент ForestProof. Фронтенд всегда работает через Go-шлюз
// (/api/v1), который проксируется Vite на http://localhost:8080.
import { http } from './http'
import {
  mapAreas,
  mapChangesToZones,
  mapLayers,
  mapProject,
  mapSensitivity,
  mapSources,
  mapStatus,
  mapSummary
} from './mappers'

const query = (params) =>
  Object.entries(params)
    .filter(([, value]) => value !== undefined && value !== null && value !== '')
    .map(([key, value]) => `${key}=${encodeURIComponent(value)}`)
    .join('&')

export const api = {
  isMock: false,

  // --- реестр ---
  async listAreas() {
    return mapAreas(await http.get('/areas'))
  },

  async listProjects() {
    const raw = await http.get('/projects')
    return Array.isArray(raw) ? raw.map(mapProject) : []
  },

  async listSources() {
    return mapSources(await http.get('/sources'))
  },

  // --- жизненный цикл анализа ---
  async createAnalysis({ aoiId, polygonGeoJson, startYear, endYear, methodProfile }) {
    const raw = await http.post('/analyses', {
      aoiId: aoiId || undefined,
      polygonGeoJson: polygonGeoJson || undefined,
      startYear,
      endYear,
      methodProfile: methodProfile || undefined
    })
    return {
      id: raw.id,
      status: raw.status,
      aoiId: raw.aoiId ?? '',
      startYear: raw.startYear,
      endYear: raw.endYear,
      createdAt: raw.createdAt,
      inputHash: raw.inputHash,
      idempotent: Boolean(raw.idempotent)
    }
  },

  async getStatus(id) {
    return mapStatus(await http.get(`/analyses/${id}/status`))
  },

  async getSummary(id) {
    return mapSummary(await http.get(`/analyses/${id}/summary`))
  },

  async getChanges(id) {
    return mapChangesToZones(await http.get(`/analyses/${id}/changes`))
  },

  async getLayers(id) {
    return mapLayers(await http.get(`/analyses/${id}/layers`))
  },

  layerUrl(id, key) {
    return http.url(`/analyses/${id}/layers/${key}`)
  },

  async getLayerGeoJson(id, key) {
    return http.get(`/analyses/${id}/layers/${key}`)
  },

  async getLayerArrayBuffer(id, key) {
    return http.getArrayBuffer(`/analyses/${id}/layers/${key}`)
  },

  // --- эксперимент ---
  async getSensitivity({ aoiId, startYear, endYear }) {
    return mapSensitivity(
      await http.get(`/experiments/sensitivity?${query({ aoiId, startYear, endYear })}`)
    )
  },

  // --- отчёты и экспорт ---
  async getReport(id, format) {
    return http.postBlob(`/analyses/${id}/reports?format=${format}`)
  },

  async getReportText(id, format) {
    return http.postText(`/analyses/${id}/reports?format=${format}`)
  },

  async getYearlyCsv(id) {
    return http.getBlob(`/analyses/${id}/yearly.csv`)
  },

  async checkHealth() {
    const response = await fetch('/healthz')
    return response.ok
  }
}
