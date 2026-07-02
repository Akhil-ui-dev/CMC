using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Akhil.Proj.Web.Services;

namespace Akhil.Proj.Web.Components.Pages
{
    public partial class Live : IDisposable
    {
        private string _activeTab = "live";
        private string _iframeSrc = "https://www.youtube.com/embed/live_stream?channel=UCQkJ2iQtZ9lGxDlTKjDz_SA&autoplay=1&mute=1";
        private string _guestNickname = string.Empty;
        private string _messageText = string.Empty;
        private int _mockViewerCount = 124;
        private readonly List<LiveReaction> _activeReactions = new();
        private System.Timers.Timer? _viewerTimer;

        protected override void OnInitialized()
        {
            // Subscribe to state change notifications and global reaction signals
            AppState.OnChange += OnAppStateChanged;
            AppState.OnReactionReceived += OnAppStateReactionReceived;

            // Generate guest nickname
            if (AppState.CurrentUser == null)
            {
                var rand = new Random();
                _guestNickname = $"Guest_{rand.Next(100, 999)}";
            }

            // Simulate viewer counts changing dynamically
            var randCount = new Random();
            _mockViewerCount = randCount.Next(110, 140);
            _viewerTimer = new System.Timers.Timer(8000);
            _viewerTimer.Elapsed += (s, e) =>
            {
                _mockViewerCount = randCount.Next(110, 140);
                InvokeAsync(StateHasChanged);
            };
            _viewerTimer.Start();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await ScrollChatToBottom();
            }
        }

        private void SwitchTab(string tab)
        {
            _activeTab = tab;
            if (tab == "live")
            {
                _iframeSrc = "https://www.youtube.com/embed/live_stream?channel=UCQkJ2iQtZ9lGxDlTKjDz_SA&autoplay=1&mute=1";
            }
            else
            {
                _iframeSrc = "https://www.youtube.com/embed/videoseries?list=UUQkJ2iQtZ9lGxDlTKjDz_SA&autoplay=1";
            }
            StateHasChanged();
        }

        private void OnAppStateChanged()
        {
            InvokeAsync(async () =>
            {
                StateHasChanged();
                await ScrollChatToBottom();
            });
        }

        private void OnAppStateReactionReceived(string emoji)
        {
            AddReaction(emoji);
        }

        private void AddReaction(string emoji)
        {
            var rand = new Random();
            var reaction = new LiveReaction
            {
                Emoji = emoji,
                LeftPercent = rand.Next(15, 85),
                DriftX = rand.Next(-60, 60)
            };

            lock (_activeReactions)
            {
                _activeReactions.Add(reaction);
            }

            // Remove it after the animation ends (3.2 seconds) to avoid memory leaks
            _ = Task.Delay(3500).ContinueWith(_ =>
            {
                InvokeAsync(() =>
                {
                    lock (_activeReactions)
                    {
                        _activeReactions.Remove(reaction);
                    }
                    StateHasChanged();
                });
            });

            InvokeAsync(StateHasChanged);
        }

        private void SendReaction(string emoji)
        {
            AppState.BroadcastReaction(emoji);
        }

        private async Task SendChatMessage()
        {
            if (string.IsNullOrWhiteSpace(_messageText)) return;

            string name = AppState.CurrentUser?.Name ?? (string.IsNullOrWhiteSpace(_guestNickname) ? "Guest" : _guestNickname);
            string role = AppState.ActiveRole ?? "Guest";

            AppState.SendChatMessage(name, _messageText, role);
            _messageText = string.Empty;

            await ScrollChatToBottom();
        }

        private async Task ScrollChatToBottom()
        {
            try
            {
                // Run eval code directly in browser to scroll chat container to bottom
                await JS.InvokeVoidAsync("eval", "const el = document.getElementById('chat-box'); if (el) { el.scrollTop = el.scrollHeight; }");
            }
            catch (Exception)
            {
                // Suppress JS exceptions in pre-rendering lifecycle phases
            }
        }

        public void Dispose()
        {
            AppState.OnChange -= OnAppStateChanged;
            AppState.OnReactionReceived -= OnAppStateReactionReceived;

            if (_viewerTimer != null)
            {
                _viewerTimer.Stop();
                _viewerTimer.Dispose();
            }
        }
    }

    public class LiveReaction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Emoji { get; set; } = string.Empty;
        public int LeftPercent { get; set; }
        public int DriftX { get; set; }
    }
}
