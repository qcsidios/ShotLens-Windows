using System.Globalization;
using System.Text.RegularExpressions;

namespace ShotLens.Windows.Core.Versioning;

public readonly record struct SemanticVersion : IComparable<SemanticVersion>
{
    private static readonly Regex Pattern = new(
        @"^v(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-beta\.(?<beta>[1-9]\d*))?$",
        RegexOptions.CultureInvariant);

    private SemanticVersion(int major, int minor, int patch, int? betaNumber)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        BetaNumber = betaNumber;
    }

    public int Major { get; }

    public int Minor { get; }

    public int Patch { get; }

    public int? BetaNumber { get; }

    public static SemanticVersion Parse(string value)
    {
        var match = Pattern.Match(value);
        if (!match.Success
            || !TryParsePart(match, "major", out var major)
            || !TryParsePart(match, "minor", out var minor)
            || !TryParsePart(match, "patch", out var patch))
        {
            throw new FormatException($"版本格式无效：{value}");
        }

        int? betaNumber = null;
        if (match.Groups["beta"].Success)
        {
            if (!TryParsePart(match, "beta", out var parsedBeta))
            {
                throw new FormatException($"版本格式无效：{value}");
            }

            betaNumber = parsedBeta;
        }

        return new SemanticVersion(major, minor, patch, betaNumber);
    }

    public int CompareTo(SemanticVersion other)
    {
        var numericComparison = Major.CompareTo(other.Major);
        if (numericComparison != 0)
        {
            return numericComparison;
        }

        numericComparison = Minor.CompareTo(other.Minor);
        if (numericComparison != 0)
        {
            return numericComparison;
        }

        numericComparison = Patch.CompareTo(other.Patch);
        if (numericComparison != 0)
        {
            return numericComparison;
        }

        return (BetaNumber, other.BetaNumber) switch
        {
            (null, null) => 0,
            (null, _) => 1,
            (_, null) => -1,
            ({ } left, { } right) => left.CompareTo(right)
        };
    }

    public override string ToString()
    {
        var stable = string.Create(
            CultureInfo.InvariantCulture,
            $"v{Major}.{Minor}.{Patch}");
        return BetaNumber is { } beta
            ? string.Create(CultureInfo.InvariantCulture, $"{stable}-beta.{beta}")
            : stable;
    }

    private static bool TryParsePart(Match match, string groupName, out int value) =>
        int.TryParse(
            match.Groups[groupName].Value,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out value);
}
