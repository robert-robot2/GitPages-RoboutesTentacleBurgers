using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace ChemCalc.Helpers
{
    /// <summary>
    /// Helper for browser localStorage via JSInterop
    /// </summary>
    public class LocalStorageHelper
    {
        private readonly IJSRuntime _jsRuntime;

        public LocalStorageHelper(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        /// <summary>
        /// Save string value to localStorage
        /// </summary>
        public async Task SetItemAsync(string key, string value)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
            }
            catch
            {
                // Silently fail if localStorage not available
            }
        }

        /// <summary>
        /// Get string value from localStorage
        /// </summary>
        public async Task<string?> GetItemAsync(string key)
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Remove item from localStorage
        /// </summary>
        public async Task RemoveItemAsync(string key)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
            }
            catch
            {
                // Silently fail
            }
        }

        /// <summary>
        /// Clear all localStorage
        /// </summary>
        public async Task ClearAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.clear");
            }
            catch
            {
                // Silently fail
            }
        }

        // ==================== CALCULATOR SPECIFIC METHODS ====================

        /// <summary>
        /// Save theme preference
        /// </summary>
        public async Task SaveThemeAsync(string themeName)
        {
            await SetItemAsync("calculon-theme", themeName);
        }

        /// <summary>
        /// Load theme preference
        /// </summary>
        public async Task<string?> LoadThemeAsync()
        {
            return await GetItemAsync("calculon-theme");
        }

        /// <summary>
        /// Save background image as base64
        /// </summary>
        public async Task SaveBackgroundAsync(string base64Image)
        {
            await SetItemAsync("calculon-background", base64Image);
        }

        /// <summary>
        /// Load background image
        /// </summary>
        public async Task<string?> LoadBackgroundAsync()
        {
            return await GetItemAsync("calculon-background");
        }

        /// <summary>
        /// Clear background image
        /// </summary>
        public async Task ClearBackgroundAsync()
        {
            await RemoveItemAsync("calculon-background");
        }

        /// <summary>
        /// Save angle mode (DEG/RAD)
        /// </summary>
        public async Task SaveAngleModeAsync(string mode)
        {
            await SetItemAsync("calculon-angle-mode", mode);
        }

        /// <summary>
        /// Load angle mode
        /// </summary>
        public async Task<string?> LoadAngleModeAsync()
        {
            return await GetItemAsync("calculon-angle-mode");
        }
    }
}