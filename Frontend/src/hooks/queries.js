import { useQuery } from '@tanstack/react-query'
import { api } from '../api/client'

export const useAreas = () =>
  useQuery({ queryKey: ['areas'], queryFn: api.listAreas, staleTime: 5 * 60_000 })

export const useProjects = () =>
  useQuery({ queryKey: ['projects'], queryFn: api.listProjects, staleTime: 60_000 })

export const useSources = () =>
  useQuery({ queryKey: ['sources'], queryFn: api.listSources, staleTime: 5 * 60_000 })

export const useSensitivity = ({ aoiId, startYear, endYear }, options = {}) =>
  useQuery({
    queryKey: ['sensitivity', aoiId, startYear, endYear],
    queryFn: () => api.getSensitivity({ aoiId, startYear, endYear }),
    enabled: Boolean(aoiId),
    ...options
  })
