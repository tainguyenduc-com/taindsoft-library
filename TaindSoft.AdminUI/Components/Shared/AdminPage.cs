using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net;
using TaindSoft.AdminUI.Routing;
using TaindSoft.AdminUI.Services;

namespace TaindSoft.AdminUI.Components.Shared
{
    /// <summary>
    /// Base component for admin pages.
    /// </summary>
    public abstract class AdminPage : ComponentBase
    {
        [Inject] protected IJSRuntime JS { get; set; } = default!;
        [Inject] protected NavigationManager Navigation { get; set; } = default!;
        [Inject] protected IAdminToastService Toast { get; set; } = default!;

        /// <summary>Whether the page is currently loading its initial data.</summary>
        protected bool IsLoading { get; set; }

        /// <summary>Whether a save operation is in progress.</summary>
        protected bool IsSaving { get; set; }

        /// <summary>Whether the last save completed successfully (auto-resets after 3 s).</summary>
        protected bool IsSaved { get; set; }

        protected override async Task OnInitializedAsync()
        {
            IsLoading = true;
            StateHasChanged();
            try
            {
                await LoadAsync();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                RedirectToLogin();
                return;
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Override to load page data. Called automatically by OnInitializedAsync.
        /// Re-throw HttpRequestException with status 401 to trigger an automatic redirect to login.
        /// </summary>
        protected virtual Task LoadAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Wraps an async save operation with IsSaving/IsSaved state, toast notifications,
        /// and automatic 401 → redirect-to-login handling.
        /// </summary>
        protected async Task HandleSaveAsync(Func<Task> action, string successMessage = "Saved successfully.")
        {
            IsSaving = true;
            IsSaved = false;
            StateHasChanged();
            try
            {
                await action();
                IsSaved = true;
                await Toast.ShowAsync(successMessage);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                RedirectToLogin();
            }
            catch (Exception ex)
            {
                await Toast.ShowAsync($"Failed to save: {ex.Message}", null, null, 10);
            }
            finally
            {
                IsSaving = false;
                StateHasChanged();
                if (IsSaved)
                {
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(3000);
                        IsSaved = false;
                        await InvokeAsync(StateHasChanged);
                    });
                }
            }
        }

        /// <summary>
        /// Redirects to /login with the current URL as returnUrl so the user is
        /// brought back after successful authentication.
        /// </summary>
        protected void RedirectToLogin()
        {
            string returnUrl = Uri.EscapeDataString(Navigation.Uri);
            Navigation.Go($"/auth/sign-in?returnUrl={returnUrl}", forceLoad: false);
        }
    }
}
