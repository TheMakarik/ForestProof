import { Layout, Menu, Tag, Typography } from 'antd'
import {
  EnvironmentOutlined,
  ExperimentOutlined,
  PlusCircleOutlined,
  ProjectOutlined
} from '@ant-design/icons'
import { useQuery } from '@tanstack/react-query'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import { api } from '../api/client'

const { Header, Content, Footer } = Layout

const items = [
  { key: '/', icon: <ProjectOutlined />, label: 'Дашборд' },
  { key: '/map', icon: <EnvironmentOutlined />, label: 'Карта' },
  { key: '/new', icon: <PlusCircleOutlined />, label: 'Новая проверка' },
  { key: '/experiment', icon: <ExperimentOutlined />, label: 'Исследование' }
]

export default function AppLayout() {
  const navigate = useNavigate()
  const location = useLocation()

  const selectedKey =
    location.pathname === '/'
      ? '/'
      : items.find((item) => item.key !== '/' && location.pathname.startsWith(item.key))?.key ?? '/'

  const { data: apiAvailable } = useQuery({
    queryKey: ['health'],
    queryFn: async () => {
      try {
        return await api.checkHealth()
      } catch {
        return false
      }
    },
    refetchInterval: 30000,
    retry: false
  })

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ display: 'flex', alignItems: 'center', gap: 24 }}>
        <img
          src="/logo.png"
          alt="ForestProof"
          style={{ width: 36, height: 36, borderRadius: 8, objectFit: 'cover' }}
        />
        <Typography.Title level={4} style={{ color: '#fff', margin: 0, whiteSpace: 'nowrap' }}>
          ForestProof
        </Typography.Title>
        <Menu
          theme="dark"
          mode="horizontal"
          selectedKeys={[selectedKey]}
          items={items}
          onClick={({ key }) => navigate(key)}
          style={{ flex: 1, minWidth: 0 }}
        />
        <Tag color={apiAvailable ? 'green' : 'red'} style={{ marginInlineEnd: 0 }}>
          {apiAvailable ? 'API доступен' : 'API недоступен'}
        </Tag>
      </Header>
      <Content style={{ padding: '24px', maxWidth: 1400, width: '100%', margin: '0 auto' }}>
        <Outlet />
      </Content>
      <Footer style={{ textAlign: 'center', color: '#7b8794' }}>
        ForestProof — потенциальные, не сертифицированные единицы. Решение использует открытые данные.
      </Footer>
    </Layout>
  )
}
