using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace ForestProof.Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "Areas",
                columns: table => new
                {
                    AoiId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ParentAoiId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Geometry = table.Column<MultiPolygon>(type: "geometry (multipolygon)", nullable: true),
                    AreaHectares = table.Column<double>(type: "numeric", nullable: false),
                    SelectionRole = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Areas", x => x.AoiId);
                    table.ForeignKey(
                        name: "FK_Areas_Areas_ParentAoiId",
                        column: x => x.ParentAoiId,
                        principalTable: "Areas",
                        principalColumn: "AoiId");
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sources",
                columns: table => new
                {
                    SourceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Product = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Version = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LicenseUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sources", x => x.SourceId);
                });

            migrationBuilder.CreateTable(
                name: "Baselines",
                columns: table => new
                {
                    AoiId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReferenceMean2015TcHa = table.Column<double>(type: "numeric", nullable: false),
                    ReferenceMean2019TcHa = table.Column<double>(type: "numeric", nullable: false),
                    HistoricalRateTcHaYr = table.Column<double>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Baselines", x => x.AoiId);
                    table.ForeignKey(
                        name: "FK_Baselines_Areas_AoiId",
                        column: x => x.AoiId,
                        principalTable: "Areas",
                        principalColumn: "AoiId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AoiId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ClaimedResult = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Areas_AoiId",
                        column: x => x.AoiId,
                        principalTable: "Areas",
                        principalColumn: "AoiId");
                    table.ForeignKey(
                        name: "FK_Projects_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Parameters",
                columns: table => new
                {
                    parameter = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ValueNumeric = table.Column<double>(type: "numeric", nullable: true),
                    SourceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parameters", x => x.parameter);
                    table.ForeignKey(
                        name: "FK_Parameters_Sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Sources",
                        principalColumn: "SourceId");
                });

            migrationBuilder.CreateTable(
                name: "SourceAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AoiId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Sha256 = table.Column<string>(type: "char(64)", nullable: true),
                    Crs = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourceAssets_Areas_AoiId",
                        column: x => x.AoiId,
                        principalTable: "Areas",
                        principalColumn: "AoiId");
                    table.ForeignKey(
                        name: "FK_SourceAssets_Sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Sources",
                        principalColumn: "SourceId");
                });

            migrationBuilder.CreateTable(
                name: "AnalysisRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    AoiId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedGeometry = table.Column<Geometry>(type: "geometry", nullable: true),
                    YearStart = table.Column<int>(type: "integer", nullable: false),
                    YearEnd = table.Column<int>(type: "integer", nullable: false),
                    KSensitivity = table.Column<double>(type: "numeric", nullable: false),
                    SclMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    InputHash = table.Column<string>(type: "char(64)", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MethodVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    DataVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisRuns_Areas_AoiId",
                        column: x => x.AoiId,
                        principalTable: "Areas",
                        principalColumn: "AoiId");
                    table.ForeignKey(
                        name: "FK_AnalysisRuns_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AnalysisRuns_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Observations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    AoiId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ObservedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CloudFraction = table.Column<double>(type: "numeric", nullable: true),
                    ValidFraction = table.Column<double>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Observations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Observations_Areas_AoiId",
                        column: x => x.AoiId,
                        principalTable: "Areas",
                        principalColumn: "AoiId");
                    table.ForeignKey(
                        name: "FK_Observations_SourceAssets_SourceAssetId",
                        column: x => x.SourceAssetId,
                        principalTable: "SourceAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BaselineResults",
                columns: table => new
                {
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    EBase = table.Column<double>(type: "numeric", nullable: false),
                    RValue = table.Column<double>(type: "numeric", nullable: false),
                    Unc = table.Column<double>(type: "numeric", nullable: false),
                    RAdj = table.Column<double>(type: "numeric", nullable: false),
                    ReserveB = table.Column<double>(type: "numeric", nullable: false),
                    QUnits = table.Column<int>(type: "integer", nullable: true),
                    QStatus = table.Column<string>(type: "text", nullable: false),
                    BlockReason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaselineResults", x => x.RunId);
                    table.ForeignKey(
                        name: "FK_BaselineResults_AnalysisRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "AnalysisRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CarbonMetrics",
                columns: table => new
                {
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalculatedAreaHa = table.Column<double>(type: "numeric", nullable: false),
                    CT0 = table.Column<double>(type: "numeric", nullable: false),
                    CT1 = table.Column<double>(type: "numeric", nullable: false),
                    DeltaC = table.Column<double>(type: "numeric", nullable: false),
                    EProj = table.Column<double>(type: "numeric", nullable: false),
                    EPerHaYear = table.Column<double>(type: "numeric", nullable: false),
                    LBound = table.Column<double>(type: "numeric", nullable: false),
                    UBound = table.Column<double>(type: "numeric", nullable: false),
                    HValue = table.Column<double>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarbonMetrics", x => x.RunId);
                    table.ForeignKey(
                        name: "FK_CarbonMetrics_AnalysisRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "AnalysisRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChangeZones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Geometry = table.Column<Geometry>(type: "geometry", nullable: true),
                    AreaHa = table.Column<double>(type: "numeric", nullable: false),
                    ContributionTc = table.Column<double>(type: "numeric", nullable: false),
                    CauseStatus = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeZones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChangeZones_AnalysisRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "AnalysisRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Format = table.Column<string>(type: "text", nullable: false),
                    Uri = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_AnalysisRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "AnalysisRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Risks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Message = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Risks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Risks_AnalysisRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "AnalysisRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioValuations",
                columns: table => new
                {
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceRub = table.Column<double>(type: "numeric", nullable: false),
                    TotalValueRub = table.Column<double>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioValuations", x => new { x.RunId, x.PriceRub });
                    table.ForeignKey(
                        name: "FK_ScenarioValuations_AnalysisRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "AnalysisRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "YearlySeries",
                columns: table => new
                {
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    CBarT = table.Column<double>(type: "numeric", nullable: false),
                    CT = table.Column<double>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YearlySeries", x => new { x.RunId, x.Year });
                    table.ForeignKey(
                        name: "FK_YearlySeries_AnalysisRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "AnalysisRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ZoneEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    EvidenceType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZoneEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZoneEvidence_ChangeZones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "ChangeZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ZoneEvidence_SourceAssets_SourceAssetId",
                        column: x => x.SourceAssetId,
                        principalTable: "SourceAssets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRuns_AoiId",
                table: "AnalysisRuns",
                column: "AoiId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRuns_ProjectId",
                table: "AnalysisRuns",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRuns_UserId",
                table: "AnalysisRuns",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_ParentAoiId",
                table: "Areas",
                column: "ParentAoiId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeZones_RunId",
                table: "ChangeZones",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_AoiId",
                table: "Observations",
                column: "AoiId");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_SourceAssetId",
                table: "Observations",
                column: "SourceAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Parameters_SourceId",
                table: "Parameters",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_AoiId",
                table: "Projects",
                column: "AoiId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OrganizationId",
                table: "Projects",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_RunId",
                table: "Reports",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_RunId",
                table: "Risks",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceAssets_AoiId",
                table: "SourceAssets",
                column: "AoiId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceAssets_SourceId",
                table: "SourceAssets",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_OrganizationId",
                table: "Users",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ZoneEvidence_SourceAssetId",
                table: "ZoneEvidence",
                column: "SourceAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ZoneEvidence_ZoneId",
                table: "ZoneEvidence",
                column: "ZoneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaselineResults");

            migrationBuilder.DropTable(
                name: "Baselines");

            migrationBuilder.DropTable(
                name: "CarbonMetrics");

            migrationBuilder.DropTable(
                name: "Observations");

            migrationBuilder.DropTable(
                name: "Parameters");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "Risks");

            migrationBuilder.DropTable(
                name: "ScenarioValuations");

            migrationBuilder.DropTable(
                name: "YearlySeries");

            migrationBuilder.DropTable(
                name: "ZoneEvidence");

            migrationBuilder.DropTable(
                name: "ChangeZones");

            migrationBuilder.DropTable(
                name: "SourceAssets");

            migrationBuilder.DropTable(
                name: "AnalysisRuns");

            migrationBuilder.DropTable(
                name: "Sources");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Areas");

            migrationBuilder.DropTable(
                name: "Organizations");
        }
    }
}
