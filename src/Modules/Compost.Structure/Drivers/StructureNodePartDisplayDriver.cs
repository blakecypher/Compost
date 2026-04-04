using System.Threading.Tasks;
using Compost.Structure.Models;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;

namespace Compost.Structure.Drivers;

public class StructureNodePartDisplayDriver : ContentPartDisplayDriver<StructureNodePart>
{
    public override IDisplayResult Display(StructureNodePart part, BuildPartDisplayContext context)
    {
        return View("StructureNodePart", part)
            .Location("Detail", "Content:5")
            .Location("Summary", "Content:5");
    }

    public override IDisplayResult Edit(StructureNodePart part, BuildPartEditorContext context)
    {
        return View("StructureNodePart_Edit", part)
            .Location("Primary", "Content:5");
    }

    public override async Task<IDisplayResult> UpdateAsync(StructureNodePart part, IUpdateModel updater, UpdatePartEditorContext context)
    {
        await updater.TryUpdateModelAsync(part, Prefix);
        return Edit(part, context);
    }
}
