using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Akhil.Proj.Web.Services;

namespace Akhil.Proj.Web.Components.Pages;

public partial class Prayer : IDisposable
{
    [Inject]
    public AppStateService AppState { get; set; } = default!;

    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    private bool _showRequestModal = false;
    private int _expandedPrayerId = 0;
    private string _encouragementText = "";
    private int? _editingPrayerId = null;
    
    // Request input fields
    private string _reqTitle = "";
    private string _reqDetails = "";
    private bool _reqIsAnonymous = false;
    private string _reqFullName = "";
    private string _reqMobileNumber = "";

    protected override void OnInitialized()
    {
        AppState.OnChange += HandleStateChange;
    }

    private void HandleStateChange() => InvokeAsync(StateHasChanged);

    private bool HasPrayed(PrayerRequest prayer)
    {
        if (AppState.CurrentUser == null) return false;
        return prayer.PrayingUsers.Contains(AppState.CurrentUser.Email);
    }

    private void TogglePraying(PrayerRequest prayer)
    {
        if (AppState.CurrentUser == null)
        {
            Navigation.NavigateTo("auth");
            return;
        }
        AppState.TogglePraying(prayer.Id, AppState.CurrentUser.Email);
    }

    private void ToggleEncouragements(int prayerId)
    {
        if (_expandedPrayerId == prayerId)
        {
            _expandedPrayerId = 0;
        }
        else
        {
            _expandedPrayerId = prayerId;
            _encouragementText = "";
        }
    }

    private void HandleKeyUp(KeyboardEventArgs e, int prayerId)
    {
        if (e.Key == "Enter")
        {
            SubmitEncouragement(prayerId);
        }
    }

    private void SubmitEncouragement(int prayerId)
    {
        if (!string.IsNullOrWhiteSpace(_encouragementText) && AppState.CurrentUser != null)
        {
            string author = AppState.CurrentUser.Name;
            AppState.AddEncouragement(prayerId, author, _encouragementText.Trim());
            _encouragementText = "";
        }
    }

    // Modal Control
    private void OpenRequestModal()
    {
        _editingPrayerId = null;
        _reqTitle = "";
        _reqDetails = "";
        _reqIsAnonymous = false;
        _reqFullName = AppState.CurrentUser?.Name ?? "";
        _reqMobileNumber = AppState.CurrentUser?.Phone ?? "";
        _showRequestModal = true;
    }

    private void OpenEditModal(PrayerRequest prayer)
    {
        _editingPrayerId = prayer.Id;
        _reqTitle = prayer.Title;
        _reqDetails = prayer.Details;
        _reqIsAnonymous = prayer.IsAnonymous;
        _reqFullName = prayer.AuthorName;
        _reqMobileNumber = prayer.MobileNumber ?? "";
        _showRequestModal = true;
    }

    private void CloseRequestModal()
    {
        _showRequestModal = false;
    }

    private void DeleteRequest(int id)
    {
        AppState.DeletePrayerRequest(id);
    }

    private void SubmitRequest()
    {
        if (!string.IsNullOrWhiteSpace(_reqTitle) && 
            !string.IsNullOrWhiteSpace(_reqDetails) &&
            !string.IsNullOrWhiteSpace(_reqFullName) &&
            !string.IsNullOrWhiteSpace(_reqMobileNumber))
        {
            if (_editingPrayerId.HasValue)
            {
                AppState.UpdatePrayerRequest(_editingPrayerId.Value, _reqTitle.Trim(), _reqDetails.Trim(), _reqIsAnonymous, _reqFullName.Trim(), _reqMobileNumber.Trim());
            }
            else
            {
                AppState.AddPrayerRequest(_reqTitle.Trim(), _reqDetails.Trim(), _reqIsAnonymous, _reqFullName.Trim(), _reqMobileNumber.Trim());
            }
            _showRequestModal = false;
        }
    }

    public void Dispose()
    {
        AppState.OnChange -= HandleStateChange;
    }
}
