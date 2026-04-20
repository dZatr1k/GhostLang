using GhostLang.Core.Benchmark;

namespace GhostLang.Tests.Benchmark;

public class MetricsTests
{
    [Fact]
    public void LevenshteinDistance_IdenticalStrings_Zero()
    {
        Assert.Equal(0, Metrics.LevenshteinDistance("hello", "hello"));
        Assert.Equal(0, Metrics.LevenshteinDistance("", ""));
    }

    [Fact]
    public void LevenshteinDistance_EmptyString_ReturnsOtherLength()
    {
        Assert.Equal(5, Metrics.LevenshteinDistance("hello", ""));
        Assert.Equal(5, Metrics.LevenshteinDistance("", "hello"));
    }

    [Theory]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("hello", "hallo", 1)]
    [InlineData("hello", "helo", 1)]
    [InlineData("hello", "helloo", 1)]
    [InlineData("abc", "xyz", 3)]
    public void LevenshteinDistance_KnownPairs(string a, string b, int expected)
    {
        Assert.Equal(expected, Metrics.LevenshteinDistance(a, b));
    }

    [Fact]
    public void LevenshteinDistance_Unicode_CjkSupported()
    {
        Assert.Equal(0, Metrics.LevenshteinDistance("こんにちは", "こんにちは"));
        Assert.Equal(1, Metrics.LevenshteinDistance("こんにちは", "こんばちは"));
    }

    [Fact]
    public void CharacterErrorRate_Perfect_Zero()
    {
        Assert.Equal(0.0, Metrics.CharacterErrorRate("hello", "hello"));
    }

    [Theory]
    [InlineData("hallo", "hello", 0.2)]
    [InlineData("hel", "hello", 0.4)]
    [InlineData("world", "hello", 0.8)]
    [InlineData("xyz", "hello", 1.0)]
    public void CharacterErrorRate_KnownPairs(string predicted, string reference, double expected)
    {
        Assert.Equal(expected, Metrics.CharacterErrorRate(predicted, reference), precision: 3);
    }

    [Fact]
    public void CharacterErrorRate_EmptyReference_HandledGracefully()
    {
        Assert.Equal(0.0, Metrics.CharacterErrorRate("", ""));
        Assert.Equal(1.0, Metrics.CharacterErrorRate("hello", ""));
    }

    [Theory]
    [InlineData("hello world", "hello world", 0.0)]
    [InlineData("hello friend", "hello world", 0.5)]
    [InlineData("the cat sat on the mat", "the cat sat on a mat", 1.0 / 6)]
    public void WordErrorRate_KnownPairs(string predicted, string reference, double expected)
    {
        Assert.Equal(expected, Metrics.WordErrorRate(predicted, reference), precision: 3);
    }

    [Fact]
    public void WordErrorRate_HandlesMultipleWhitespace()
    {
        var wer = Metrics.WordErrorRate("hello   world", "hello world");
        Assert.Equal(0.0, wer);
    }

    [Fact]
    public void BoundingBoxIoU_Identical_One()
    {
        var iou = Metrics.BoundingBoxIoU(0, 0, 100, 100, 0, 0, 100, 100);
        Assert.Equal(1.0, iou, precision: 3);
    }

    [Fact]
    public void BoundingBoxIoU_Disjoint_Zero()
    {
        var iou = Metrics.BoundingBoxIoU(0, 0, 50, 50, 100, 100, 50, 50);
        Assert.Equal(0.0, iou, precision: 3);
    }

    [Fact]
    public void BoundingBoxIoU_HalfOverlap()
    {

        var iou = Metrics.BoundingBoxIoU(0, 0, 100, 100, 50, 0, 100, 100);
        Assert.Equal(1.0 / 3, iou, precision: 3);
    }

    [Fact]
    public void BoundingBoxIoU_ZeroSize_Zero()
    {
        Assert.Equal(0.0, Metrics.BoundingBoxIoU(0, 0, 0, 100, 0, 0, 100, 100));
    }

    [Fact]
    public void Bleu4_PerfectMatch_One()
    {
        var score = Metrics.Bleu4("the cat sat on the mat", "the cat sat on the mat");
        Assert.Equal(1.0, score, precision: 3);
    }

    [Fact]
    public void Bleu4_NoMatch_Low()
    {

        var score = Metrics.Bleu4("foo bar baz qux", "the cat sat on");
        Assert.InRange(score, 0.0, 0.3);
    }

    [Fact]
    public void Bleu4_PartialMatch_BetweenZeroAndOne()
    {

        var score = Metrics.Bleu4("the cat on the mat", "the cat sat on the mat");
        Assert.InRange(score, 0.01, 0.99);
    }

    [Fact]
    public void ChrF_PerfectMatch_One()
    {
        var score = Metrics.ChrF("hello world", "hello world");
        Assert.Equal(1.0, score, precision: 3);
    }

    [Fact]
    public void ChrF_CompletelyDifferent_Low()
    {
        var score = Metrics.ChrF("zzzzzzzzz", "aaaaaaaaaa");
        Assert.InRange(score, 0.0, 0.1);
    }

    [Fact]
    public void ChrF_MorphologyCloseForms_NonZero()
    {

        var score = Metrics.ChrF("running fast", "runs fast");
        Assert.InRange(score, 0.3, 1.0);
    }

    [Fact]
    public void ChrF_EmptyStrings_HandledGracefully()
    {
        Assert.Equal(1.0, Metrics.ChrF("", ""));
        Assert.Equal(0.0, Metrics.ChrF("hello", ""));
        Assert.Equal(0.0, Metrics.ChrF("", "hello"));
    }
}
