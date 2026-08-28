namespace RoboutesTentacleBurgers.Pages
{
    public partial class SpectralX:ComponentBase
    {

        private SpectralXGLX.SpectralXCanvas? EngineComponent;
        private SpectralXGLX.SpectralXComponent.SpectralXEngine? Engine => EngineComponent?.Engine;

        [Inject] private IJSRuntime JS { get; set; } = default!;               

        private bool showIntro = true;
        private string _starsOutput = string.Empty;
        private System.Timers.Timer? _starsTimer;



        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (EngineComponent != null)
                {
                    EngineComponent.OnMeshesReady += OnEngineReady; // named ref so it can be removed
                }
                // startRenderLoop already calls SpectralEngineLoader.ready() —
                // calling it again here creates a race with the 500ms delay.
                // Remove the Task.Delay + InvokeVoidAsync("SpectralEngineLoader.ready") block.
            }
        }

        private void OnEngineReady()
        {
            EngineComponent?.Engine?.SwitchToScene(
                SpectralXGLX.SpectralXComponent.SpectralXEngine.SceneID.SpectralXDemo);
        }

        public void Dispose()
        {
            _starsTimer?.Stop();
            _starsTimer?.Dispose();
            _starsTimer = null;

            if (EngineComponent != null)
                EngineComponent.OnMeshesReady -= OnEngineReady; // prevent accumulating handlers on re-nav
        }


        protected override void OnInitialized()
        {
            StartStars();
        }

        private void StartStars()
        {
            _starsTimer = new System.Timers.Timer(1000);
            _starsTimer.Elapsed += (s, e) =>
            {
                var rnd = new Random();
                _starsOutput = string.Empty;
                for (int i = 0; i < 20; i++)
                {
                    int x = rnd.Next(800);
                    int y = rnd.Next(600);
                    _starsOutput += $@"<div style='width:4px;height:4px;background:white;border-radius:50%;position:absolute;top:{y}px;left:{x}px;box-shadow:0 0 10px white;'></div>";
                }
                InvokeAsync(StateHasChanged);
            };
            _starsTimer.Start();
        }

        private void HideIntro()
        {
            showIntro = false;
            _starsTimer?.Stop();
            _starsOutput = string.Empty;
            _ = Task.Delay(1).ContinueWith(_ =>
            {
                JS.InvokeVoidAsync("SpectralEngineLoader.ready");   // ← dismiss engine loader
                JS.InvokeVoidAsync("SpectralGLLoader.reset", false, false); // keep if you need asset loader too
            });
        }


     



    }
}
