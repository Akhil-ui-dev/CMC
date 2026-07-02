using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Akhil.Proj.Web.Services;

namespace Akhil.Proj.Web.Components.Pages;

public partial class Sermons : IDisposable
{
    [Inject]
    public AppStateService AppState { get; set; } = default!;

    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    private Sermon? _activeSermon;
    private bool _isAudioPlaying = false;
    private double _playbackProgress = 0;
    private int _playbackSeconds = 0;
    private System.Threading.Timer? _playbackTimer;

    // Notepad state
    private int _noteSermonId = 0;
    private string _currentNoteText = "";

    protected override void OnInitialized()
    {
        AppState.OnChange += HandleStateChange;
    }

    private void HandleStateChange() => InvokeAsync(StateHasChanged);

    private void SelectSermon(Sermon s)
    {
        StopPlayback();
        _activeSermon = s;
        _noteSermonId = s.Id;
        
        // Load note if already exists for this sermon
        if (AppState.UserNotes.ContainsKey(s.Id))
        {
            _currentNoteText = AppState.UserNotes[s.Id];
        }
        else
        {
            _currentNoteText = "";
        }
    }

    // Audio Playback Simulation
    private void ToggleAudioPlayback()
    {
        if (_activeSermon == null) return;
        _isAudioPlaying = !_isAudioPlaying;

        if (_isAudioPlaying)
        {
            _playbackTimer?.Dispose();
            _playbackTimer = new System.Threading.Timer(_ =>
            {
                InvokeAsync(() =>
                {
                    if (_activeSermon != null && _isAudioPlaying)
                    {
                        _playbackSeconds++;
                        // Extract mock duration length
                        var parts = _activeSermon.DurationText.Split(':');
                        int totalSec = int.Parse(parts[0]) * 60 + int.Parse(parts[1]);
                        
                        _playbackProgress = ((double)_playbackSeconds / totalSec) * 100;
                        
                        if (_playbackSeconds >= totalSec)
                        {
                            StopPlayback();
                        }
                        StateHasChanged();
                    }
                });
            }, null, 1000, 1000);
        }
        else
        {
            _playbackTimer?.Dispose();
        }
    }

    private void StopPlayback()
    {
        _isAudioPlaying = false;
        _playbackProgress = 0;
        _playbackSeconds = 0;
        _playbackTimer?.Dispose();
    }

    private string GetPlaybackTimeText()
    {
        int minutes = _playbackSeconds / 60;
        int seconds = _playbackSeconds % 60;
        return $"{minutes:D2}:{seconds:D2}";
    }

    private void SeekAudio(MouseEventArgs e)
    {
        // Simple seek mockup trigger (just sets progress to 45% for demo)
        _playbackProgress = 45;
        var parts = _activeSermon?.DurationText.Split(':') ?? new[]{"10", "00"};
        int totalSec = int.Parse(parts[0]) * 60 + int.Parse(parts[1]);
        _playbackSeconds = (int)(totalSec * 0.45);
    }

    // Note actions
    private void SaveNote()
    {
        if (AppState.CurrentUser != null && !string.IsNullOrWhiteSpace(_currentNoteText))
        {
            AppState.SaveSermonNote(_noteSermonId, _currentNoteText.Trim());
        }
    }

    private void LoadNote(int sermonId)
    {
        _noteSermonId = sermonId;
        if (AppState.UserNotes.ContainsKey(sermonId))
        {
            _currentNoteText = AppState.UserNotes[sermonId];
        }
    }

    private void DeleteNote(int sermonId)
    {
        AppState.DeleteSermonNote(sermonId);
        if (_noteSermonId == sermonId)
        {
            _currentNoteText = "";
        }
    }

    private void ExportNotesMock()
    {
        // Simple visual notify mockup
        AppState.BroadcastNotification("Export Successful", "Your sermon notes have been shared to your email address!");
    }

    public void Dispose()
    {
        AppState.OnChange -= HandleStateChange;
        _playbackTimer?.Dispose();
    }
}
