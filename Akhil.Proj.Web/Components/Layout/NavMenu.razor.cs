using Microsoft.AspNetCore.Components;
using Akhil.Proj.Web.Services;

namespace Akhil.Proj.Web.Components.Layout;

public partial class NavMenu : IDisposable
{
    [Inject]
    public AppStateService AppState { get; set; } = default!;

    protected override void OnInitialized()
    {
        AppState.OnChange += HandleStateChange;
    }

    private void HandleStateChange() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        AppState.OnChange -= HandleStateChange;
    }
}
