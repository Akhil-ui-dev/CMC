using Microsoft.AspNetCore.Components;
using Akhil.Proj.Web.Services;

namespace Akhil.Proj.Web.Components.Layout;

public partial class MainLayout : IDisposable
{
    [Inject]
    public AppStateService AppState { get; set; } = default!;

    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    private Notification? _activeAlert;
    private System.Threading.Timer? _alertTimer;

    protected override void OnInitialized()
    {
        AppState.OnChange += HandleStateChange;
        AppState.OnNotificationReceived += HandleNotification;
    }

    private void ToggleTheme()
    {
        AppState.ToggleTheme();
    }

    private void HandleStateChange() => InvokeAsync(StateHasChanged);

    private void HandleNotification(Notification notif)
    {
        InvokeAsync(() =>
        {
            _activeAlert = notif;
            StateHasChanged();
            
            // Clear alert after 6 seconds
            _alertTimer?.Dispose();
            _alertTimer = new System.Threading.Timer(_ =>
            {
                InvokeAsync(() =>
                {
                    _activeAlert = null;
                    StateHasChanged();
                });
            }, null, 6000, System.Threading.Timeout.Infinite);
        });
    }

    private void DismissAlert()
    {
        _activeAlert = null;
    }

    private void Logout()
    {
        AppState.LogOut();
        Navigation.NavigateTo("");
    }

    public void Dispose()
    {
        AppState.OnChange -= HandleStateChange;
        AppState.OnNotificationReceived -= HandleNotification;
        _alertTimer?.Dispose();
    }
}
