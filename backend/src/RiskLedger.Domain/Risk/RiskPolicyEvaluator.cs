using RiskLedger.Domain;

namespace RiskLedger.Domain.Risk;

public enum RiskOutcome { Approve, Review, Restrict }

/// <summary>Immutable policy version. Publishing a changed policy must create a new version.</summary>
public sealed record RiskPolicy(
    string PolicyId,
    int Version,
    decimal ReviewExposureThreshold,
    decimal RestrictExposureThreshold,
    bool RequireVerifiedOrganizer = true)
{
    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(PolicyId);
        if (Version < 1) throw new ArgumentOutOfRangeException(nameof(Version));
        if (ReviewExposureThreshold < 0m) throw new ArgumentOutOfRangeException(nameof(ReviewExposureThreshold));
        if (RestrictExposureThreshold < ReviewExposureThreshold)
            throw new ArgumentException("Restrict threshold must be greater than or equal to the review threshold.");
    }
}

public sealed record RiskAssessmentInput(
    string OrganizerId,
    bool IsVerified,
    Money UncoveredExposure,
    bool HasOpenHighSeverityDispute,
    DateTimeOffset AssessedAt);

public sealed record RiskAssessment(
    Guid Id,
    string OrganizerId,
    string PolicyId,
    int PolicyVersion,
    RiskOutcome Outcome,
    IReadOnlyList<string> ReasonCodes,
    Money UncoveredExposure,
    DateTimeOffset AssessedAt);

/// <summary>Deterministic policy evaluation; no score or AI output can override explicit rules.</summary>
public static class RiskPolicyEvaluator
{
    public static RiskAssessment Evaluate(RiskPolicy policy, RiskAssessmentInput input)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(input);
        policy.Validate();
        ArgumentException.ThrowIfNullOrWhiteSpace(input.OrganizerId);
        ArgumentNullException.ThrowIfNull(input.UncoveredExposure);
        if (input.UncoveredExposure.Amount < 0m)
            throw new ArgumentOutOfRangeException(nameof(input), "Uncovered exposure cannot be negative.");

        var reasons = new List<string>();
        var outcome = RiskOutcome.Approve;

        if (policy.RequireVerifiedOrganizer && !input.IsVerified)
        {
            outcome = RiskOutcome.Restrict;
            reasons.Add("ORGANIZER_NOT_VERIFIED");
        }

        if (input.HasOpenHighSeverityDispute)
        {
            outcome = RiskOutcome.Restrict;
            reasons.Add("OPEN_HIGH_SEVERITY_DISPUTE");
        }

        var exposure = input.UncoveredExposure.Amount;
        if (exposure >= policy.RestrictExposureThreshold)
        {
            outcome = RiskOutcome.Restrict;
            reasons.Add("EXPOSURE_ABOVE_RESTRICT_THRESHOLD");
        }
        else if (exposure >= policy.ReviewExposureThreshold && outcome != RiskOutcome.Restrict)
        {
            outcome = RiskOutcome.Review;
            reasons.Add("EXPOSURE_REQUIRES_REVIEW");
        }

        if (reasons.Count == 0) reasons.Add("POLICY_CHECKS_PASSED");

        return new RiskAssessment(Guid.NewGuid(), input.OrganizerId, policy.PolicyId, policy.Version,
            outcome, reasons.AsReadOnly(), input.UncoveredExposure, input.AssessedAt);
    }
}
