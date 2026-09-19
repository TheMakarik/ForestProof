namespace ForestProof.Testing.ChangeZones;

public sealed class CauseStatusRulesTests
{
    [Fact]
    public void Evaluate_WhenNoEvidence_ReturnsUnknown()
    {
        CauseStatusRules.Evaluate([]).Should().Be(CauseStatus.Unknown);
    }

    [Fact]
    public void Evaluate_WhenOnlyGfc_ReturnsUnknown()
    {
        CauseStatusRules.Evaluate([EvidenceType.Gfc]).Should().Be(CauseStatus.Unknown);
    }

    [Fact]
    public void Evaluate_WhenOneCauseSignal_ReturnsProbable()
    {
        CauseStatusRules.Evaluate([EvidenceType.Gfc, EvidenceType.Modis]).Should().Be(CauseStatus.Probable);
    }

    [Fact]
    public void Evaluate_WhenTwoCauseSignals_ReturnsConfirmed()
    {
        CauseStatusRules.Evaluate([EvidenceType.Gfc, EvidenceType.Modis, EvidenceType.Sentinel2])
            .Should().Be(CauseStatus.Confirmed);
    }
}
