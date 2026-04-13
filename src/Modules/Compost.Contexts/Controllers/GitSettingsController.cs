using Compost.Contexts.Models;
using Compost.Contexts.Services;
using Compost.Contexts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using OrchardCore.DisplayManagement.Notify;
using YesSql;

namespace Compost.Contexts.Controllers;

[Authorize]
public class GitSettingsController(
    IContentManager contentManager,
    ISession session,
    INotifier notifier,
    IGitSecretStore secretStore) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var settingsItem = await GetSettingsItemAsync();
        var part = settingsItem.As<GitSettingsPart>() ?? new GitSettingsPart();

        // Retrieve the encrypted token from secure store (not from the content part)
        var token = await secretStore.GetTokenAsync();

        var model = new GitSettingsViewModel
        {
            PersonalAccessToken = token ?? string.Empty,
            AuthorName = part.AuthorName,
            AuthorEmail = part.AuthorEmail
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(GitSettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var settingsItem = await GetSettingsItemAsync();
        bool isNew = string.IsNullOrEmpty(settingsItem.ContentItemId);

        // Store sensitive token in secure store (encrypted, outside YesSql)
        await secretStore.SetTokenAsync(model.PersonalAccessToken);

        // Store non-sensitive data in the content part (serialized to YesSql)
        settingsItem.Alter<GitSettingsPart>(part =>
        {
            part.AuthorName = model.AuthorName;
            part.AuthorEmail = model.AuthorEmail;
        });

        if (isNew)
        {
            await contentManager.CreateAsync(settingsItem, VersionOptions.Published);
        }
        else
        {
            await contentManager.UpdateAsync(settingsItem);
            await contentManager.PublishAsync(settingsItem);
        }

        await notifier.SuccessAsync(new LocalizedHtmlString("GitSettingsUpdated", "Git settings updated successfully."));

        return RedirectToAction(nameof(Edit));
    }

    private async Task<ContentItem> GetSettingsItemAsync()
    {
        var contentItems = await session.Query<ContentItem, ContentItemIndex>()
            .Where(ci => ci.ContentType == "GitSettings" && ci.Published && ci.Latest)
            .ListAsync();

        var item = contentItems.FirstOrDefault();

        if (item == null)
        {
            item = await contentManager.NewAsync("GitSettings");
            item.DisplayText = "Global Git Settings";
        }

        return item;
    }
}
