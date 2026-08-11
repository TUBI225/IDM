using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsDownloadManager.Domain.Downloads;

namespace WindowsDownloadManager.Domain.Tests;

[TestClass]
public sealed class SegmentPlannerTests
{
    [TestMethod]
    public void Plan_DividesLengthIntoBalancedContiguousSegments()
    {
        var segments = SegmentPlanner.Plan(10, 3);

        Assert.AreEqual(3, segments.Count);
        CollectionAssert.AreEqual(
            new[] { new DownloadSegment(0, 4), new DownloadSegment(4, 3), new DownloadSegment(7, 3) },
            segments.ToArray());
    }

    [TestMethod]
    public void Plan_WhenDivisible_ProducesEqualSegments()
    {
        var segments = SegmentPlanner.Plan(10, 2);

        CollectionAssert.AreEqual(
            new[] { new DownloadSegment(0, 5), new DownloadSegment(5, 5) },
            segments.ToArray());
    }

    [TestMethod]
    public void Plan_WithSingleSegment_ReturnsWholeFile()
    {
        var segments = SegmentPlanner.Plan(10, 1);

        CollectionAssert.AreEqual(new[] { new DownloadSegment(0, 10) }, segments.ToArray());
    }

    [TestMethod]
    public void Plan_WithMoreSegmentsThanBytes_ReturnsOneSegmentPerByte()
    {
        var segments = SegmentPlanner.Plan(5, 10);

        Assert.AreEqual(5, segments.Count);
        for (var index = 0; index < segments.Count; index++)
        {
            Assert.AreEqual(new DownloadSegment(index, 1), segments[index]);
        }
    }

    [TestMethod]
    public void Plan_ZeroLength_ReturnsNoSegments()
    {
        Assert.AreEqual(0, SegmentPlanner.Plan(0, 4).Count);
    }

    [TestMethod]
    public void Plan_RejectsNonPositiveSegmentCount()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SegmentPlanner.Plan(10, 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SegmentPlanner.Plan(10, -1));
    }

    [TestMethod]
    public void Plan_RejectsNegativeLength()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SegmentPlanner.Plan(-1, 2));
    }

    [TestMethod]
    public void Validate_RejectsDisjointSegments()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            SegmentPlanner.Validate([new DownloadSegment(0, 2), new DownloadSegment(5, 2)], 4));
    }

    [TestMethod]
    public void Validate_RejectsIncompleteCoverage()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            SegmentPlanner.Validate([new DownloadSegment(0, 2)], 4));
    }

    [TestMethod]
    public void Validate_RejectsEmptySegment()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            SegmentPlanner.Validate([new DownloadSegment(0, 0), new DownloadSegment(0, 4)], 4));
    }

    [TestMethod]
    public void Validate_AcceptsExactContiguousCoverage()
    {
        SegmentPlanner.Validate(
            [new DownloadSegment(0, 4), new DownloadSegment(4, 3), new DownloadSegment(7, 3)],
            10);
    }
}
