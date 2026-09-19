# ТЗ на доработку фронтенда ForestProof

> Документ описывает, что **осталось сделать во фронтенде** согласно ТЗ кейса
> «Спутниковая верификация "зелёных" инвестиций и углеродных кредитов»
> (КосмоХакатон 2026), с учётом уже реализованного backend (C# + Go).

**Версия:** 1.0
**Область:** только `Frontend/` (React SPA). Backend-задачи здесь не описываются,
но перечислены как зависимости.
**Основание:** `Решение.pdf` (разделы 6, 7, 8, 10, 11), `таски.pdf` (страницы 1–12),
`Backend/WORK_SPLIT.md`, инвентаризация текущего кода `Frontend/src`.

---

## 0. Как читать документ

- Раздел 3 — что уже есть (чтобы не переделывать).
- Раздел 5 — **самое важное**: реальный контракт с Go-шлюзом. Текущий
  `api/client.js` написан под старый/моковый контракт и в реальном режиме
  сломается.
- Раздел 6 — постановка по экранам (что доделать).
- Раздел 8 — готовый список задач с ID и приоритетами.
- Раздел 9 — критерии приёмки.

Приоритеты: **P0** — без этого демо/кейс не проходит; **P1** — требуется ТЗ,
но можно успеть после P0; **P2** — улучшения.

---

## 1. Цель и границы фронтенда

Фронтенд — единственный пользовательский интерфейс веб-сервиса. Он обязан
обеспечить сквозной путь верификатора:

> территория и период → доступность данных → запас и изменение → карта
> подтверждений → неопределённость → baseline → потенциальные единицы →
> воспроизводимый отчёт.

Что фронтенд **делает**: формы, карта, графики, карточки, статусы, отчёт.
Что фронтенд **не делает**: не считает углерод, не интерпретирует причину
самостоятельно, не сертифицирует единицы. Все числа приходят с backend.

Обязательные требования к UI из ТЗ:
- адаптивность и русская локализация (ТЗ 8, таблица Web UI);
- различение «Q = 0» и «Q недоступен» (ТЗ 5.9, 7.7);
- запрет формулировки «сертифицированные кредиты» / «кредит выпущен» (ТЗ 5.14, 14.1);
- ограничения и предупреждения — рядом с числом, а не мелким текстом внизу (таски.pdf);
- главный результат виден за несколько секунд (таски.pdf, критерий дизайнера).

---

## 2. Текущее состояние (инвентаризация)

### 2.1. Стек и запуск

| Параметр | Значение |
|---|---|
| Фреймворк | React `^19.3.0`, JS (не TS), Vite `^8.3.0` |
| UI | antd `^6.6.4`, `@ant-design/icons ^6.3.4` |
| Роутинг | `react-router-dom ^7.18.4` |
| Запросы | `@tanstack/react-query ^5.103.1` |
| Графики | `echarts ^6.1.0`, `echarts-for-react ^3.0.6` |
| Карта | `maplibre-gl ^6.10.0` |
| Тема | `src/theme.js`, зелёный primary `#2f7d32` |
| Локаль | `ruRU` в `src/main.jsx` |
| Прокси Vite | `/api` → `http://localhost:5300` (`vite.config.js:6-14`) |

### 2.2. Маршруты (`src/App.jsx`)

`/` Dashboard · `/new` Create · `/analysis` Results · `/map` Map ·
`/experiment` Experiment · `*` 404.
Отдельного маршрута **отчёта нет** (отчёт встроен в `/analysis`).

### 2.3. API-клиент и режим mock (`src/api/client.js`)

- `USE_MOCK = import.meta.env.VITE_USE_MOCK !== 'false'` → **по умолчанию `true`**,
  файлов `.env` в проекте нет, то есть приложение целиком на моках.
- `API_BASE = import.meta.env.VITE_API_BASE ?? '/api/v1'`.
- Функции: `listAreas`, `listSources`, `createAnalysis`, `getSummary`,
  `getSensitivity`, `getAreasGeoJson` (мёртвый код), `buildReportHtml`
  (клиентская генерация HTML).
- `normalizeSummary()` переводит PascalCase backend в camelCase UI.

### 2.4. Компоненты (`src/components/`)

| Компонент | Состояние |
|---|---|
| `AppLayout.jsx` | реальный: header, меню, бейдж mock, футер-дисклеймер |
| `MapView.jsx` | maplibre; слои `aoi`, `coverage` (дубль AOI), `zones`. `gfc`/`modis` не рисуются |
| `MetricCard.jsx` | реальный (antd Statistic + tooltip) |
| `StatusTag.jsx` | реальный (run/unit/cause) |
| `UncertaintyChart.jsx` | реальный (bar L/Eproj/U) |
| `UnitsPanel.jsx` | реальный; различает «Q = 0» и «Q недоступен», цены 500/1500/4000 |
| `YearlyChart.jsx` | реальный (C_t, c̄_t, coverage) |
| `ZonesTable.jsx` | реальный (id, площадь, вклад, evidence, причина) |

### 2.5. Страницы (`src/pages/`)

- `DashboardPage` — 4 карточки AOI + таблица источников. Нет счётчиков, последних
  проверок, покрытия, средней длительности, CTA.
- `CreateAnalysisPage` — выбор AOI/год, textarea GeoJSON, загрузка файла.
  Нет полей «название проекта», «заявленный результат», «описание»; валидации
  только `start < end`; сабмит не вызывает API, а кладёт geometry в URL.
- `AnalysisResultPage` — карточки метрик, графики, карта, зоны, экспорт HTML/PDF
  (клиентский). Нет `Ebase` на экране, нет прогресса, нет серверного отчёта.
- `MapPage` — переключатели слоёв (gfc/modis — no-op), карта, панель зон.
- `ExperimentPage` — k=1/k=2, SCL 4–5/4–7 (значения пикселей захардкожены в моке),
  сравнение Vologda/Tver. Нет сравнения «CCI Change vs собственная разность».

### 2.6. Что уже соответствует ТЗ

- Все 7 статусов прогона (`constants.js:10-18`).
- Разделение «Q = 0, расчёт допустим» / «Q недоступен» (`UnitsPanel.jsx`).
- Причины блокировки Q (`BLOCK_REASON_LABELS`).
- Ценовые сценарии 500/1500/4000.
- Русская локализация и единицы (т C, т CO₂-экв., га).
- Дисклеймер «потенциальные, не сертифицированные единицы».

---

## 3. Целевая архитектура

Стек оставляем. Меняем **слой доступа к данным** и **структуру страниц**.

```
src/
  api/
    http.js          # fetch-обёртка: base, JSON, разбор { error: { code, message } }
    client.js        # публичные функции под контракт Go-шлюза
    mappers.js       # нормализация ответов backend -> UI-модель
    mock/            # мок-режим, эмулирующий ту же последовательность вызовов
  hooks/
    useAnalysisRun.js  # POST -> polling status -> summary/changes/layers
    useAreas.js
    useSources.js
    useSensitivity.js
  constants.js       # статусы, слои, единицы, тексты ограничений
  components/        # UI-примитивы (без вызовов API)
  pages/             # экраны, только композиция
```

Правила:
- Компоненты не вызывают `fetch` напрямую; только hooks/api.
- Все текстовые формулировки-ограничения — в `constants.js`, не разбросаны по JSX.
- Любая страница, зависящая от API, обязана иметь состояния: loading / error /
  empty / success.
- Мок-режим остаётся для офлайн-демо, но должен воспроизводить **ту же**
  последовательность `POST → status → summary/changes/layers/report`, что и Go-шлюз.

### 3.1. Переменные окружения (создать)

`.env.development` (режим реального API):
```
VITE_USE_MOCK=false
VITE_API_BASE=/api/v1
```

`.env.mock` / `.env` (офлайн-демо):
```
VITE_USE_MOCK=true
VITE_API_BASE=/api/v1
```

### 3.2. Прокси Vite (исправить)

Текущий target `http://localhost:5300` не соответствует ни C# (5126), ни Go (8080).
Публичный API — Go-шлюз (порт `:8080`). Установить:
```js
proxy: { '/api': { target: 'http://localhost:8080', changeOrigin: true } }
```

---

## 4. Контракт с backend (Go-шлюз, `/api/v1`)

> Фронтенд общается **только с Go-шлюзом**. C# — внутренний сервис, браузер его
> не вызывает (у C# нет CORS). Все запросы идут через Vite-прокси `/api`.

### 4.1. Поток запуска анализа (критично)

Backend **асинхронный**, `POST /analyses` возвращает не результат, а job.

```
1. POST /api/v1/analyses            -> 202 { id, status, aoiId?, inputHash, startYear, endYear, createdAt, idempotent }
   (повторный тот же запрос        -> 200 { ..., idempotent: true })
2. GET  /api/v1/analyses/{id}/status -> { id, status, phase, progress, errorMessage?, warnings?, createdAt, updatedAt }
   поллить, пока status ∈ { complete, partial, units_unavailable, failed }
   statuses: draft | validating | running | complete | partial | units_unavailable | failed
3. GET  /api/v1/analyses/{id}/summary   -> сводка (см. 4.3)
4. GET  /api/v1/analyses/{id}/changes   -> GeoJSON зон (см. 4.4)
5. GET  /api/v1/analyses/{id}/layers    -> список слоёв карты (см. 4.5)
6. POST /api/v1/analyses/{id}/reports?format=pdf|html|json -> файл отчёта
```

Ключевое: `{id}` — это **id задания Go**, а не `aoiId`. Его нужно сохранять после
POST (в состоянии/URL) и использовать во всех последующих запросах.
`GET /analyses/{aoiId}/summary` в Go **не существует**.

### 4.2. Эндпоинты

| Метод | Путь | Ответ | Использование |
|---|---|---|---|
| POST | `/analyses` | job envelope | создать проверку |
| GET | `/analyses/{id}/status` | статус/прогресс | polling, экран прогресса |
| GET | `/analyses/{id}/summary` | `AnalysisSummary` | результаты |
| GET | `/analyses/{id}/changes` | GeoJSON FeatureCollection | карта зон, карточка зоны |
| GET | `/analyses/{id}/layers` | `{ aoiId, layers[] }` | легенда/доступность слоёв |
| GET | `/analyses/{id}/layers/{layer}` | bytes (png/tiff/geojson) | тайлы/изображения карты |
| POST | `/analyses/{id}/reports?format=` | pdf/html/json | скачать/открыть отчёт |
| GET | `/sources` | каталог источников | дашборд, отчёт |
| GET | `/areas` | список AOI (проксируется из C#) | выбор территории |
| GET | `/projects` | список проектов | дашборд |
| GET | `/experiments/sensitivity?aoiId=&startYear=&endYear=` | k1/k2/SCL | исследовательский режим |

### 4.3. Форма `summary` (camelCase, без обёртки)

```
runId, methodVersion, dataVersion, createdAt, inputHash, status,
aoiId ("" для произвольного контура), startYear, endYear, polygonAreaHectares,
yearlySeries: [{ year, areaHectares, totalCarbon, meanCarbonPerHectare, coverage }],
change:      { deltaCarbon, projectEmission, emissionPerHectarePerYear },
uncertainty: { lower, upper, halfWidth },          // L, U, H
baseline:    { baselineEmission },                 // Ebase
units: {
  status: "Available" | "Zero" | "Unavailable",
  reason: "None" | "ZeroArea" | "NonPositivePeriod" | "MissingData" |
          "IncompleteCoverage" | "MissingBaseline" | "NonPositiveResult" |
          "UncertaintyExceedsResult",
  resultRelativeToBaseline,   // R
  uncertaintyDeduction,       // UNC
  adjustedResult,             // Radj
  reserve,                    // B
  units,                      // Q (int | null)
  priceScenarios: [{ pricePerUnit, value }]
},
changeZones:        [{ id, areaHectares, contributionToDeltaCarbon }],  // без geometry!
changeZoneEvidence: [{ zoneId, evidenceTypes: ["Gfc"|"Modis"|"Sentinel2"], causeStatus: "Confirmed"|"Probable"|"Unknown" }],
cciChangeMeanTonnesPerHectare: number | null,
warnings: string[]
```

Важно: `summary` **не содержит геометрию** ни AOI, ни зон. Геометрию AOI/контура
нужно хранить на фронте (из формы или `/areas`), геометрию зон — брать из `/changes`.

### 4.4. Форма `/changes` (GeoJSON)

```
{ "type": "FeatureCollection",
  "features": [{
    "type": "Feature",
    "geometry": { ... WGS84 ... },
    "properties": {
      "id": int,
      "areaHectares": double,
      "contributionToDeltaCarbon": double,
      "pixelCount": int,
      "evidenceTypes": string[],
      "causeStatus": string,
      "interpretation": string   // готовый текст «что можно утверждать»
    }
  }]
}
```

### 4.5. Слои карты

`GET /analyses/{id}/layers` → `{ aoiId, layers: [{ key, available, reason?, description?, legend?, sourceId?, licenseUrl? }] }`.

Ключи: `aoi, agb_start, agb_end, gfc, cci_change, modis_burn, coverage,
sentinel2_before, sentinel2_after, ndvi, nbr`.

Ограничения backend (учесть в UI):
- `ndvi`/`nbr` всегда `available: false` с причиной — показывать как «недоступно»,
  а не как ошибку;
- `cci_change` доступен только для пары 2019–2020;
- для произвольного контура (без `aoiId`) слои возвращают 404 — карта должна
  работать на AOI, для кастомного контура показывать только `aoi`/`zones`.

### 4.6. Ошибки

Go-шлюз: `{ "error": { "code": "invalid_request|not_found|conflict|upstream_error|timeout|internal", "message": "..." } }`.
Текущий `request()` читает `body.message` — **исправить** на `body?.error?.message ?? body?.message`.

### 4.7. Прочие контракты, требующие нормализации

| Эндпоинт | Реальная форма | Проблема фронта |
|---|---|---|
| `GET /areas` | `[{ id, name, area_ha, selection_role }]` | фронт ждёт `aoi_id, region, baseline_id, bbox_*, analysis_start_year…` |
| `GET /sources` | `[{ sourceId, product, version, licenseUrl, requiredAttribution, files:[…] }]` | мок отдаёт snake_case `source_id`, `license_url` |
| `POST /analyses` | job envelope | фронт прогоняет его через `normalizeSummary` |
| `GET /experiments/sensitivity` | `{ k1, k2, sclStrict, sclExtended, sclStrictValidPixels, sclExtendedValidPixels, cciChangeMean, selfDifferenceMean }` | `buildSensitivity`-мок другой формы |
| `POST /reports` | поток байтов pdf/html/json | используется клиентский `buildReportHtml` |

Зависимости от backend (если решим не нормализовывать на фронте):
- расширить `GET /areas` полями `region`, `bbox`, `baseline_id`;
- отдавать хэши и лицензии в `/sources` (уже есть в Go — использовать).

---

## 5. Требования по экранам

### 5.1. Общий каркас (FE-01, P0)

- Меню: Дашборд, Карта, Новая проверка, Исследование, **Отчёт** (или отчёт как
  действие на странице результатов).
- Глобальный переключатель/индикатор режима (mock / real API) — оставить бейдж,
  добавить явный статус подключения к API.
- Хлебные крошки и кнопка «Назад» на страницах результатов/карты/отчёта.
- Дисклеймер о несертифицированных единицах — на всех экранах с числами.

### 5.2. Экран 1 — Дашборд (FE-10, P1)

Доделать:
- счётчик проектов (`GET /projects`);
- «последние проверки» — список последних run с AOI, периодом, статусом, датой;
- статусы покрытия по AOI (из последних summary или заглушка «нет данных»);
- средняя длительность расчёта;
- CTA «Новая проверка»;
- 4 карточки AOI с **различимыми** ролями: контроль / потери / пожар / ранняя
  потеря (сейчас `roleColor` в `DashboardPage.jsx:9-14` путает пожар и раннюю
  потерю для `RU_MORDOVIA_04`).

### 5.3. Экран 2 — Создание проверки (FE-20, P0)

Доделать поля:
- название проекта (обязательное);
- заявленный результат (число + единица);
- необязательное описание.

Доделать валидации (ТЗ 7.2, 4.1) — сейчас в UI только текст-памятка:
- WGS 84 (парсинг GeoJSON, проверка CRS);
- отсутствие самопересечений;
- площадь ≤ 20 км² (посчитать в равновеликой проекции, не по bbox);
- контур внутри покрытия;
- `start < end`, годы 2019–2024 (уже частично).

Логика сабмита:
- собрать payload `{ aoiId?, polygonGeoJson?, startYear, endYear, methodProfile? }`;
- вызвать `POST /analyses`, получить `id`;
- перейти на экран прогресса/результатов с `runId` в URL;
- **не** передавать geometry через query-string.

### 5.4. Экран 3 — Карта (FE-30, P0)

- Переключатели слоёв строить из `GET /analyses/{id}/layers`, а не из
  захардкоженного `LAYER_DEFINITIONS`; показывать недоступные слои с причиной.
- Реально рисовать: `agb_start`, `agb_end` (разность), `gfc`, `modis_burn`,
  `sentinel2_before/after`, `coverage`. Для растровых слоёв — тайлы/COG по
  `GET /analyses/{id}/layers/{layer}`.
- Легенда для каждого слоя (`legend` из ответа).
- Клик по зоне: площадь, период, вклад в ΔC, источники подтверждения, статус
  причины и текст «что можно утверждать / чего утверждать нельзя»
  (`properties.interpretation` из `/changes`).
- Зоны брать из `/changes` (там есть geometry), а не из `summary.changeZones`.
- Показывать снимок до/после и временное окно.

### 5.5. Экран 4 — Результаты (FE-40, P0)

- Добавить карточку **Ebase** (сейчас только в HTML-отчёте).
- Сохранить все обязательные карточки (ТЗ 7.4): площадь AOI и расчётная;
  c̄_t0/t1; C_t0/t1; ΔC; Eproj; e; L/U/H; coverage; Ebase; R; UNC; Radj; B; Q;
  ценовые сценарии; статус потенциального расчёта.
- Явно различать «Q = 0» и «Q недоступен» + причина блокировки
  (`BLOCK_REASON_LABELS`).
- Показывать период как «2019–2024, 5 лет».
- Предупреждения из `warnings` — рядом с числами.
- Экспорт: заменить клиентский `buildReportHtml` на серверный
  `POST /analyses/{id}/reports` (см. 5.7).
- Добавить индикатор прогресса из `/status` (фаза + прогресс), а не один спиннер.

### 5.6. Экран 5 — Исследовательский режим (FE-50, P1)

- SCL 4–5 vs 4–7: брать реальные `sclStrict`/`sclExtended` и `*ValidPixels`
  из `/experiments/sensitivity` (сейчас пиксели захардкожены в моке).
- k=1 vs k=2: реальные `k1`/`k2`.
- Сравнение изменённого (`RU_VOLOGDA_02`) и контрольного (`RU_TVER_01`) AOI.
- **Добавить** сравнение «CCI Change 2019–2020 vs собственная разность»
  (`cciChangeMean` vs `selfDifferenceMean`).
- Пометка «исследовательский режим», явный вывод об устойчивости результата.
- Запрещённые формулировки: «наземная валидация», «доверительный интервал».

### 5.7. Экран 6 — Отчёт (FE-60, P0)

- Добавить маршрут/страницу отчёта или полноценный просмотр.
- Формировать отчёт через `POST /analyses/{id}/reports?format=pdf|html|json`
  (серверный HTML/PDF/JSON), не клиентским `buildReportHtml`.
- Обязательные разделы (ТЗ 7.6): территория и период; итоговая таблица;
  годовая динамика; карта; зоны изменений; источник и дата сцен; метод;
  baseline; неопределённость; ограничения; параметры; **хэши**; run_id и дата;
  подпись «не сертифицированные единицы».
- Кнопки: открыть HTML, скачать PDF, скачать JSON, скачать CSV годовой динамики
  (эндпоинт `yearly.csv` есть только в C#; в Go отсутствует — см. раздел 10).

---

## 6. Сквозные требования

### 6.1. Статусы (FE-02, P0)
Все 7 статусов отображать единообразно (`RUN_STATUS`): draft, validating, running,
complete, partial, units_unavailable, failed. При `failed` — `errorMessage` и
кнопка «повторить».

### 6.2. Q = 0 vs Q недоступен (FE-03, P0)
Сохранить текущее поведение `UnitsPanel`, но привязать к реальному `reason`.
`units.units === null` → «недоступно»; `units.units === 0` → «Q = 0, расчёт
допустим, результата нет».

### 6.3. Локализация и единицы (FE-04, P1)
Русский язык, единицы `т C`, `т C/га`, `т CO₂-экв.`, `т CO₂-экв./га/год`, `га`.
Все подписи осей графиков — с единицами. Не смешивать разные единицы на одной оси
без явной маркировки.

### 6.4. Валидация геометрии (FE-05, P0)
Клиентская валидация — обязательна до отправки (WGS84, самопересечения,
≤20 км², внутри покрытия). Для расчёта площади использовать библиотеку
(например, `@turf/area`/`turf` + equal-area), а не bbox. Backend дублирует
проверку и возвращает `invalid_request`.

### 6.5. Загрузка / ошибки / пусто (FE-06, P0)
Каждый экран: skeleton/spinner, понятная ошибка, пустое состояние, кнопка
повтора. Ошибку API показывать человеческим текстом, не `[object Object]`.

### 6.6. Доступность и адаптивность (FE-07, P1)
- Адаптив под ноутбук демонстрации (antd grid уже используется, maxWidth 1400).
- Контраст, размер шрифта, alt/aria для иконок и графиков.
- Знак результата (потеря/накопление) не только цветом, но и текстом.

### 6.7. Производительность (FE-08, P2)
- Polling `/status` с backoff (например 1s → 2s → 3s, максимум N попыток).
- Растровые слои — по требованию (lazy), не грузить все сразу.
- Не блокировать браузер: длительный расчёт идёт на backend, UI только опрашивает.

---

## 7. Что удалить / почистить

- `Frontend/заглушка.txt` — удалить.
- `api.getAreasGeoJson` — мёртвый код.
- `constants.PRICE_SCENARIOS_RUB` — дублируется в `client.js`; оставить один источник.
- Клиентский `buildReportHtml` — заменить серверным отчётом.
- Захардкоженные SCL-пиксели и `stableId` в `mockData.js` — оставить только для
  mock-режима, помечать явно.

---

## 8. План работ

### P0 — блокирует сдачу

| ID | Задача | Файлы |
|---|---|---|
| FE-01 | Настроить env (`.env.development`), исправить прокси на Go `:8080` | `vite.config.js`, `.env*` |
| FE-02 | `http.js`: base, JSON, разбор `{error:{code,message}}` | `api/http.js` |
| FE-03 | Переписать `client.js` под Go-контракт (job flow, все эндпоинты) | `api/client.js` |
| FE-04 | Mappers: `summary`, `changes`, `areas`, `sources`, `sensitivity` | `api/mappers.js` |
| FE-05 | Hook `useAnalysisRun`: POST → polling → summary/changes | `hooks/useAnalysisRun.js` |
| FE-06 | Создание проверки: поля, валидации, реальный сабмит, переход с `runId` | `pages/CreateAnalysisPage.jsx` |
| FE-07 | Результаты: Ebase, прогресс, warnings у чисел, серверный отчёт | `pages/AnalysisResultPage.jsx` |
| FE-08 | Карта: слои из `/layers`, зоны из `/changes`, карточка зоны по `interpretation` | `MapPage.jsx`, `MapView.jsx` |
| FE-09 | Отчёт: маршрут + вызов `POST /reports`, разделы, скачивание PDF/HTML/JSON | `pages/ReportPage.jsx`, `App.jsx` |
| FE-10 | Статусы и Q-семантика на всех экранах (проверка) | `constants.js`, компоненты |

### P1 — требуется ТЗ

| ID | Задача | Файлы |
|---|---|---|
| FE-11 | Дашборд: проекты, последние проверки, покрытие, средняя длительность, CTA, 4 роли | `DashboardPage.jsx` |
| FE-12 | Исследование: реальные SCL/k, CCI vs собственная разность | `ExperimentPage.jsx` |
| FE-13 | Локализация единиц и подписи осей | компоненты графиков |
| FE-14 | Адаптив и доступность, текстовые подписи к знаку | все страницы |
| FE-15 | Нормализация `/areas` и `/sources` (или расширение backend) | `api/mappers.js` |

### P2 — улучшения

| ID | Задача |
|---|---|
| FE-16 | Polling с backoff, lazy-загрузка растровых слоёв |
| FE-17 | Error boundary, тесты критичных mappers/hooks |
| FE-18 | Единый компонент «Ограничение рядом с числом» |

---

## 9. Критерии приёмки фронтенда

Из ТЗ 7.7 и таски.pdf:

1. Новый полигон внутри AOI обрабатывается без изменения кода.
2. Результат для 2019–2024 воспроизводится при повторном запуске (тот же `runId`).
3. Площадь — по пересечению пикселей (число с backend отображается как есть).
4. При неполном покрытии Q помечен **недоступным**, а не нулём.
5. Виден внешний STAC-источник или его кэш (карточка источника с версией/датой).
6. Отчёт содержит все обязательные разделы, run_id, хэши, дату.
7. Контрольный и изменившийся AOI показаны раздельно.
8. Ошибки не скрываются, показывается причина.
9. Главный результат виден за несколько секунд, единицы подписаны.
10. Нигде нет формулировок «сертифицированные кредиты» / «кредит выпущен».

---

## 10. Известные ограничения backend (влияют на фронт)

- `POST /analyses` синхронный в C#, но Go оборачивает его в job → нужен polling.
- Нет `GET /analyses/{aoiId}/summary` в Go — только по `{jobId}`.
- `/changes` не содержит envelope (`method_version`, `run_id`, `warnings`) —
  метаданные брать из `summary`/`status`.
- Нет `GET /analyses/{id}/layers/{layer}` для произвольного контура (404).
- `ndvi`/`nbr` всегда недоступны — показывать как ограничение, не ошибку.
- `GET /sources` в Go богаче C# (есть хэши и лицензии) — использовать Go.
- Экспорт годовой динамики в CSV (`yearly.csv`) есть только в C#; в Go не
  проксируется. Для скачивания CSV — либо добавить в Go, либо собирать CSV на
  фронте из `summary.yearlySeries`.
- CORS у C# не настроен; фронт обязан ходить через Go-шлюз.

---

## 11. Риски и открытые вопросы

1. **Формат отчёта:** ТЗ 19 спрашивает, нужен PDF, HTML или оба. Сейчас backend
   умеет все три — по умолчанию показываем HTML, даём скачать PDF и JSON.
2. **Публичный URL демо** (ТЗ 19) — не влияет на фронт.
3. **Порог площади зоны** для наглядности (ТЗ 19) — параметр backend.
4. **Offline-демо:** нужно подтвердить, что mock-режим воспроизводит полный путь
   без сети и что `VITE_USE_MOCK=true` собирается в отдельный бандл.
5. **Геометрия произвольного контура:** `summary` её не возвращает — хранить на
   фронте (sessionStorage/URL-state) или запрашивать из `/areas`.

---

## Приложение A. Соответствие экранов ТЗ

| ТЗ | Экран | Статус сейчас | После работ |
|---|---|---|---|
| 7.1 | Дашборд | частично | FE-11 |
| 7.2 | Создание проверки | частично | FE-06 |
| 7.3 | Карта | частично | FE-08 |
| 7.4 | Результаты | частично | FE-07 |
| 7.5 | Исследовательский режим | частично | FE-12 |
| 7.6 | Отчёт | частично (внутри результатов) | FE-09 |
| 6.2 | Статусы | есть | FE-10 |
| 5.9 | Q=0 / Q недоступен | есть | FE-10 |
| 8 | Web UI (адаптив, ru) | есть | FE-14 |
