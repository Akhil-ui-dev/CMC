using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Akhil.Proj.Web.Services;

namespace Akhil.Proj.Web.Components.Pages;

public partial class Auth : IDisposable
{
    [Inject]
    public AppStateService AppState { get; set; } = default!;

    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    private bool _isLogin = true;
    private string _errorMessage = "";

    // Profile CRUD states
    private bool _isEditingProfile = false;
    private bool _showDeleteConfirm = false;
    private bool _profileDeletedSuccess = false;
    
    // Reset password states
    private bool _isResetPasswordMode = false;
    private string _resetEmail = "";
    private string _resetPassword = "";
    private string _resetConfirmPassword = "";
    private bool _resetSuccess = false;

    // Profile edit fields
    private string _editName = "";
    private string _editEmail = "";
    private string _editPhone = "";
    private string _editAddress = "";
    private string _editFamily = "";
    private string _editReferral = "";
    private string _editPassword = "";
    private List<string> _editSelectedInterests = new();

    // Login fields
    private string _email = "";
    private string _password = "";

    // Registration fields
    private string _regName = "";
    private string _regEmail = "";
    private string _regPassword = "";
    private string _regPhone = "";
    private string _regAddress = "";
    private string _regFamily = "";
    private string _regReferral = "";
    private bool _regConsent = false;

    private readonly List<string> _availableInterests = new() { "Youth Group", "Choir / Praise Band", "Missions & Charity", "Sound & AV Tech", "Kids Ministry", "Greeters & Ushers" };
    private readonly List<string> _selectedInterests = new();

    protected override void OnInitialized()
    {
        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        if (uri.AbsolutePath.EndsWith("/register", StringComparison.OrdinalIgnoreCase))
        {
            _isLogin = false;
        }
        AppState.OnChange += HandleStateChange;
    }

    private void HandleStateChange() => InvokeAsync(StateHasChanged);

    private void ToggleAuthMode()
    {
        _isLogin = !_isLogin;
        _isResetPasswordMode = false;
        _errorMessage = "";
    }

    private void EnableResetPasswordMode()
    {
        _isResetPasswordMode = true;
        _errorMessage = "";
        _resetSuccess = false;
        _resetEmail = _email;
        _resetPassword = "";
        _resetConfirmPassword = "";
    }

    private void DisableResetPasswordMode()
    {
        _isResetPasswordMode = false;
        _errorMessage = "";
    }

    private void HandleResetPassword()
    {
        _errorMessage = "";
        _resetSuccess = false;
        
        if (string.IsNullOrWhiteSpace(_resetEmail) || string.IsNullOrWhiteSpace(_resetPassword) || string.IsNullOrWhiteSpace(_resetConfirmPassword))
        {
            _errorMessage = "Please fill in all fields.";
            return;
        }

        if (_resetPassword != _resetConfirmPassword)
        {
            _errorMessage = "New passwords do not match.";
            return;
        }

        try
        {
            AppState.ResetUserPassword(_resetEmail, _resetPassword);
            _resetSuccess = true;
            
            // Redirect back to login after 2 seconds
            var timer = new System.Timers.Timer(2000);
            timer.Elapsed += (s, e) =>
            {
                timer.Stop();
                timer.Dispose();
                InvokeAsync(() => {
                    _isResetPasswordMode = false;
                    _resetSuccess = false;
                    _email = _resetEmail;
                    _password = "";
                    StateHasChanged();
                });
            };
            timer.Start();
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
    }

    private void HandleLogin()
    {
        _errorMessage = "";
        
        if (string.IsNullOrWhiteSpace(_email) || string.IsNullOrWhiteSpace(_password))
        {
            _errorMessage = "Please enter both credentials.";
            return;
        }

        try
        {
            AppState.LogIn(_email, _password);
            Navigation.NavigateTo("");
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
    }

    private void SimulateMagicLink()
    {
        if (string.IsNullOrWhiteSpace(_email))
        {
            _errorMessage = "Please fill in the email field first to send a Magic Link.";
            return;
        }
        
        AppState.LogIn(_email, "magic");
        Navigation.NavigateTo("");
    }

    private void SubmitRegistration()
    {
        _errorMessage = "";
        if (!_regConsent)
        {
            _errorMessage = "You must agree to the Privacy Policy to proceed.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_regName) || string.IsNullOrWhiteSpace(_regEmail) || string.IsNullOrWhiteSpace(_regPassword) || string.IsNullOrWhiteSpace(_regPhone) || string.IsNullOrWhiteSpace(_regReferral))
        {
            _errorMessage = "Please fill in all required registration fields.";
            return;
        }

        var newMember = new Member
        {
            Name = _regName,
            Email = _regEmail,
            Password = _regPassword,
            Phone = _regPhone,
            Address = _regAddress,
            FamilyMembers = string.IsNullOrWhiteSpace(_regFamily) ? "None" : _regFamily,
            HowHeard = _regReferral,
            Interests = new List<string>(_selectedInterests)
        };

        try
        {
            AppState.Register(newMember);
            Navigation.NavigateTo("");
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
    }

    private void ToggleInterest(string interest)
    {
        if (_selectedInterests.Contains(interest))
        {
            _selectedInterests.Remove(interest);
        }
        else
        {
            _selectedInterests.Add(interest);
        }
    }

    // Authenticated Profile CRUD actions
    private void StartEditProfile()
    {
        if (AppState.CurrentUser != null)
        {
            _editName = AppState.CurrentUser.Name;
            _editEmail = AppState.CurrentUser.Email;
            _editPhone = AppState.CurrentUser.Phone;
            _editAddress = AppState.CurrentUser.Address;
            _editFamily = AppState.CurrentUser.FamilyMembers;
            _editReferral = AppState.CurrentUser.HowHeard;
            _editSelectedInterests = new List<string>(AppState.CurrentUser.Interests);
            _editPassword = "";
            _errorMessage = "";
            _isEditingProfile = true;
        }
    }

    private void ToggleEditInterest(string interest)
    {
        if (_editSelectedInterests.Contains(interest))
        {
            _editSelectedInterests.Remove(interest);
        }
        else
        {
            _editSelectedInterests.Add(interest);
        }
    }

    private void SaveProfileChanges()
    {
        _errorMessage = "";
        if (string.IsNullOrWhiteSpace(_editName) || string.IsNullOrWhiteSpace(_editPhone) || string.IsNullOrWhiteSpace(_editReferral))
        {
            _errorMessage = "Please fill in all required fields.";
            return;
        }

        try
        {
            AppState.UpdateMemberProfile(_editEmail, _editName, _editPhone, _editAddress, _editFamily, _editReferral, _editSelectedInterests, _editPassword);
            _isEditingProfile = false;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
    }

    private void ConfirmDeleteAccount()
    {
        if (AppState.CurrentUser != null)
        {
            var email = AppState.CurrentUser.Email;
            _profileDeletedSuccess = true;
            _showDeleteConfirm = false;
            
            var timer = new System.Timers.Timer(2000);
            timer.Elapsed += (s, e) =>
            {
                timer.Stop();
                timer.Dispose();
                AppState.DeleteMemberAccount(email);
                InvokeAsync(() => {
                    _profileDeletedSuccess = false;
                    _isLogin = true;
                    StateHasChanged();
                });
            };
            timer.Start();
        }
    }

    private void LogOutUser()
    {
        AppState.LogOut();
    }

    public void Dispose()
    {
        AppState.OnChange -= HandleStateChange;
    }
}
