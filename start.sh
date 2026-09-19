#!/usr/bin/env bash
#
# ForestProof — установка зависимостей и запуск проекта в Docker.
#
# Поддерживает:
#   - Linux:  apt / dnf / yum / zypper / pacman (официальный get.docker.com)
#   - macOS:  Homebrew + Docker Desktop
#
# Использование:
#   ./start.sh              # установить всё нужное и поднять проект
#   ./start.sh logs -f      # логи
#   ./start.sh ps           # статус контейнеров
#   ./start.sh down         # остановить
#   ./start.sh down -v      # остановить и удалить БД
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

GREEN='\033[0;32m'
YELLOW='\033[0;33m'
RED='\033[0;31m'
NC='\033[0m'

log()  { printf "${GREEN}[ForestProof]${NC} %s\n" "$*"; }
warn() { printf "${YELLOW}[ForestProof]${NC} %s\n" "$*" >&2; }
die()  { printf "${RED}[ForestProof]${NC} %s\n" "$*" >&2; exit 1; }

need_cmd() { command -v "$1" >/dev/null 2>&1; }

run_root() {
  if [ "$(id -u)" -eq 0 ]; then
    "$@"
  else
    sudo "$@"
  fi
}

wait_for_docker() {
  local attempts="${1:-90}"
  local i
  for ((i = 1; i <= attempts; i++)); do
    if docker info >/dev/null 2>&1; then
      return 0
    fi
    sleep 2
  done
  return 1
}

install_docker_macos() {
  if ! need_cmd brew; then
    log "Homebrew не найден — устанавливаю Homebrew..."
    NONINTERACTIVE=1 /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
    if [ -x /opt/homebrew/bin/brew ]; then
      eval "$(/opt/homebrew/bin/brew shellenv)"
    elif [ -x /usr/local/bin/brew ]; then
      eval "$(/usr/local/bin/brew shellenv)"
    fi
  fi

  log "Устанавливаю Docker Desktop через Homebrew..."
  brew install --cask docker

  log "Запускаю Docker Desktop..."
  open -a Docker >/dev/null 2>&1 || true
}

install_docker_linux() {
  local pkg=""
  local candidate
  for candidate in apt-get dnf yum zypper pacman; do
    if need_cmd "$candidate"; then
      pkg="$candidate"
      break
    fi
  done
  [ -n "$pkg" ] || die "Не найден менеджер пакетов (apt/dnf/yum/zypper/pacman)."

  if ! need_cmd curl; then
    log "Устанавливаю curl..."
    case "$pkg" in
      apt-get) run_root apt-get update && run_root apt-get install -y curl ;;
      dnf|yum) run_root "$pkg" install -y curl ;;
      zypper)  run_root zypper --non-interactive install curl ;;
      pacman)  run_root pacman -Sy --noconfirm curl ;;
    esac
  fi

  log "Устанавливаю Docker Engine (get.docker.com)..."
  local tmp
  tmp="$(mktemp)"
  curl -fsSL https://get.docker.com -o "$tmp"

  if ! run_root sh "$tmp"; then
    warn "Официальный скрипт не сработал — пробую пакетный менеджер..."
    set +e
    case "$pkg" in
      apt-get) run_root apt-get update && run_root apt-get install -y docker.io docker-compose-v2 ;;
      dnf|yum) run_root "$pkg" install -y moby-engine docker-compose ;;
      zypper)  run_root zypper --non-interactive install docker docker-compose ;;
      pacman)  run_root pacman -Sy --noconfirm docker docker-compose ;;
    esac
    set -e
  fi
  rm -f "$tmp"

  if need_cmd systemctl; then
    run_root systemctl enable --now docker || warn "Не удалось запустить systemd-сервис docker."
  fi

  if [ "$(id -u)" -ne 0 ]; then
    run_root usermod -aG docker "$USER" || true
  fi
}

main() {
  if ! need_cmd docker; then
    log "Docker не найден — устанавливаю..."
    case "$(uname -s)" in
      Darwin) install_docker_macos ;;
      Linux)  install_docker_linux ;;
      *)      die "Неподдерживаемая ОС: $(uname -s). Установите Docker вручную." ;;
    esac
  else
    log "Docker уже установлен: $(docker --version 2>/dev/null || echo 'версия неизвестна')"
  fi

  if ! need_cmd docker; then
    die "Docker не установился автоматически. Установите Docker вручную и запустите скрипт снова."
  fi

  if [ "$(uname -s)" = "Darwin" ]; then
    open -a Docker >/dev/null 2>&1 || true
  fi

  local -a docker_cmd
  if docker info >/dev/null 2>&1; then
    docker_cmd=(docker)
  elif sudo docker info >/dev/null 2>&1; then
    warn "Пользователь $USER не в группе docker — использую sudo. Выйдите и войдите, чтобы убрать sudo."
    docker_cmd=(sudo docker)
  else
    log "Ожидаю готовности Docker..."
    if wait_for_docker 90; then
      docker_cmd=(docker)
    elif sudo docker info >/dev/null 2>&1; then
      docker_cmd=(sudo docker)
    else
      die "Docker не запустился. Linux: sudo systemctl start docker; macOS: откройте Docker Desktop."
    fi
  fi

  if ! "${docker_cmd[@]}" compose version >/dev/null 2>&1; then
    die "Не найден плагин 'docker compose'. Установите docker-compose-plugin."
  fi

  if [ "$#" -gt 0 ]; then
    log "docker compose $*"
    exec "${docker_cmd[@]}" compose "$@"
  fi

  log "Собираю и запускаю ForestProof..."
  "${docker_cmd[@]}" compose up --build -d
  "${docker_cmd[@]}" compose ps

  cat <<'EOF'

ForestProof запущен:
  Frontend:  http://localhost:5173
  Gateway:   http://localhost:5300/healthz
  PostGIS:   localhost:5432 (forestproof/forestproof)

Полезные команды:
  ./start.sh logs -f   — логи
  ./start.sh ps        — статус
  ./start.sh down      — остановить
  ./start.sh down -v   — остановить и удалить БД
EOF
}

main "$@"
