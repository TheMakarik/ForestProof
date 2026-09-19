import React from 'react'
import { Button, Result } from 'antd'

export default class ErrorBoundary extends React.Component {
  state = { error: null }

  static getDerivedStateFromError(error) {
    return { error }
  }

  componentDidCatch(error, info) {
    console.error('Ошибка рендеринга ForestProof:', error, info)
  }

  render() {
    const { error } = this.state

    if (!error) {
      return this.props.children
    }

    try {
      return (
        <Result
          status="error"
          title="Что-то пошло не так"
          subTitle={error.message || 'Непредвиденная ошибка при отображении страницы.'}
          extra={
            <Button type="primary" onClick={() => window.location.reload()}>
              Перезагрузить
            </Button>
          }
        />
      )
    } catch {
      return (
        <div style={{ padding: 24 }}>
          <h2>Что-то пошло не так</h2>
          <p>{error.message || 'Непредвиденная ошибка при отображении страницы.'}</p>
          <button type="button" onClick={() => window.location.reload()}>
            Перезагрузить
          </button>
        </div>
      )
    }
  }
}
