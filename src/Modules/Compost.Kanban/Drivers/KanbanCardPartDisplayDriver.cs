using System.Threading.Tasks;
using Compost.Kanban.Models;
using Compost.Kanban.ViewModels;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;

namespace Compost.Kanban.Drivers;

public class KanbanCardPartDisplayDriver : ContentPartDisplayDriver<KanbanCardPart>
{
    public override IDisplayResult Display(KanbanCardPart part, BuildPartDisplayContext context)
    {
        return Initialize<KanbanCardPartViewModel>(GetDisplayShapeType(context), m => BuildViewModel(m, part))
            .Location("Detail", "Content:5")
            .Location("Summary", "Content:5");
    }

    public override IDisplayResult Edit(KanbanCardPart part, BuildPartEditorContext context)
    {
        return Initialize<KanbanCardPartViewModel>(GetEditorShapeType(context), m => BuildViewModel(m, part));
    }

    public override async Task<IDisplayResult> UpdateAsync(KanbanCardPart part, IUpdateModel updater, UpdatePartEditorContext context)
    {
        var model = new KanbanCardPartViewModel();

        if (await updater.TryUpdateModelAsync(model, Prefix))
        {
            part.Status = model.Status;
            part.StoryPoints = model.StoryPoints;
            part.WorkContextId = model.WorkContextId;
            part.IsBlocked = model.IsBlocked;
            part.BlockedReason = model.BlockedReason;
        }

        return Edit(part, context);
    }

    private void BuildViewModel(KanbanCardPartViewModel model, KanbanCardPart part)
    {
        model.Status = part.Status;
        model.StoryPoints = part.StoryPoints;
        model.WorkContextId = part.WorkContextId;
        model.IsBlocked = part.IsBlocked;
        model.BlockedReason = part.BlockedReason;
        model.ContentItem = part.ContentItem;
    }
}
