// Контекст запуска анализа, который не возвращает summary:
// исходный полигон произвольного контура, название проекта, заявленный
// результат. Хранится в sessionStorage по id задания.

const KEY = (runId) => `forestproof.run.${runId}`

export const saveRunContext = (runId, context) => {
  if (!runId) return
  try {
    sessionStorage.setItem(KEY(runId), JSON.stringify(context))
  } catch {
    // sessionStorage может быть недоступен (private mode)
  }
}

export const getRunContext = (runId) => {
  if (!runId) return null
  try {
    const raw = sessionStorage.getItem(KEY(runId))
    return raw ? JSON.parse(raw) : null
  } catch {
    return null
  }
}

export const removeRunContext = (runId) => {
  try {
    sessionStorage.removeItem(KEY(runId))
  } catch {
    // ignore
  }
}
