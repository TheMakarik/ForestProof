// Тонкая обёртка над fetch для публичного API Go-шлюза.
// Разбирает единый конверт ошибок {"error":{"code","message"}} и умеет
// возвращать json / text / blob / arrayBuffer.

export const API_BASE = import.meta.env.VITE_API_BASE ?? '/api/v1'

export class ApiError extends Error {
  constructor(message, { status, code } = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
  }
}

export const apiUrl = (path) => `${API_BASE}${path.startsWith('/') ? path : `/${path}`}`

const parseError = async (response) => {
  let code
  let message = `Ошибка запроса (${response.status})`
  try {
    const body = await response.json()
    // Go-шлюз: { error: { code, message } }; C#: { message }.
    if (body?.error?.message) {
      message = body.error.message
      code = body.error.code
    } else if (body?.message) {
      message = body.message
    }
  } catch {
    // тело не JSON
  }
  return new ApiError(message, { status: response.status, code })
}

const doFetch = async (path, { method = 'GET', body, signal, headers } = {}) => {
  const response = await fetch(apiUrl(path), {
    method,
    signal,
    headers: {
      Accept: 'application/json',
      ...(body !== undefined ? { 'Content-Type': 'application/json' } : {}),
      ...headers
    },
    body: body !== undefined ? JSON.stringify(body) : undefined
  })
  if (!response.ok) throw await parseError(response)
  return response
}

export const http = {
  url: apiUrl,

  async get(path, options) {
    const response = await doFetch(path, { ...options, method: 'GET' })
    return response.json()
  },

  async post(path, body, options) {
    const response = await doFetch(path, { ...options, method: 'POST', body })
    return response.json()
  },

  async getText(path, options) {
    const response = await doFetch(path, { ...options, method: 'GET' })
    return response.text()
  },

  async getBlob(path, options) {
    const response = await doFetch(path, { ...options, method: 'GET' })
    return response.blob()
  },

  async getArrayBuffer(path, options) {
    const response = await doFetch(path, { ...options, method: 'GET' })
    return response.arrayBuffer()
  },

  async postBlob(path, body, options) {
    const response = await doFetch(path, { ...options, method: 'POST', body })
    return response.blob()
  },

  async postText(path, body, options) {
    const response = await doFetch(path, { ...options, method: 'POST', body })
    return response.text()
  }
}
