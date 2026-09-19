import { useQuery } from '@tanstack/react-query'
import { api } from '../api/client'
import { isRunSuccess, isRunTerminal } from '../constants'

const POLL_INTERVAL = 1500

export const useRunStatus = (runId) =>
  useQuery({
    queryKey: ['run-status', runId],
    queryFn: () => api.getStatus(runId),
    enabled: Boolean(runId),
    refetchInterval: (query) => {
      const status = query.state.data?.status
      if (!status) return POLL_INTERVAL
      return isRunTerminal(status) ? false : POLL_INTERVAL
    }
  })

// Полный жизненный цикл запуска: статус -> summary / changes / layers.
export const useAnalysisRun = (runId) => {
  const statusQuery = useRunStatus(runId)
  const status = statusQuery.data
  const ready = isRunSuccess(status?.status)
  const failed = status?.status === 'failed'
  const enabled = Boolean(runId) && ready

  const summaryQuery = useQuery({
    queryKey: ['run-summary', runId],
    queryFn: () => api.getSummary(runId),
    enabled
  })

  const changesQuery = useQuery({
    queryKey: ['run-changes', runId],
    queryFn: () => api.getChanges(runId),
    enabled
  })

  const layersQuery = useQuery({
    queryKey: ['run-layers', runId],
    queryFn: () => api.getLayers(runId),
    enabled
  })

  return {
    status,
    statusQuery,
    summary: summaryQuery.data ?? null,
    zones: changesQuery.data ?? [],
    layers: layersQuery.data?.layers ?? [],
    ready,
    failed,
    pending: Boolean(runId) && !status,
    loading: enabled && (summaryQuery.isLoading || changesQuery.isLoading),
    isError: statusQuery.isError || summaryQuery.isError,
    error: statusQuery.error ?? summaryQuery.error ?? null,
    progress: status?.progress ?? 0,
    phase: status?.phase ?? '',
    warnings: status?.warnings ?? [],
    refetch: () => {
      statusQuery.refetch()
      summaryQuery.refetch()
      changesQuery.refetch()
      layersQuery.refetch()
    }
  }
}
