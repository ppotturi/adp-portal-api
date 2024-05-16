using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Services;
using AutoFixture;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace ADP.Portal.Core.Tests.Git.Services;

[TestFixture]
public class FluxManifestServiceTests
{
    private Fixture fixture = null!;
    private IFluxTemplateService fluxTemplateService = null!;
    private ILogger<FluxManifestService> logger = null!;
    private FluxManifestService fluxManifestService = null!;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        fluxTemplateService = Substitute.For<IFluxTemplateService>();
        logger = Substitute.For<ILogger<FluxManifestService>>();
        fluxManifestService = new FluxManifestService(fluxTemplateService, logger);
    }

    [Test]
    public async Task GetFluxServiceTemplatePatchValuesAsync_ReturnsValues_WhenTemplateExists()
    {
        // Arrange
        var templateType = fixture.Create<string>();
        var path = $"{Constants.Flux.Templates.SERVICE_FOLDER}/{templateType}/environment/patch.yaml";
        var file = new Dictionary<object, object>
            {
                {
                    Constants.Flux.Templates.SPEC_KEY, new Dictionary<object, object>
                    {
                        {
                            Constants.Flux.Templates.VALUES_KEY, new Dictionary<object, object>
                            {
                                { Constants.Flux.Templates.POSTGRESRESOURCEGROUPNAME_KEY, "group" },
                                { Constants.Flux.Templates.POSTGRESSERVERNAME_KEY, "server" }
                            }
                        }
                    }
                }
            };
        fluxTemplateService.GetFluxTemplateAsync(path).Returns(new FluxTemplateFile(file));

        // Act
        var result = await fluxManifestService.GetFluxServiceTemplatePatchValuesAsync(templateType);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task GetFluxServiceTemplatePatchValuesAsync_ReturnsNull_WhenTemplateDoesNotExist()
    {
        // Arrange
        var templateType = fixture.Create<string>();
        var path = $"{Constants.Flux.Templates.SERVICE_FOLDER}/{templateType}/environment/patch.yaml";
        fluxTemplateService.GetFluxTemplateAsync(path).Returns((FluxTemplateFile?)null);

        // Act
        var result = await fluxManifestService.GetFluxServiceTemplatePatchValuesAsync(templateType);

        // Assert
        Assert.That(result, Is.Null);
    }

}
