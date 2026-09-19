import { Layout, Menu, Tag, Typography } from 'antd'
import {
  EnvironmentOutlined,
  ExperimentOutlined,
  FundOutlined,
  PlusCircleOutlined,
  ProjectOutlined
} from '@ant-design/icons'
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
  const selectedKey = items.find((item) => item.key !== '/' && location.pathname.startsWith(item.key))?.key ?? '/'

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ display: 'flex', alignItems: 'center', gap: 24 }}>
        <FundOutlined style={{ fontSize: 22, color: '#a5d6a7' }} />
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
        {api.isMock && <Tag color="gold">мок-данные</Tag>}
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
