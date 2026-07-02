using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Akhil.Proj.Web.Services;

namespace Akhil.Proj.Web.Components.Pages;

public partial class Gallery : IDisposable
{
    [Inject]
    public AppStateService AppState { get; set; } = default!;

    [Inject]
    public IWebHostEnvironment WebHostEnvironment { get; set; } = default!;

    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    private string _selectedAlbum = "All";
    private GalleryImage? _activeLightboxImage;
    private bool _showUploadModal = false;
    private bool _showModerationTab = false;
    
    // User upload fields
    private string _uploadCaption = "";
    private string _uploadAlbum = "Baptisms";
    private string _uploadImageUrl = "";
    private bool _uploadSuccess = false;

    // Guest and File Upload states
    private string _guestName = "";
    private string _guestEmail = "";
    private string _uploadTab = "file"; // "file" or "url"
    private bool _isUploadingFile = false;
    private string _uploadedFileUrl = "";
    private string _fileUploadError = "";

    protected override void OnInitialized()
    {
        AppState.OnChange += HandleStateChange;
    }

    private void HandleStateChange() => InvokeAsync(StateHasChanged);

    private void FilterGallery(string album)
    {
        _selectedAlbum = album;
    }

    private List<GalleryImage> GetApprovedImages()
    {
        var query = AppState.GalleryImages.Where(g => g.IsApproved);
        if (_selectedAlbum == "All")
        {
            return query.OrderByDescending(g => g.UploadedDate).ToList();
        }
        return query.Where(g => g.Album == _selectedAlbum).OrderByDescending(g => g.UploadedDate).ToList();
    }

    // Lightbox Controls
    private void OpenLightbox(GalleryImage img)
    {
        _activeLightboxImage = img;
    }

    private void CloseLightbox()
    {
        _activeLightboxImage = null;
    }

    private void PrevImage()
    {
        if (_activeLightboxImage == null) return;
        var list = GetApprovedImages();
        var idx = list.FindIndex(i => i.Id == _activeLightboxImage.Id);
        if (idx > 0)
        {
            _activeLightboxImage = list[idx - 1];
        }
        else
        {
            _activeLightboxImage = list[list.Count - 1]; // Loop to end
        }
    }

    private void NextImage()
    {
        if (_activeLightboxImage == null) return;
        var list = GetApprovedImages();
        var idx = list.FindIndex(i => i.Id == _activeLightboxImage.Id);
        if (idx < list.Count - 1)
        {
            _activeLightboxImage = list[idx + 1];
        }
        else
        {
            _activeLightboxImage = list[0]; // Loop to start
        }
    }

    // Upload Controls
    private void OpenUploadModal()
    {
        _uploadCaption = "";
        _uploadAlbum = "Baptisms";
        _uploadImageUrl = "";
        _uploadSuccess = false;
        _guestName = "";
        _guestEmail = "";
        _uploadTab = "file";
        _uploadedFileUrl = "";
        _fileUploadError = "";
        _isUploadingFile = false;
        _showUploadModal = true;
    }

    private void CloseUploadModal()
    {
        _showUploadModal = false;
    }

    private void SetUploadTab(string tab)
    {
        _uploadTab = tab;
        _fileUploadError = "";
        if (tab == "file")
        {
            _uploadImageUrl = _uploadedFileUrl;
        }
        else
        {
            _uploadImageUrl = "";
        }
    }

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        _isUploadingFile = true;
        _fileUploadError = "";
        _uploadedFileUrl = "";
        _uploadImageUrl = "";
        StateHasChanged();

        try
        {
            var file = e.File;
            if (file != null)
            {
                // Validate size (max 5MB)
                long maxFileSize = 1024 * 1024 * 5; // 5MB
                if (file.Size > maxFileSize)
                {
                    _fileUploadError = "File size exceeds 5MB limit.";
                    return;
                }

                // Validate extension
                var extension = Path.GetExtension(file.Name);
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension.ToLower()))
                {
                    _fileUploadError = "Only JPG, JPEG, PNG, WEBP, and GIF images are allowed.";
                    return;
                }

                // Save file to disk
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                
                try
                {
                    var uploadsFolder = Path.Combine(WebHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Open file stream and write to disk
                    using (var stream = file.OpenReadStream(maxFileSize))
                    {
                        using (var fs = new FileStream(filePath, FileMode.Create))
                        {
                            await stream.CopyToAsync(fs);
                        }
                    }

                    _uploadedFileUrl = $"/uploads/{uniqueFileName}";
                    _uploadImageUrl = _uploadedFileUrl;
                }
                catch (Exception)
                {
                    // Fallback to Base64 if disk write fails
                    using (var stream = file.OpenReadStream(maxFileSize))
                    {
                        using (var ms = new MemoryStream())
                        {
                            await stream.CopyToAsync(ms);
                            var bytes = ms.ToArray();
                            _uploadedFileUrl = $"data:{file.ContentType};base64,{Convert.ToBase64String(bytes)}";
                            _uploadImageUrl = _uploadedFileUrl;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _fileUploadError = $"Failed to process image: {ex.Message}";
        }
        finally
        {
            _isUploadingFile = false;
            StateHasChanged();
        }
    }

    private void SubmitUserPhoto()
    {
        if (string.IsNullOrWhiteSpace(_uploadCaption)) return;

        string uploadedByEmail = "";
        
        if (AppState.CurrentUser != null)
        {
            uploadedByEmail = AppState.CurrentUser.Email;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(_guestName) || string.IsNullOrWhiteSpace(_guestEmail))
            {
                _fileUploadError = "Please enter your name and email to submit.";
                return;
            }
            uploadedByEmail = $"{_guestName} <{_guestEmail}>";
        }

        // If the user selected the file tab but didn't upload any file, show error
        if (_uploadTab == "file" && string.IsNullOrEmpty(_uploadImageUrl))
        {
            _fileUploadError = "Please select an image file to upload.";
            return;
        }

        // Submit photo
        AppState.UploadGalleryImage(_uploadCaption, _uploadAlbum, _uploadImageUrl, uploadedByEmail, autoApprove: true);
        _uploadSuccess = true;
    }

    // Moderation Controls
    private void ApproveImage(int id)
    {
        AppState.ApproveGalleryImage(id);
    }

    private void RejectImage(int id)
    {
        AppState.RejectGalleryImage(id);
    }

    public void Dispose()
    {
        AppState.OnChange -= HandleStateChange;
    }
}
