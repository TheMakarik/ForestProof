using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using ForestProof.Backend.Domain.Analysis;
using ForestProof.Backend.Domain.ChangeZones;
using ForestProof.Backend.Domain.Enums;
using ForestProof.Backend.Domain.Report;
using ForestProof.Backend.Domain.Units;
using ForestProof.Backend.Options;
using ForestProof.Backend.Services.Report.Interfaces;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ForestProof.Backend.Services.Report;

/// <summary>
/// Формирует отчёт по сводке расчёта: HTML, JSON, манифест и PDF.
/// </summary>
/// <param name="options">Параметры набора данных, включая имя папки отчётов.</param>
public sealed class ReportService(IOptions<DataOptions> options) : IReportService
{
    /// <summary>
    /// Версия методики, используемая в отчёте.
    /// </summary>
    public const string MethodVersion = "1.0";

    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("ru-RU");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _outputDirectoryName = options.Value.ReportOutputDirectoryName;

    static ReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc />
    public ReportResult Generate(AnalysisSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var generatedAt = DateTime.UtcNow;

        return new ReportResult
        {
            Html = BuildHtml(summary, generatedAt),
            Json = JsonSerializer.Serialize(summary, JsonOptions),
            ManifestJson = BuildManifest(summary, generatedAt),
            Pdf = BuildPdf(summary, generatedAt)
        };
    }

    private string BuildManifest(AnalysisSummary summary, DateTime generatedAt)
    {
        var manifest = new Dictionary<string, object?>
        {
            ["method_version"] = summary.MethodVersion,
            ["data_version"] = summary.DataVersion,
            ["run_id"] = summary.RunId,
            ["input_hash"] = summary.InputHash,
            ["generated_at"] = generatedAt.ToString("O", CultureInfo.InvariantCulture),
            ["output_directory"] = _outputDirectoryName,
            ["aoi_id"] = summary.AoiId,
            ["start_year"] = summary.StartYear,
            ["end_year"] = summary.EndYear,
            ["warnings"] = summary.Warnings,
            ["evidence_types"] = EvidenceTypes(summary).Select(EvidenceDisplay).ToArray(),
            ["scene_years"] = summary.ChangeZoneEvidence
                .SelectMany(evidence => evidence.EvidenceYears)
                .Distinct()
                .Order()
                .ToArray(),
            ["source_assets"] = summary.SourceAssets
                .Select(asset => new Dictionary<string, object?>
                {
                    ["aoi_id"] = asset.AoiId,
                    ["relative_path"] = asset.RelativePath,
                    ["version"] = asset.Version,
                    ["retrieved_at"] = asset.RetrievedAt,
                    ["sha256"] = asset.Sha256
                })
                .ToArray()
        };

        return JsonSerializer.Serialize(manifest, JsonOptions);
    }

