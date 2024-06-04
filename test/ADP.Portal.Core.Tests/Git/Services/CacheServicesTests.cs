using ADP.Portal.Core.Git.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using NUnit.Framework;

namespace ADP.Portal.Core.Tests.Git.Services;

[TestFixture]
public class CacheServicesTests
{
    private TimeProvider Clock { get; set; } = null!;
    private IMemoryCache Cache { get; set; } = null!;
    private CacheService Sut { get; set; } = null!;

    [SetUp]
    public void Setup()
    {
        Clock = Substitute.For<TimeProvider>();
        Cache = Substitute.For<IMemoryCache>();
        Sut = new CacheService(Clock, Cache);
    }

    [Test]
    public void Set_ExpiresAtMidnight()
    {
        // arrange
        Clock.GetUtcNow().Returns(new DateTimeOffset(2024, 06, 04, 09, 44, 47, TimeSpan.Zero));
        var key = Guid.NewGuid().ToString();
        var value = new TestRecord(Guid.NewGuid());
        var expiration = new TimeSpan(14, 15, 13);
        var cacheEntry = Substitute.For<ICacheEntry>();

        Cache.CreateEntry(key).Returns(cacheEntry);

        // act
        Sut.Set(key, value);

        // assert
        Cache.Received(1).CreateEntry(key);
        cacheEntry.Received(1).AbsoluteExpirationRelativeToNow = expiration;
        cacheEntry.Received(1).Value = value;
    }
    [Test]
    public void Get_ReturnsTheCorrectValue()
    {
        // arrange
        var key = Guid.NewGuid().ToString();
        var value = new TestRecord(Guid.NewGuid());

        Cache.TryGetValue(key, out Arg.Any<ICacheEntry>()!).Returns(x =>
        {
            x[1] = value;
            return true;
        });

        // act
        var actual = Sut.Get<TestRecord>(key);

        // assert
        actual.Should().Be(value);
        Cache.Received(1).TryGetValue(key, out Arg.Any<ICacheEntry>()!);
    }

    private record TestRecord(Guid Id);
}
