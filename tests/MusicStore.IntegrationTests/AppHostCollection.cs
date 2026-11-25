namespace MusicStore.IntegrationTests;

using Xunit;

/// <summary>
/// Single collection so the AppHost (and its PostgreSQL container) boots once
/// for the whole test run.
/// </summary>
[CollectionDefinition("AppHost")]
public sealed class AppHostCollection : ICollectionFixture<AppHostFixture>
{
}
