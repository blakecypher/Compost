using Compost.Contexts.Models;
using Compost.Core.Interfaces;
using Compost.Core.Models;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using YesSql;

namespace Compost.Contexts.Services;

public class GitCredentialProvider(
    IContentManager contentManager,
    ISession session,
    IGitSecretStore secretStore) : IGitCredentialProvider
{
    public async Task<GitCredential> GetDefaultCredentialAsync()
    {
        var contentItems = await session.Query<ContentItem, ContentItemIndex>()
            .Where(ci => ci.ContentType == "GitSettings" && ci.Published && ci.Latest)
            .ListAsync();

        var settingsItem = contentItems.FirstOrDefault();
        if (settingsItem == null) return new GitCredential();

        var part = settingsItem.As<GitSettingsPart>();
        if (part == null) return new GitCredential();

        // Retrieve the encrypted token from secure store (outside YesSql)
        var token = await secretStore.GetTokenAsync();

        return new GitCredential
        {
            PersonalAccessToken = token ?? string.Empty,
            AuthorName = part.AuthorName,
            AuthorEmail = part.AuthorEmail
        };
    }
}
