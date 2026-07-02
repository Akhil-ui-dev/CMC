using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Akhil.Proj.Web.Services;

namespace Akhil.Proj.Web.Components.Pages;

public partial class Events : IDisposable
{
    [Inject]
    public AppStateService AppState { get; set; } = default!;

    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    [Inject]
    public IJSRuntime JS { get; set; } = default!;

    private readonly List<string> _volunteerRoles = new() { "Greeter", "Audio Visual", "Kids Helper" };
    private bool _showEventModal = false;
    private ChurchEvent? _editingEvent;
    
    private string _eventTitle = "";
    private string _eventDateText = "";
    private string _eventDescription = "";

    private void OpenCreateEventModal()
    {
        _editingEvent = null;
        _eventTitle = "";
        _eventDateText = "";
        _eventDescription = "";
        _showEventModal = true;
    }

    private void OpenEditEventModal(ChurchEvent ev)
    {
        _editingEvent = ev;
        _eventTitle = ev.Title;
        _eventDateText = ev.DateText;
        _eventDescription = ev.Description;
        _showEventModal = true;
    }

    private void CloseEventModal()
    {
        _showEventModal = false;
        _editingEvent = null;
    }

    private void SubmitForm()
    {
        if (string.IsNullOrWhiteSpace(_eventTitle) || string.IsNullOrWhiteSpace(_eventDescription) || string.IsNullOrWhiteSpace(_eventDateText)) return;

        if (_editingEvent == null)
        {
            AppState.AddChurchEvent(_eventTitle.Trim(), _eventDescription.Trim(), _eventDateText.Trim());
        }
        else
        {
            AppState.UpdateChurchEvent(_editingEvent.Id, _eventTitle.Trim(), _eventDescription.Trim(), _eventDateText.Trim());
        }

        _showEventModal = false;
        _editingEvent = null;
    }

    private async Task HandleDeleteEvent(ChurchEvent ev)
    {
        bool confirmed = await JS.InvokeAsync<bool>("confirm", $"Are you sure you want to delete the event '{ev.Title}'? This will also purge all associated RSVPs and volunteer lists.");
        if (confirmed)
        {
            AppState.DeleteChurchEvent(ev.Id);
        }
    }

    protected override void OnInitialized()
    {
        AppState.OnChange += HandleStateChange;
    }

    private void HandleStateChange() => InvokeAsync(StateHasChanged);

    private int GetYesCount(ChurchEvent ev)
    {
        return ev.RSVPs.Values.Count(v => v == "Yes");
    }

    private int GetNoCount(ChurchEvent ev)
    {
        return ev.RSVPs.Values.Count(v => v == "No");
    }

    private string? _guestEmail;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _guestEmail = await JS.InvokeAsync<string>("localStorage.getItem", "church_guest_email");
                if (string.IsNullOrEmpty(_guestEmail))
                {
                    _guestEmail = "guest_" + Guid.NewGuid().ToString("N") + "@church.guest";
                    await JS.InvokeVoidAsync("localStorage.setItem", "church_guest_email", _guestEmail);
                }
                StateHasChanged();
            }
            catch
            {
                if (string.IsNullOrEmpty(_guestEmail))
                {
                    _guestEmail = "guest_temp_" + Guid.NewGuid().ToString("N") + "@church.guest";
                }
            }
        }
    }

    private string GetRSVPStatus(ChurchEvent ev)
    {
        var email = AppState.CurrentUser?.Email ?? _guestEmail;
        if (!string.IsNullOrEmpty(email) && ev.RSVPs.ContainsKey(email))
        {
            return ev.RSVPs[email];
        }
        return "";
    }

    private void SetRSVP(int eventId, string status)
    {
        var email = AppState.CurrentUser?.Email ?? _guestEmail;
        if (string.IsNullOrEmpty(email))
        {
            _guestEmail = "guest_lazy_" + Guid.NewGuid().ToString("N") + "@church.guest";
            email = _guestEmail;
        }
        AppState.ToggleRSVP(eventId, email, status);
    }

    private string? GetVolunteerForRole(ChurchEvent ev, string role)
    {
        return ev.Volunteers.FirstOrDefault(x => x.Value == role).Key;
    }

    private string GetVolunteerName(string email)
    {
        var member = AppState.Members.FirstOrDefault(m => m.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (member != null) return member.Name;
        return email.Split('@')[0];
    }

    private void AcceptVolunteer(int eventId, string role)
    {
        if (AppState.CurrentUser != null)
        {
            AppState.SignUpAsVolunteer(eventId, AppState.CurrentUser.Email, role);
        }
    }

    private void CancelVolunteer(int eventId)
    {
        if (AppState.CurrentUser != null)
        {
            AppState.RemoveVolunteer(eventId, AppState.CurrentUser.Email);
        }
    }

    public void Dispose()
    {
        AppState.OnChange -= HandleStateChange;
    }
}
