using ADP.Portal.Core.Git.Entities;
using ADP.Portal.Core.Git.Infrastructure;
using ADP.Portal.Core.Git.Services;
using AutoFixture;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace ADP.Portal.Core.Tests.Git.Services;

[TestFixture]
public class FluxTemplateServiceTests
{
    private ICacheService cacheService = null!;
    private IGitHubRepository gitHubRepository = null!;
    private FluxTemplateService fluxTemplateService = null!;
    private ILogger<FluxTemplateService> logger = null!;
    private readonly IOptionsSnapshot<GitRepo> gitRepoOptions = Substitute.For<IOptionsSnapshot<GitRepo>>();
    private GitRepo fluxTemplateRepo = null!;
    private Fixture fixture = null!;
    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        cacheService = Substitute.For<ICacheService>();
        gitHubRepository = Substitute.For<IGitHubRepository>();
        logger = Substitute.For<ILogger<FluxTemplateService>>();
        fluxTemplateRepo = fixture.Build<GitRepo>().Create();
        gitRepoOptions.Get(Constants.GitRepo.TEAM_FLUX_TEMPLATES_CONFIG).Returns(fluxTemplateRepo);
        fluxTemplateService = new FluxTemplateService(gitHubRepository, gitRepoOptions, cacheService, logger);
    }

    [Test]
    public async Task GetFluxTemplatesAsync_ReturnsTemplatesFromCache_WhenTemplatesAreCached()
    {
        // Arrange
        var cachedTemplates = fixture.CreateMany<KeyValuePair<string, FluxTemplateFile>>(5).ToList();
        cacheService.Get<IEnumerable<KeyValuePair<string, FluxTemplateFile>>>(Arg.Any<string>()).Returns(cachedTemplates);

        // Act
        var result = await fluxTemplateService.GetFluxTemplatesAsync();

        // Assert
        Assert.That(result, Is.EqualTo(cachedTemplates));
        await gitHubRepository.DidNotReceive().GetAllFilesAsync(Arg.Any<GitRepo>(), Arg.Any<string>());
    }

    [Test]
    public async Task GetFluxTemplatesAsync_ReturnsTemplatesFromGitHub_WhenTemplatesAreNotCached()
    {
        // Arrange
        var templates = fixture.CreateMany<KeyValuePair<string, FluxTemplateFile>>(5).ToList();
        cacheService.Get<IEnumerable<KeyValuePair<string, FluxTemplateFile>>>(Arg.Any<string>()).Returns((List<KeyValuePair<string, FluxTemplateFile>>?)null);
        gitHubRepository.GetAllFilesAsync(Arg.Any<GitRepo>(), Arg.Any<string>()).Returns(templates);

        // Act
        var result = await fluxTemplateService.GetFluxTemplatesAsync();

        // Assert
        Assert.That(result, Is.EqualTo(templates));
        cacheService.Received().Set(Arg.Any<string>(), templates);
    }

    [Test]
    public async Task GetFluxTemplateAsync_ReturnsTemplate_WhenTemplateExists()
    {
        // Arrange
        var path = fixture.Create<string>();
        var templates = fixture.CreateMany<KeyValuePair<string, FluxTemplateFile>>(5).ToList();
        templates.Add(new KeyValuePair<string, FluxTemplateFile>(path, fixture.Create<FluxTemplateFile>()));
        cacheService.Get<IEnumerable<KeyValuePair<string, FluxTemplateFile>>>(Arg.Any<string>()).Returns(templates);

        // Act
        var result = await fluxTemplateService.GetFluxTemplateAsync(path);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(templates.First(t => t.Key == path).Value));
    }

    [Test]
    public async Task GetFluxTemplateAsync_ReturnsNull_WhenTemplateDoesNotExist()
    {
        // Arrange
        var path = fixture.Create<string>();
        var templates = fixture.CreateMany<KeyValuePair<string, FluxTemplateFile>>(5).ToList();
        cacheService.Get<IEnumerable<KeyValuePair<string, FluxTemplateFile>>>(Arg.Any<string>()).Returns(templates);

        // Act
        var result = await fluxTemplateService.GetFluxTemplateAsync(path);

        // Assert
        Assert.That(result, Is.Null);
    }
}
