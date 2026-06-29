namespace ShotLens.Windows.Core.Ocr.Benchmark;

public static class OcrBenchmarkScorer
{
    public static OcrBenchmarkScore Score(
        IReadOnlyList<string> expectedLines,
        IReadOnlyList<OcrTextBlock> actualBlocks)
    {
        if (expectedLines.Count == 0
            || expectedLines.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "期望行不能为空。",
                nameof(expectedLines));
        }

        var orderedBlocks = actualBlocks
            .OrderBy(block => block.Order)
            .ToArray();
        var expectedText = string.Concat(expectedLines);
        var actualText = string.Concat(
            orderedBlocks.Select(block => block.Text));
        var editDistance = LevenshteinDistance(expectedText, actualText);
        var matches = MatchLines(expectedLines, orderedBlocks);
        var missingLines = expectedLines
            .Where((_, index) => matches[index] is null)
            .ToArray();
        var matchedCount = expectedLines.Count - missingLines.Length;

        return new OcrBenchmarkScore(
            expectedText.Length,
            editDistance,
            (double)editDistance / expectedText.Length,
            (double)matchedCount / expectedLines.Count,
            PairwiseOrderAccuracy(matches),
            missingLines);
    }

    private static int?[] MatchLines(
        IReadOnlyList<string> expectedLines,
        IReadOnlyList<OcrTextBlock> actualBlocks)
    {
        var used = new HashSet<int>();
        var matches = new int?[expectedLines.Count];
        for (var expectedIndex = 0;
             expectedIndex < expectedLines.Count;
             expectedIndex++)
        {
            var expected = NormalizeLine(expectedLines[expectedIndex]);
            var bestSimilarity = 0d;
            int? bestIndex = null;
            for (var actualIndex = 0;
                 actualIndex < actualBlocks.Count;
                 actualIndex++)
            {
                if (used.Contains(actualIndex))
                {
                    continue;
                }

                var actual = NormalizeLine(actualBlocks[actualIndex].Text);
                var length = Math.Max(expected.Length, actual.Length);
                var similarity = length == 0
                    ? 1
                    : 1 - (double)LevenshteinDistance(expected, actual)
                        / length;
                if (similarity > bestSimilarity)
                {
                    bestSimilarity = similarity;
                    bestIndex = actualIndex;
                }
            }

            if (bestIndex is not null && bestSimilarity >= 0.5)
            {
                used.Add(bestIndex.Value);
                matches[expectedIndex] = bestIndex;
            }
        }

        return matches;
    }

    private static string NormalizeLine(string value) =>
        new(
            value
                .Where(character => !char.IsWhiteSpace(character))
                .Select(char.ToLowerInvariant)
                .ToArray());

    private static double PairwiseOrderAccuracy(int?[] matches)
    {
        var comparablePairs = 0;
        var correctPairs = 0;
        for (var left = 0; left < matches.Length; left++)
        {
            if (matches[left] is null)
            {
                continue;
            }

            for (var right = left + 1; right < matches.Length; right++)
            {
                if (matches[right] is null)
                {
                    continue;
                }

                comparablePairs++;
                if (matches[left] < matches[right])
                {
                    correctPairs++;
                }
            }
        }

        return comparablePairs == 0
            ? 1
            : (double)correctPairs / comparablePairs;
    }

    private static int LevenshteinDistance(string expected, string actual)
    {
        var previous = new int[actual.Length + 1];
        var current = new int[actual.Length + 1];
        for (var column = 0; column <= actual.Length; column++)
        {
            previous[column] = column;
        }

        for (var row = 1; row <= expected.Length; row++)
        {
            current[0] = row;
            for (var column = 1; column <= actual.Length; column++)
            {
                var substitutionCost =
                    expected[row - 1] == actual[column - 1] ? 0 : 1;
                current[column] = Math.Min(
                    Math.Min(
                        current[column - 1] + 1,
                        previous[column] + 1),
                    previous[column - 1] + substitutionCost);
            }

            (previous, current) = (current, previous);
        }

        return previous[actual.Length];
    }
}

public sealed record OcrBenchmarkScore(
    int ExpectedCharacterCount,
    int EditDistance,
    double CharacterErrorRate,
    double LineRecall,
    double ReadingOrderAccuracy,
    string[] MissingLines);
