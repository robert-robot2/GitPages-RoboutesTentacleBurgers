using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace RoboutesTentacleBurgers.Pages
{
    public partial class Home : ComponentBase, IAsyncDisposable
    {
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        private SpectralXGLX.SpectralXCanvas? EngineComponent;
      
        private SpectralXGLX.SpectralXComponent.SpectralXEngine? Engine => EngineComponent?.Engine;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // Disable browser scrollbar — WebGL canvas handles scrolling on this page
                await JSRuntime.InvokeVoidAsync("document.body.classList.add", "no-scroll");

                if (EngineComponent != null)
                {
                    EngineComponent.OnMeshesReady += OnEngineReady;
                 
                }
            }
        }
        private void OnEngineReady()
        {
            EngineComponent?.Engine?.SwitchToScene(
                SpectralXGLX.SpectralXComponent.SpectralXEngine.SceneID.Home);
        }

        public void Dispose()
        {
            glowTimer?.Dispose();

            if (EngineComponent != null)
                EngineComponent.OnMeshesReady -= OnEngineReady; // prevent accumulating handlers on re-nav
        }
        public async ValueTask DisposeAsync()
        {
            // Restore browser scrollbar when leaving this page
            try
            {
                await JSRuntime.InvokeVoidAsync("document.body.classList.remove", "no-scroll");
            }
            catch
            {
                // Swallow — JS runtime may already be disposed during hot reload or shutdown
            }
        }

    }
}