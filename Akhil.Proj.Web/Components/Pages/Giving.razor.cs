using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Akhil.Proj.Web.Services;

namespace Akhil.Proj.Web.Components.Pages;

public partial class Giving : IDisposable
{
    [Inject]
    public AppStateService AppState { get; set; } = default!;

    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    private int _paymentStep = 1;
    private int _progressVal = 0;
    
    // Config selection
    private string _selectedFund = "General Tithe";
    private int _donationAmount = 50;
    private readonly List<int> _presetAmounts = new() { 10, 25, 50, 100, 250 };

    // Credit Card Fields
    private string _cardName = "";
    private string _cardNumber = "";
    private string _cardExpiry = "";
    private string _cardCvv = "";

    // Simulated Txn Details
    private string _txnId = "";

    protected override void OnInitialized()
    {
        AppState.OnChange += HandleStateChange;
    }

    private void HandleStateChange() => InvokeAsync(StateHasChanged);

    private void SelectFund(string fund)
    {
        _selectedFund = fund;
    }

    private void SelectAmount(int amt)
    {
        _donationAmount = amt;
    }

    private void GoToPaymentDetails()
    {
        if (_donationAmount > 0)
        {
            _paymentStep = 2;
        }
    }

    private void ProcessDonation()
    {
        _paymentStep = 3;
        _progressVal = 0;
        _txnId = new Random().Next(100000, 999999).ToString();

        // Increment progress bar to simulate processing
        var timer = new System.Timers.Timer(100);
        timer.Elapsed += (s, e) =>
        {
            _progressVal += 10;
            InvokeAsync(StateHasChanged);
            if (_progressVal >= 100)
            {
                timer.Stop();
                timer.Dispose();
                _paymentStep = 4;
                InvokeAsync(StateHasChanged);
            }
        };
        timer.Start();
    }

    private void ResetGivingFlow()
    {
        _paymentStep = 1;
        _donationAmount = 50;
        _cardName = "";
        _cardNumber = "";
        _cardExpiry = "";
        _cardCvv = "";
    }

    // Interactive Keypress Format Helpers
    private void FormatCardNumber(KeyboardEventArgs e)
    {
        // Simple mock character formatting in-browser
        if (e.Key != "Backspace" && char.IsDigit(e.Key.ToString()[0]))
        {
            var clean = _cardNumber.Replace(" ", "");
            if (clean.Length > 0 && clean.Length % 4 == 0)
            {
                _cardNumber += " ";
            }
        }
    }

    private void FormatExpiry(KeyboardEventArgs e)
    {
        if (e.Key != "Backspace" && char.IsDigit(e.Key.ToString()[0]))
        {
            var clean = _cardExpiry.Replace("/", "");
            if (clean.Length == 2)
            {
                _cardExpiry += "/";
            }
        }
    }

    public void Dispose()
    {
        AppState.OnChange -= HandleStateChange;
    }
}
