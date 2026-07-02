using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.JSInterop;
using Akhil.Proj.Web.Services;

namespace Akhil.Proj.Web.Components.Pages;

public partial class News : IDisposable
{
    [Inject]
    public AppStateService AppState { get; set; } = default!;

    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    [Inject]
    public IJSRuntime JS { get; set; } = default!;

    [Inject]
    public IWebHostEnvironment Env { get; set; } = default!;

    private string _selectedCategory = "All";
    private Post? _selectedPost;
    private bool _showCreateModal = false;
    private string _commentInput = "";

    // Draft Post Fields
    private string _newPostTitle = "";
    private string _newPostCategory = "Announcement";
    private string _newPostImageUrl = "";
    private string _newPostContent = "";
    private string _newPostAuthor = "";
    private Post? _editingPost;
    private bool _isUploading = false;

    protected override void OnInitialized()
    {
        AppState.OnChange += HandleStateChange;
    }

    private void HandleStateChange() => InvokeAsync(StateHasChanged);

    private void FilterFeed(string category)
    {
        _selectedCategory = category;
    }

    private List<Post> GetFilteredPosts()
    {
        if (_selectedCategory == "All")
        {
            return AppState.Posts;
        }
        return AppState.Posts.Where(p => p.Category == _selectedCategory).ToList();
    }

    private string GetExcerpt(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        if (text.Length <= 150) return text;
        return text.Substring(0, 150) + "...";
    }

    private bool IsLiked(Post post)
    {
        if (AppState.CurrentUser == null) return false;
        return post.Likes.Contains(AppState.CurrentUser.Email);
    }

    private void ToggleLike(Post post)
    {
        if (AppState.CurrentUser == null)
        {
            // Redirect to sign in if trying to interact
            Navigation.NavigateTo("auth");
            return;
        }
        AppState.ToggleLikePost(post.Id, AppState.CurrentUser.Email);
    }

    private void OpenPostDetails(Post post)
    {
        _selectedPost = post;
        _commentInput = "";
    }

    private void ClosePostDetails()
    {
        _selectedPost = null;
    }

    private void SubmitComment()
    {
        if (_selectedPost != null && !string.IsNullOrWhiteSpace(_commentInput) && AppState.CurrentUser != null)
        {
            AppState.AddCommentToPost(_selectedPost.Id, AppState.CurrentUser.Name, _commentInput.Trim());
            _commentInput = "";
        }
    }

    // Post Creation & Editing
    private void OpenCreatePostModal()
    {
        _editingPost = null;
        _newPostTitle = "";
        _newPostCategory = "Testimony"; // Default to Testimony for public posts
        _newPostImageUrl = "";
        _newPostContent = "";
        _newPostAuthor = AppState.CurrentUser?.Name ?? "";
        _showCreateModal = true;
    }

    private void OpenEditPostModal(Post post)
    {
        _editingPost = post;
        _newPostTitle = post.Title;
        _newPostCategory = post.Category;
        _newPostImageUrl = post.ImageUrl;
        _newPostContent = post.Content;
        _newPostAuthor = post.Author;
        _showCreateModal = true;
    }

    private void CloseCreatePostModal()
    {
        _showCreateModal = false;
        _editingPost = null;
    }

    private void SubmitForm()
    {
        if (_editingPost == null)
        {
            SubmitNewPost();
        }
        else
        {
            SubmitEditPost();
        }
    }

    private void SubmitNewPost()
    {
        if (!string.IsNullOrWhiteSpace(_newPostTitle) && !string.IsNullOrWhiteSpace(_newPostContent))
        {
            string author = string.IsNullOrWhiteSpace(_newPostAuthor) ? "Anonymous Member" : _newPostAuthor.Trim();
            AppState.CreatePost(_newPostTitle, _newPostContent, _newPostImageUrl, _newPostCategory, author);
            _showCreateModal = false;
        }
    }

    private void SubmitEditPost()
    {
        if (_editingPost != null && !string.IsNullOrWhiteSpace(_newPostTitle) && !string.IsNullOrWhiteSpace(_newPostContent))
        {
            string author = string.IsNullOrWhiteSpace(_newPostAuthor) ? "Anonymous Member" : _newPostAuthor.Trim();
            AppState.UpdatePost(_editingPost.Id, _newPostTitle, _newPostContent, _newPostImageUrl, _newPostCategory, author);
            _showCreateModal = false;
            _editingPost = null;
        }
    }

    private async Task HandleDeletePost(Post post)
    {
        bool confirmed = await JS.InvokeAsync<bool>("confirm", $"Are you sure you want to delete the post '{post.Title}'?");
        if (confirmed)
        {
            AppState.DeletePost(post.Id);
            if (_selectedPost?.Id == post.Id)
            {
                _selectedPost = null;
            }
        }
    }

    // Helpers for Badge Colors & Icons
    private string GetCategoryBadgeClass(string cat) => cat switch
    {
        "Announcement" => "badge-announcement",
        "Sermon" => "badge-sermon",
        "Testimony" => "badge-testimony",
        _ => "bg-secondary"
    };

    private string GetCategoryIcon(string cat) => cat switch
    {
        "Announcement" => "bi bi-megaphone",
        "Sermon" => "bi bi-journal-richtext",
        "Testimony" => "bi bi-chat-left-quote",
        _ => "bi bi-tag"
    };

    private string GetTimeAgo(DateTime dt)
    {
        var span = DateTime.Now - dt;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        return dt.ToString("MMM dd");
    }

    public void Dispose()
    {
        AppState.OnChange -= HandleStateChange;
    }

    private async Task HandleImageUpload(InputFileChangeEventArgs e)
    {
        try
        {
            _isUploading = true;
            var file = e.File;
            if (file != null)
            {
                var extension = Path.GetExtension(file.Name).ToLower();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                if (!allowedExtensions.Contains(extension))
                {
                    await JS.InvokeVoidAsync("alert", "Only JPG, JPEG, PNG, and GIF images are allowed.");
                    return;
                }

                var uniqueName = Guid.NewGuid().ToString() + extension;
                var uploadsDir = Path.Combine(Env.WebRootPath, "uploads");
                
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                var filePath = Path.Combine(uploadsDir, uniqueName);

                using (var stream = file.OpenReadStream(5 * 1024 * 1024))
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await stream.CopyToAsync(fileStream);
                }

                _newPostImageUrl = $"/uploads/{uniqueName}";
            }
        }
        catch (Exception ex)
        {
            await JS.InvokeVoidAsync("alert", "Error uploading file: " + ex.Message);
        }
        finally
        {
            _isUploading = false;
        }
    }

    private void ClearUploadedImage()
    {
        _newPostImageUrl = "";
    }
}
