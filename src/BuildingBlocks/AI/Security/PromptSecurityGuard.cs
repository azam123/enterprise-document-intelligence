using System.Text.RegularExpressions;

namespace BuildingBlocks.AI.Security;

/// <summary>
/// Deterministic first-pass screening for untrusted user and retrieved-document text.
/// This is a defense-in-depth signal, not a substitute for model-side safeguards,
/// authorization, isolation, or human review.
/// </summary>
public sealed class PromptSecurityGuard
{
    private static readonly string[] InjectionPatterns =
    [
        @"ignore\s+(all\s+)?(previous|prior|above)\s+instructions",
        @"(reveal|print|show|expose)\s+(the\s+)?(system|developer)\s+prompt",
        @"you\s+are\s+now\s+(in\s+)?(developer|system|admin)\s+mode",
        @"(disregard|override)\s+(all\s+)?(safety|security|policy|guardrails)",
        @"exfiltrat(e|ion)\s+(the\s+)?(secret|credential|api\s+key)",
        @"call\s+the\s+tool\s+with\s+(admin|root|system)\s+privileges"
    ];

    private static readonly Regex InjectionRegex = new(
        string.Join("|", InjectionPatterns.Select(pattern => $"(?:{pattern})")),
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public PromptInspectionResult Inspect(string? untrustedText, int maxCharacters = 20_000)
    {
        if (maxCharacters <= 0)
            return PromptInspectionResult.Reject("The configured input limit must be positive.");

        if (string.IsNullOrWhiteSpace(untrustedText))
            return PromptInspectionResult.Reject("Input is empty.");

        if (untrustedText.Length > maxCharacters)
            return PromptInspectionResult.Reject($"Input exceeds the {maxCharacters} character limit.");

        if (untrustedText.Any(char.IsControl) && untrustedText.Any(c => char.IsControl(c) && c is not '\r' and not '\n' and not '\t'))
            return PromptInspectionResult.Reject("Input contains disallowed control characters.");

        if (InjectionRegex.IsMatch(untrustedText))
            return PromptInspectionResult.Reject("Input contains a prompt-injection pattern and requires review.");

        return PromptInspectionResult.Accept();
    }
}

public sealed record PromptInspectionResult(bool Allowed, string Reason)
{
    public static PromptInspectionResult Accept() => new(true, "Input passed deterministic screening.");
    public static PromptInspectionResult Reject(string reason) => new(false, reason);
}
