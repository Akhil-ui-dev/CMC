using Microsoft.AspNetCore.Components;
using Akhil.Proj.Web.Services;

namespace Akhil.Proj.Web.Components.Pages;

public partial class Registrations : IDisposable
{
    [Inject]
    public AppStateService AppState { get; set; } = default!;

    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    private string _searchQuery = "";

    protected override void OnInitialized()
    {
        AppState.OnChange += HandleStateChange;
    }

    private void HandleStateChange() => InvokeAsync(StateHasChanged);

    private List<Member> GetFilteredMembers()
    {
        if (string.IsNullOrWhiteSpace(_searchQuery))
        {
            return AppState.Members;
        }

        return AppState.Members.Where(m =>
            m.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
            m.Email.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
            m.Phone.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
            m.Address.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
            m.HowHeard.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    public void Dispose()
    {
        AppState.OnChange -= HandleStateChange;
    }
}
