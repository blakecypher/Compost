using System.Threading.Tasks;
using Compost.Kanban.Models;
using Compost.Kanban.ViewModels;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;

namespace Compost.Kanban.Drivers;

public class TreeNodePartDisplayDriver : ContentPartDisplayDriver<TreeNodePart>
{
    public override IDisplayResult Display(TreeNodePart part, BuildPartDisplayContext context)
    {
        return Initialize<TreeNodePartViewModel>(GetDisplayShapeType(context), m => BuildViewModel(m, part))
            .Location("Detail", "Content:5")
            .Location("Summary", "Content:5");
    }

    public override IDisplayResult Edit(TreeNodePart part, BuildPartEditorContext context)
    {
        return Initialize<TreeNodePartViewModel>(GetEditorShapeType(context), m => BuildViewModel(m, part));
    }

    public override async Task<IDisplayResult> UpdateAsync(TreeNodePart part, IUpdateModel updater, UpdatePartEditorContext context)
    {
        var model = new TreeNodePartViewModel();

        if (await updater.TryUpdateModelAsync(model, Prefix))
        {
            part.WorkContextId = model.WorkContextId;
            part.Complexity = model.Complexity;
            part.Priority = model.Priority;
            part.AcceptanceCriteria = model.AcceptanceCriteria;
            part.TechnicalRequirements = model.TechnicalRequirements;
            part.IsPromotedToKanban = model.IsPromotedToKanban;
        }

        return Edit(part, context);
    }

    private void BuildViewModel(TreeNodePartViewModel model, TreeNodePart part)
    {
        model.WorkContextId = part.WorkContextId;
        model.Complexity = part.Complexity;
        model.Priority = part.Priority;
        model.AcceptanceCriteria = part.AcceptanceCriteria;
        model.TechnicalRequirements = part.TechnicalRequirements;
        model.IsPromotedToKanban = part.IsPromotedToKanban;
        model.ContentItem = part.ContentItem;
    }
}
