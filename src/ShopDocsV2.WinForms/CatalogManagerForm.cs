using ShopDocsV2.Application;

namespace ShopDocsV2.WinForms;

public partial class CatalogManagerForm : Form
{
    private readonly ICatalogRepository _catalogRepository;
    private readonly IPaintColorLookupService _paintColorLookupService;

    public CatalogManagerForm(ICatalogRepository catalogRepository, IPaintColorLookupService paintColorLookupService)
    {
        _catalogRepository = catalogRepository;
        _paintColorLookupService = paintColorLookupService;
        InitializeComponent();

        Load += async (_, _) => await LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        var repo = _catalogRepository;

        await materialsGrid.BindAsync(
            async ct => (await repo.GetMaterialsAsync(ct)).Select(m => (m.Id, m.Name)).ToList(),
            repo.AddMaterialAsync, repo.UpdateMaterialAsync, repo.DeleteMaterialAsync);

        await pullsGrid.BindAsync(
            async ct => (await repo.GetPullsAsync(ct)).Select(p => (p.Id, p.Name)).ToList(),
            repo.AddPullAsync, repo.UpdatePullAsync, repo.DeletePullAsync);

        await hardwareColorsGrid.BindAsync(
            async ct => (await repo.GetHardwareColorsAsync(ct)).Select(h => (h.Id, h.Name)).ToList(),
            repo.AddHardwareColorAsync, repo.UpdateHardwareColorAsync, repo.DeleteHardwareColorAsync);

        await hingesGrid.BindAsync(
            async ct => (await repo.GetHingesAsync(ct)).Select(h => (h.Id, h.Name)).ToList(),
            repo.AddHingeAsync, repo.UpdateHingeAsync, repo.DeleteHingeAsync);

        await guidesGrid.BindAsync(
            async ct => (await repo.GetGuidesAsync(ct)).Select(g => (g.Id, g.Name)).ToList(),
            repo.AddGuideAsync, repo.UpdateGuideAsync, repo.DeleteGuideAsync);

        await finishesGrid.BindAsync(repo, _paintColorLookupService);
        await countertopColorsGrid.BindAsync(repo);
    }
}
