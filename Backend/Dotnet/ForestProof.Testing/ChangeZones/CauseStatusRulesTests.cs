namespace ForestProof.Testing.ChangeZones;

public sealed class CauseStatusRulesTests
{
    [Fact]
    public void Evaluate_WhenNoEvidence_ReturnsUnknown()
    {
        CauseStatusRules.Evaluate([], [], 2019, 2024).Should().Be(CauseStatus.Unknown);
    }

    [Fact]
    public void Evaluate_WhenOnlyGfc_ReturnsUnknown()
    {
        CauseStatusRules.Evaluate([EvidenceType.Gfc], [2020], 2019, 2024).Should().Be(CauseStatus.Unknown);
    }

    [Fact]
    public void Evaluate_WhenOneCauseSignal_ReturnsProbable()
    {
        CauseStatusRules.Evaluate([EvidenceType.Gfc, EvidenceType.Modis], [2021], 2019, 2024)
            .Should().Be(CauseStatus.Probable);
    }

    [Fact]
    public void Evaluate_WhenTwoCauseSignals_ReturnsConfirmed()
    {
        CauseStatusRules.Evaluate(
                [EvidenceType.Gfc, EvidenceType.Modis, EvidenceType.Sentinel2],
                [2021],
                2019,
                2024)
            .Should().Be(CauseStatus.Confirmed);
    }

    [Fact]
    public void Evaluate_WhenEvidenceOutsidePeriod_ReturnsUnknown()
    {
        CauseStatusRules.Evaluate(
                [EvidenceType.Gfc, EvidenceType.Modis, EvidenceType.Sentinel2],
                [2030],
                2019,
                2024)
            .Should().Be(CauseStatus.Unknown);
    }
}
