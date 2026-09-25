using ShopDocsV2.Application;
using ShopDocsV2.Domain;

namespace ShopDocsV2.WinForms;

public partial class CatalogManagerForm : Form
{
    private readonly ICatalogRepository _catalogRepository;
    private readonly IPaintColorLookupService _paintColorLookupService;
    private readonly QuestionSet _questionSet;

    public CatalogManagerForm(ICatalogRepository catalogRepository, IPaintColorLookupService paintColorLookupService, QuestionSet questionSet)
    {
        _catalogRepository = catalogRepository;
        _paintColorLookupService = paintColorLookupService;
        _questionSet = questionSet;
        InitializeComponent();

        Load += async (_, _) => await LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        await materialsGrid.BindAsync(_catalogRepository, CatalogList.Materials);
        await pullsGrid.BindAsync(_catalogRepository, CatalogList.Pulls);
        await hardwareColorsGrid.BindAsync(_catalogRepository, CatalogList.HardwareColors);
        await finishesGrid.BindAsync(_catalogRepository, _paintColorLookupService);
        await countertopColorsGrid.BindAsync(_catalogRepository, _questionSet);
    }
}
