using ADP.Portal.Api.Controllers;
using ADP.Portal.Core.Git.Services;
using AutoFixture;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADP.Portal.Api.Tests.Controllers;

[TestFixture]
public class FluxManifestControllerTests
{
    private Fixture fixture = null!;
    private IFluxManifestService fluxManifestService = null!;
    private ILogger<FluxManifestController> logger = null!;
    private FluxManifestController fluxManifestController = null!;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        fluxManifestService = Substitute.For<IFluxManifestService>();
        logger = Substitute.For<ILogger<FluxManifestController>>();
        fluxManifestController = new FluxManifestController(fluxManifestService, logger);
    }

    [Test]
    public async Task GetFluxServiceTemplateManifest_ReturnsBadRequest_WhenTemplateTypeIsInvalid()
    {
        // Arrange
        var templateType = fixture.Create<string>();

        // Act
        var result = await fluxManifestController.GetFluxServiceTemplateManifest(templateType);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetFluxServiceTemplateManifest_ReturnsOk_WhenTemplateTypeIsValid()
    {
        // Arrange
        var templateType = "Deploy";
        var patchValues = fixture.Create<Dictionary<object, object>?>();
        fluxManifestService.GetFluxServiceTemplatePatchValuesAsync(templateType.ToLower()).Returns(patchValues);

        // Act
        var result = await fluxManifestController.GetFluxServiceTemplateManifest(templateType);

        // Assert
        Assert.That(result,Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult?.Value, Is.EqualTo(patchValues));
    }
}
