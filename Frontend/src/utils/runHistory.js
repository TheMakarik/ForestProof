// Локальная история проверок для дашборда: backend не отдаёт список
// запусков, поэтому храним краткие записи о созданных анализах.

const KEY = 'forestproof.runs'
const LIMIT = 25

export const listRuns = () => {
  try {
    const raw = localStorage.getItem(KEY)
    return raw ? JSON.parse(raw) : []
  } catch {
    return []
  }
}

export const pushRun = (record) => {
  try {
    const runs = listRuns().filter((item) => item.runId !== record.runId)
    runs.unshift(record)
    localStorage.setItem(KEY, JSON.stringify(runs.slice(0, LIMIT)))
  } catch {
    // ignore
  }
  return listRuns()
}

export const updateRun = (runId, patch) => {
  try {
    const runs = listRuns().map((item) => (item.runId === runId ? { ...item, ...patch } : item))
    localStorage.setItem(KEY, JSON.stringify(runs))
  } catch {
    // ignore
  }
}

export const clearRuns = () => {
  try {
    localStorage.removeItem(KEY)
  } catch {
    // ignore
  }
}

export const averageDurationMs = (runs) => {
  const durations = runs.map((run) => run.durationMs).filter((value) => Number.isFinite(value) && value > 0)
  if (durations.length === 0) return null
  return durations.reduce((sum, value) => sum + value, 0) / durations.length
}
