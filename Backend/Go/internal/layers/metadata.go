package layers

// layerMetadata carries the human-facing description and legend for a layer
// key. It is static per key (independent of AOI/period) and is copied onto
// every LayerInfo returned by ListLayers.
type layerMetadata struct {
	Description string
	Legend      string
}

// layerMetadataByKey documents every key in allLayerKeys. Legends state the
// physical unit or value range of the raster the gateway serves (or, for
// computed indices, the formula the value would follow).
var layerMetadataByKey = map[string]layerMetadata{
	"aoi": {
		Description: "Контур территории анализа (AOI) в GeoJSON.",
		Legend:      "Полигон GeoJSON без тематических значений.",
	},
	"agb_start": {
		Description: "Надземная биомасса на начало периода (ESA CCI Biomass).",
		Legend:      "Надземная биомасса, т/га.",
	},
	"agb_end": {
		Description: "Надземная биомасса на конец периода (ESA CCI Biomass).",
		Legend:      "Надземная биомасса, т/га.",
	},
	"gfc": {
		Description: "Потеря древесного покрова (Hansen GFC v1.13).",
		Legend:      "Год потери покрова (1–25); 0 — потери нет.",
	},
	"cci_change": {
		Description: "Изменение биомассы за фиксированный период 2019–2020 (ESA CCI Biomass Change).",
		Legend:      "Изменение надземной биомассы, т/га (положительное — прирост).",
	},
	"modis_burn": {
		Description: "Даты выгорания (MODIS MCD64A1 Burn Date).",
		Legend:      "День года выгорания (1–366); 0 — не горело.",
	},
	"coverage": {
		Description: "Пригодное покрытие по классификации сцен Sentinel-2 (SCL).",
		Legend:      "Код класса SCL: 1 — дефект, 2 — тёмные пиксели, 3 — тень облака, 4 — растительность, 5 — не растительность, 6 — вода, 7 — без классификации, 8–10 — облака, 11 — снег/лёд; пригодны классы 4–5 (расширенно 4–7).",
	},
	"sentinel2_before": {
		Description: "Спектральный композит Sentinel-2 L2A на начало периода.",
		Legend:      "Коэффициент отражения поверхности (безразмерный).",
	},
	"sentinel2_after": {
		Description: "Спектральный композит Sentinel-2 L2A на конец периода.",
		Legend:      "Коэффициент отражения поверхности (безразмерный).",
	},
	"ndvi": {
		Description: "Нормализованный разностный вегетационный индекс (NDVI), вычисляется по Sentinel-2.",
		Legend:      "NDVI = (B8A − B04) / (B8A + B04); безразмерный, от −1 до 1.",
	},
	"nbr": {
		Description: "Нормализованный разностный индекс выгорания (NBR), вычисляется по Sentinel-2.",
		Legend:      "NBR = (B8A − B12) / (B8A + B12); безразмерный, от −1 до 1.",
	},
	"ndwi": {
		Description: "Нормализованный разностный водный индекс (NDWI), вычисляется по Sentinel-2.",
		Legend:      "NDWI = (B03 − B08) / (B03 + B08); безразмерный, от −1 до 1.",
	},
}
