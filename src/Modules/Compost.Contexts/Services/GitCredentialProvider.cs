using System.Linq;
using System.Threading.Tasks;
using Compost.Contexts.Models;
using Compost.Core.Interfaces;
using Compost.Core.Models;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using YesSql;

namespace Compost.Contexts.Services;

public class GitCredentialProvider(IContentManager contentManager, ISession session) : IGitCredentialProvider
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

        return new GitCredential
        {
            PersonalAccessToken = part.PersonalAccessToken ?? string.Empty,
            AuthorName = part.AuthorName,
            AuthorEmail = part.AuthorEmail
        };
    }
}
