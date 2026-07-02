using Microsoft.AspNetCore.Components;
using Akhil.Proj.Web.Services;

namespace Akhil.Proj.Web.Components.Pages;

public partial class Home : IDisposable
{
    [Inject]
    public AppStateService AppState { get; set; } = default!;

    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    private string _activeTab = "times";
    private string _contactName = "";
    private string _contactEmail = "";
    private string _contactMessage = "";
    private bool _formSubmitted = false;

    protected override void OnInitialized()
    {
        AppState.OnChange += HandleAppStateChange;
        UpdateActiveTab();
    }

    private void HandleAppStateChange()
    {
        InvokeAsync(() =>
        {
            UpdateActiveTab();
            StateHasChanged();
        });
    }

    private void UpdateActiveTab()
    {
        if (AppState.CurrentUser != null && _activeTab == "times")
        {
            _activeTab = "dashboard";
        }
        else if (AppState.CurrentUser == null && _activeTab == "dashboard")
        {
            _activeTab = "times";
        }
    }

    private void SetActiveTab(string tab)
    {
        if (tab == "dashboard" && AppState.CurrentUser == null)
        {
            _activeTab = "times";
        }
        else
        {
            _activeTab = tab;
        }
    }

    private void ScrollToServiceTimes()
    {
        SetActiveTab("times");
    }

    private void SubmitForm()
    {
        if (!string.IsNullOrWhiteSpace(_contactName) && !string.IsNullOrWhiteSpace(_contactEmail))
        {
            // Simulate processing
            _formSubmitted = true;
        }
    }

    private void ResetForm()
    {
        _contactName = "";
        _contactEmail = "";
        _contactMessage = "";
        _formSubmitted = false;
    }

    public void Dispose()
    {
        AppState.OnChange -= HandleAppStateChange;
    }
}
