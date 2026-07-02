using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Akhil.Proj.Web.Services;

namespace Akhil.Proj.Web.Components.Pages;

public partial class Admin : IDisposable
{
    [Inject]
    public AppStateService AppState { get; set; } = default!;

    [Inject]
    public IJSRuntime JS { get; set; } = default!;

    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    private string _searchQuery = "";
    
    // Broadcast notification fields
    private string _broadcastTitle = "";
    private string _broadcastMessage = "";
    private bool _broadcastSuccess = false;

    protected override void OnInitialized()
    {
        AppState.OnChange += HandleStateChange;
    }

    private void HandleStateChange() => InvokeAsync(StateHasChanged);

    private void LoginAsAdminDemo()
    {
        AppState.LogIn("admin@example.com", "admin");
    }

    private int GetTotalRSVPs()
    {
        return AppState.Events.Sum(e => e.RSVPs.Count);
    }

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
            m.HowHeard.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    // Export Excel / CSV file dynamically to user browser
    private async Task ExportMembersToCSV()
    {
        // Generate CSV Content string
        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine("Name,Email,Phone,Family Members,Discovery Source,Joined Date,Interests");

        foreach (var member in AppState.Members)
        {
            var interests = string.Join(";", member.Interests);
            csvBuilder.AppendLine($"\"{member.Name}\",\"{member.Email}\",\"{member.Phone}\",\"{member.FamilyMembers}\",\"{member.HowHeard}\",\"{member.JoinedDate:yyyy-MM-dd}\",\"{interests}\"");
        }

        var csvContent = csvBuilder.ToString();
        
        // Trigger file download using JS Interop utility in App.razor
        await JS.InvokeVoidAsync("downloadFileFromStream", "registered_members.csv", csvContent);
    }

    private void SendBroadcast()
    {
        if (!string.IsNullOrWhiteSpace(_broadcastTitle) && !string.IsNullOrWhiteSpace(_broadcastMessage))
        {
            AppState.BroadcastNotification(_broadcastTitle, _broadcastMessage);
            _broadcastTitle = "";
            _broadcastMessage = "";
            _broadcastSuccess = true;

            // Clear success banner
            var timer = new System.Timers.Timer(5000);
            timer.Elapsed += (s, e) =>
            {
                timer.Stop();
                timer.Dispose();
                _broadcastSuccess = false;
                InvokeAsync(StateHasChanged);
            };
            timer.Start();
        }
    }

    // Photo moderation helpers
    private void ApprovePhoto(int id)
    {
        AppState.ApproveGalleryImage(id);
    }

    private void RejectPhoto(int id)
    {
        AppState.RejectGalleryImage(id);
    }

    public void Dispose()
    {
        AppState.OnChange -= HandleStateChange;
    }
}