    private static string BuildHtml(AnalysisSummary summary, DateTime generatedAt)
    {
        var first = summary.YearlySeries.Count > 0 ? summary.YearlySeries[0] : null;
        var last = summary.YearlySeries.Count > 0 ? summary.YearlySeries[^1] : null;
        var calculatedArea = last?.AreaHectares ?? 0;

        var builder = new StringBuilder();
        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"ru\">");
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset=\"utf-8\"/>");
        builder.AppendLine($"<title>ForestProof — отчёт {Escape(summary.AoiId)}</title>");
        builder.AppendLine("<style>");
        builder.AppendLine("body{font-family:Arial,Helvetica,sans-serif;margin:24px;color:#1f2933;}");
        builder.AppendLine("h1{font-size:22px;}h2{font-size:17px;margin-top:24px;border-bottom:1px solid #cbd2d9;}");
        builder.AppendLine("table{border-collapse:collapse;margin-top:8px;}");
        builder.AppendLine("th,td{border:1px solid #cbd2d9;padding:4px 10px;text-align:left;}");
        builder.AppendLine("th{background:#f0f4f8;}.disclaimer{margin-top:24px;font-style:italic;color:#7b8794;}");
        builder.AppendLine("</style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("<h1>Отчёт ForestProof</h1>");
        builder.AppendLine($"<p>Дата формирования: {FormatDate(generatedAt)}</p>");
        builder.AppendLine($"<p>method_version = {summary.MethodVersion}</p>");
        builder.AppendLine($"<p>data_version = {Escape(summary.DataVersion)}; run_id = {Escape(summary.RunId)}</p>");

        builder.AppendLine("<h2>Территория и период</h2>");
        builder.AppendLine("<table>");
        Row(builder, "Территория (AoiId)", Escape(summary.AoiId));
        Row(builder, "Период", $"{summary.StartYear}–{summary.EndYear}");
        Row(builder, "Площадь полигона, га", Format(summary.PolygonAreaHectares));
        Row(builder, "Рассчитанная площадь, га", Format(calculatedArea));
        builder.AppendLine("</table>");

        builder.AppendLine("<h2>Запас углерода</h2>");
        builder.AppendLine("<table>");
        Row(builder, $"C_t0, т C ({summary.StartYear})", first is null ? "—" : Format(first.TotalCarbon));
        Row(builder, $"C_t1, т C ({summary.EndYear})", last is null ? "—" : Format(last.TotalCarbon));
        Row(builder, "c̄_t0, т C/га", first is null ? "—" : Format(first.MeanCarbonPerHectare));
        Row(builder, "c̄_t1, т C/га", last is null ? "—" : Format(last.MeanCarbonPerHectare));
        Row(builder, "ΔC, т C", Format(summary.Change.DeltaCarbon));
        Row(builder, "Eproj, т CO₂-экв.", Format(summary.Change.ProjectEmission));
        Row(builder, "e, т CO₂-экв./га/год", Format(summary.Change.EmissionPerHectarePerYear));
        builder.AppendLine("</table>");

        builder.AppendLine("<h2>Неопределённость</h2>");
        builder.AppendLine("<table>");
        Row(builder, "L, т CO₂-экв.", Format(summary.Uncertainty.Lower));
        Row(builder, "U, т CO₂-экв.", Format(summary.Uncertainty.Upper));
        Row(builder, "H, т CO₂-экв.", Format(summary.Uncertainty.HalfWidth));
        builder.AppendLine("</table>");

        builder.AppendLine("<h2>Покрытие по годам</h2>");
        builder.AppendLine("<table>");
        builder.AppendLine("<tr><th>Год</th><th>Площадь, га</th><th>C_t, т C</th><th>c̄_t, т C/га</th><th>coverage</th></tr>");
        foreach (var year in summary.YearlySeries)
        {
            builder.AppendLine(
                $"<tr><td>{year.Year}</td><td>{Format(year.AreaHectares)}</td>" +
                $"<td>{Format(year.TotalCarbon)}</td><td>{Format(year.MeanCarbonPerHectare)}</td>" +
                $"<td>{Format(year.Coverage)}</td></tr>");
        }

        builder.AppendLine("</table>");

        builder.AppendLine("<h2>Базовая линия и потенциальные единицы</h2>");
        builder.AppendLine("<table>");
        Row(builder, "Ebase, т CO₂-экв.", Format(summary.Baseline.BaselineEmission));
        Row(builder, "R, т CO₂-экв.", Format(summary.Units.ResultRelativeToBaseline));
        Row(builder, "UNC", Format(summary.Units.UncertaintyDeduction));
        Row(builder, "Radj, т CO₂-экв.", Format(summary.Units.AdjustedResult));
        Row(builder, "B, т CO₂-экв.", Format(summary.Units.Reserve));
        Row(builder, "Q (потенциальные единицы)", FormatUnits(summary.Units));
        Row(builder, "Статус единиц", summary.Units.Status.ToString());
        Row(builder, "Причина", summary.Units.Reason.ToString());
        builder.AppendLine("</table>");

        builder.AppendLine("<h2>Зоны изменений</h2>");
        builder.AppendLine("<table>");
        builder.AppendLine(
            "<tr><th>Id</th><th>Площадь, га</th><th>Вклад в ΔC, т C</th><th>Evidence</th><th>Статус причины</th></tr>");
        foreach (var zone in summary.ChangeZones)
        {
            var evidence = summary.ChangeZoneEvidence.FirstOrDefault(item => item.ZoneId == zone.Id);
            var evidenceText = evidence is null || evidence.EvidenceTypes.Count == 0
                ? "—"
                : string.Join(", ", evidence.EvidenceTypes.Select(EvidenceDisplay));
            var cause = evidence is null ? "—" : CauseDisplay(evidence.CauseStatus);

            builder.AppendLine(
                $"<tr><td>{zone.Id}</td><td>{Format(zone.AreaHectares)}</td>" +
                $"<td>{Format(zone.ContributionToDeltaCarbon)}</td><td>{evidenceText}</td>" +
                $"<td>{cause}</td></tr>");
        }

        builder.AppendLine("</table>");

        builder.AppendLine("<h2>Предупреждения</h2>");
        if (summary.Warnings.Count == 0)
        {
            builder.AppendLine("<p>Отсутствуют.</p>");
        }
        else
        {
            builder.AppendLine("<ul>");
            foreach (var warning in summary.Warnings)
                builder.AppendLine($"<li>{Escape(warning)}</li>");
            builder.AppendLine("</ul>");
        }

        builder.AppendLine("<p class=\"disclaimer\">Потенциальные, не сертифицированные единицы.</p>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }

    private static byte[] BuildPdf(AnalysisSummary summary, DateTime generatedAt)
    {
        try
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(text => text.FontSize(11));

                    page.Header()
                        .Text("ForestProof report")
                        .SemiBold()
                        .FontSize(16);

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        column.Spacing(6);
                        column.Item().Text($"Area: {summary.AoiId}");
                        column.Item().Text($"Period: {summary.StartYear}-{summary.EndYear}");
                        column.Item().Text($"Methodology: v{MethodVersion}");
                        column.Item().Text($"Generated: {generatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} UTC");
                        column.Item().Text($"Polygon area: {Invariant(summary.PolygonAreaHectares)} ha");
                        column.Item().Text($"Delta C: {Invariant(summary.Change.DeltaCarbon)} t C");
                        column.Item().Text($"Eproj: {Invariant(summary.Change.ProjectEmission)} t CO2-eq");
                        column.Item().Text($"R: {Invariant(summary.Units.ResultRelativeToBaseline)} t CO2-eq");
                        column.Item().Text($"Radj: {Invariant(summary.Units.AdjustedResult)} t CO2-eq");
                        column.Item().Text($"Q: {UnitsText(summary.Units)}");
                        column.Item().Text($"Unit status: {summary.Units.Status}");
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                    });
                });
            }).GeneratePdf();
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<EvidenceType> EvidenceTypes(AnalysisSummary summary) =>
        summary.ChangeZoneEvidence
            .SelectMany(item => item.EvidenceTypes)
            .Distinct()
            .OrderBy(type => type)
            .ToArray();

    private static string EvidenceDisplay(EvidenceType type) => type switch
    {
        EvidenceType.Gfc => "GFC",
        EvidenceType.Modis => "MODIS",
        EvidenceType.Sentinel2 => "Sentinel2",
        _ => type.ToString()
    };

    private static string CauseDisplay(CauseStatus status) => status switch
    {
        CauseStatus.Confirmed => "Confirmed",
        CauseStatus.Probable => "Probable",
        CauseStatus.Unknown => "Unknown",
        _ => status.ToString()
    };

    private static void Row(StringBuilder builder, string label, string value) =>
        builder.AppendLine($"<tr><th>{Escape(label)}</th><td>{value}</td></tr>");

    private static string Format(double value) => value.ToString("N2", Culture);

    private static string Invariant(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string FormatUnits(UnitResult units) =>
        units.Units is null ? "—" : units.Units.Value.ToString(Culture);

    private static string UnitsText(UnitResult units) =>
        units.Units is null ? "n/a" : units.Units.Value.ToString(CultureInfo.InvariantCulture);

    private static string FormatDate(DateTime value) =>
        value.ToString("dd.MM.yyyy HH:mm", Culture) + " UTC";

    private static string Escape(string value) => WebUtility.HtmlEncode(value);
}
