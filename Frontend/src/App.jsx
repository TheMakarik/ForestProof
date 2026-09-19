import { Route, Routes } from 'react-router-dom'
import { Result } from 'antd'
import AppLayout from './components/AppLayout'
import DashboardPage from './pages/DashboardPage'
import CreateAnalysisPage from './pages/CreateAnalysisPage'
import AnalysisResultPage from './pages/AnalysisResultPage'
import ExperimentPage from './pages/ExperimentPage'
import MapPage from './pages/MapPage'

export default function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<DashboardPage />} />
        <Route path="new" element={<CreateAnalysisPage />} />
        <Route path="analysis" element={<AnalysisResultPage />} />
        <Route path="map" element={<MapPage />} />
        <Route path="experiment" element={<ExperimentPage />} />
        <Route
          path="*"
          element={<Result status="404" title="404" subTitle="Страница не найдена" />}
        />
      </Route>
    </Routes>
  )
}
