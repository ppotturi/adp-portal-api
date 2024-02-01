using ADP.Portal.Api.Mapster;
using ADP.Portal.Core.Ado.Entities;
using AutoFixture;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using NSubstitute;
using NUnit.Framework;

namespace ADP.Portal.Api.Tests.Mapster
{
    [TestFixture]
    public class MapsterEntitiesConfigTests
    {
        private readonly IServiceCollection servicesMock;

        public MapsterEntitiesConfigTests()
        {
            servicesMock = Substitute.For<IServiceCollection>();
        }

        [Test]
        public void TestEntitiesConfigure()
        {
            // Arrange
            var fixture = new Fixture();
            var adoVariableGroup = fixture.Build<AdoVariableGroup>().Create();

            // Act
            servicesMock.EntitiesConfigure();
            var results = adoVariableGroup.Adapt<VariableGroupParameters>();

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.VariableGroupProjectReferences, Is.Not.Null);
            Assert.That(results.Variables, Is.Not.Null);
        }
    }
}