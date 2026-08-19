using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace TaindSoft.AdminUI.Components.Pages.Setup;

public partial class Install : ComponentBase
{
    [Inject] public NavigationManager? Navigation { get; set; }

    private HttpClient CreateClient()
    {
        var baseUrl = Navigation?.BaseUri?.TrimEnd('/') ?? "https://localhost:15000";
        return new HttpClient { BaseAddress = new Uri(baseUrl + "/") };
    }

    [Required]
    public string? Email { get; set; }
    [Required]
    public string? Username { get; set; }
    [Required, MinLength(8)]
    public string? Password { get; set; }
    [Required]
    public string? ConfirmPassword { get; set; }
    public string? FullName { get; set; }
    public bool IsLoading { get; set; }
    public bool IsInstalled { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var client = CreateClient();
        try
        {
            var resp = await client.GetAsync("api/v1/identity/install/status");
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var data = doc.RootElement.TryGetProperty("data", out var d) ? d : doc.RootElement;
                var isInstalled = data.TryGetProperty("isInstalled", out var v) && v.GetBoolean();
                if (isInstalled)
                {
                    IsInstalled = true;
                    SuccessMessage = "System already installed. Redirecting to login...";
                    StateHasChanged();
                    await Task.Delay(3000);
                    Navigation?.NavigateTo("/auth/sign-in", true);
                    return;
                }
            }
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Cannot connect to server.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An unexpected error occurred: {ex.Message}";
        }
    }

    private async Task HandleSubmit()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (string.IsNullOrWhiteSpace(Email) || !IsValidEmail(Email!))
        {
            ErrorMessage = "Please enter a valid email address.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Username is required.";
            return;
        }
        if (string.IsNullOrEmpty(Password) || Password!.Length < 8)
        {
            ErrorMessage = "Password must be at least 8 characters.";
            return;
        }
        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return;
        }
        IsLoading = true;
        var client = CreateClient();
        try
        {
            var reqBody = new { email = Email, username = Username, password = Password, confirmPassword = ConfirmPassword, fullName = FullName };
            var resp = await client.PostAsJsonAsync("api/v1/identity/install/setup", reqBody);
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var success = doc.RootElement.TryGetProperty("success", out var v) && v.GetBoolean();
            if (success)
            {
                SuccessMessage = "Setup complete. Redirecting to login...";
                StateHasChanged();
                await Task.Delay(3000);
                Navigation?.NavigateTo("/auth/sign-in", true);
                return;
            }
            ErrorMessage = doc.RootElement.TryGetProperty("message", out var msg) ? msg.GetString() : "Setup failed.";
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Cannot connect to server.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An unexpected error occurred: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        // Simple RFC5322 regex
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}
