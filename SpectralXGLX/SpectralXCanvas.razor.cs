using static SpectralXGLX.SpectralXComponent.SpectralXEngine;

namespace SpectralXGLX
{
    public partial class SpectralXCanvas : IDisposable
    {
        [Parameter] public string CanvasClass { get; set; } = "SpectralX-Viewport";
        [Parameter] public string ContainerId { get; set; } = "SpectralX-Container";
        [Parameter] public RenderFragment? ChildContent { get; set; }
        public SpectralXViewport Viewport { get; set; } = new();
        public bool UIHidden => _uiHidden;
        public bool LandscapePanelVisible => _landscapePanelVisible;
        private bool _landscapePanelVisible = false;


        public SpectralXEngine Engine { get; private set; } = default!;
        private DotNetObjectReference<SpectralXEngine>? _engineRef;

        public event Action? OnMeshesReady;
        [Parameter] public bool ShowLandscapePanel { get; set; } = false;

        public SpectralXCamera Camera { get; private set; } = default!;
        public SpectralXInput Input { get; private set; } = default!;
        public SpectralXScene Scene => Engine.Scene;
        public SpectralXMeshLibrary MeshLibrary => Engine.MeshLibrary;   

        public SpectralXDebugRender Debug { get; private set; } = default!;

        [Inject] private GamepadService Gamepad { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private HttpClient Http { get; set; } = default!;

        private ElementReference CanvasRef;
        public bool IsFullscreen { get; set; } = false;
        private string ToggleIcon => IsFullscreen ? "🗗" : "⬜";

        private bool _loopStarted;
        private bool _savedFlash = false;
        private bool _uiHidden = false;
        private bool _bwpMenuVisible = false;
        private bool _isTogglingFullscreen = false;
        private float _timeOfDay = 0.5f;
        private System.Threading.Timer? _uiRefreshTimer;
        private const int UiRefreshIntervalMs = 100;

        // BWP Menu
        private bool _bwpInventoryVisible = false;
        public bool BWPInventoryVisible => _bwpInventoryVisible;
        public bool BWPMenuVisible => _bwpMenuVisible;
        public bool BWPHUDVisible => _bwpHUDVisible;
        private bool _bwpHUDVisible = true;
        // WebGL menu
        private string ActiveTab = "video";
        private void StartUiRefreshTimer()
        {
            _uiRefreshTimer = new System.Threading.Timer(_ =>
            {
                _ = InvokeAsync(StateHasChanged);
            }, null, UiRefreshIntervalMs, UiRefreshIntervalMs);
        }

        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("[SpectralX] OnInitializedAsync fired");
            Camera = new SpectralXCamera();
            Input = new SpectralXInput(this, Viewport, Camera, JS,Gamepad);

            Engine = new SpectralXEngine(
                this,
                Viewport,
                Camera,
                Input,
                Gamepad,
                JS
            );

            Debug = new SpectralXDebugRender(
                Engine,
                Viewport,
                Input,
                Camera,               
                MeshLibrary
            );

            Input.Debug = Debug;

        }


        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
           
                try
                {
                    // ── FBX Batch Load ───────────────────────────────────────────────────
                    var meshList = new[]
                    {
                      
        new { url = "iMeshes/Cube.fbx",          name = "FBXCube" },
        new { url = "iMeshes/Sphere.fbx",         name = "FBXSphere" },
        new { url = "iMeshes/Pyramid.fbx",        name = "FBXPyramid" },
        new { url = "iMeshes/PyramidT.fbx",       name = "FBXPyramidT" },
        new { url = "iMeshes/IsoPyramid.fbx",     name = "FBXIsoPyramid" },
        new { url = "iMeshes/IsoPyramidT.fbx",    name = "FBXIsoPyramidT" },
        new { url = "iMeshes/BullDozerBox3.fbx",  name = "FBXDozerBox" },
        new { url = "iMeshes/BrickBox.fbx",       name = "BrickBox" },
        new { url = "iMeshes/TriangleT.fbx",      name = "TriT" },
        new { url = "iMeshes/Hex002.fbx",         name = "HexCyl" },
        new { url = "iMeshes/JibbaCola.fbx",      name = "ColaSquare" },
        new { url = "iMeshes/SmoothSphere001.fbx",name = "SmoothSphere" },
          new { url = "iMeshes/SmoothSphereStatic.fbx",name = "SmoothSphereStatic" },
        new { url = "iMeshes/SmoothSphereT001.fbx",name = "SmoothSphereT" },
        new { url = "iMeshes/HexCylT001.fbx",     name = "HexCylT" },
        new { url = "iMeshes/LightBulb002.fbx",   name = "LightBulb" },
         new { url = "iMeshes/LightBulbStatic.fbx",   name = "LightBulbStatic" },
        new { url = "iMeshes/CheeseSign004.fbx",  name = "CheeseSign" },
        new { url = "iMeshes/UVSphere001.fbx",    name = "UVSphere" },
        new { url = "iMeshes/FBXCube001.fbx",     name = "FBXCubeRed" },
        new { url = "iMeshes/BushGroup001.fbx",   name = "Bush001" },
        new { url = "iMeshes/RockGroup002.fbx",   name = "Rock001" },
          new { url = "iMeshes/RockGroup003.fbx",   name = "Rock002" },
        new { url = "iMeshes/TreeGroup002.fbx",   name = "Tree001" },
             new { url = "iMeshes/TreeGroup003.fbx",   name = "Tree002" },
         new { url = "iMeshes/Grass001.fbx",   name = "Grass001" },
          new { url = "iMeshes/Grave001.fbx",   name = "Grave001" },
           new { url = "iMeshes/GraveS001.fbx",   name = "GraveS001" },
        new { url = "iMeshes/HouseGroup001.fbx",  name = "House001" },
        new { url = "iMeshes/Stables001.fbx",     name = "Stable001" },
        new { url = "iMeshes/Well001.fbx",        name = "Well001" },
        new { url = "iMeshes/Blacksmith001.fbx",  name = "Blacksmith001" },
        new { url = "iMeshes/Temple001.fbx",      name = "Temple001" },
        new { url = "iMeshes/Storage001.fbx",     name = "Storage001" },
        new { url = "iMeshes/House005.fbx",       name = "House005" },
        new { url = "iMeshes/Mill001.fbx",        name = "Mill001" },
        new { url = "iMeshes/Market001.fbx",      name = "Market001" },
        new { url = "iMeshes/House003.fbx",      name = "House003" },
        new { url = "iMeshes/House006.fbx",      name = "House006" },
        new { url = "iMeshes/SawMill001.fbx",      name = "SawMill001" },
        new { url = "iMeshes/Inn001.fbx",      name = "Inn001" },
        new { url = "iMeshes/BellTower001.fbx",      name = "BellTower001" },
        new { url = "iMeshes/CastleWall001.fbx",      name = "CastleWall001" },
        new { url = "iMeshes/Crypt001.fbx",      name = "Crypt001" },
        new { url = "iMeshes/Shack001.fbx",      name = "Shack001" },
         new { url = "iMeshes/Roboute001.fbx",          name = "Roboute" },
    };

                    var meshListJson = System.Text.Json.JsonSerializer.Serialize(meshList);
                    await JS.InvokeVoidAsync("SpectralFBXHelper.loadAllAndUploadJson", meshListJson);

           
                    foreach (var m in meshList)
                        Engine.MeshLibrary.RegisterJSMesh(m.name);

                    // ── STL Batch Load ───────────────────────────────────────────────────
                
                    try
                    {
                        var stlMeshList = new[]
                        {
        new { url = "iMeshes/FemurBoneSTL.stl", name = "FemurBoneSTL", smooth = false },
    };

                        var stlMeshListJson = System.Text.Json.JsonSerializer.Serialize(stlMeshList);
                        await JS.InvokeVoidAsync("SpectralSTLHelper.loadAllAndUploadJson", stlMeshListJson);
                        foreach (var m in stlMeshList)
                            Engine.MeshLibrary.RegisterJSMesh(m.name);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[SpectralX] STL batch load failed: " + ex.ToString());
                    }

                    // ── GLTF Batch Load ─────────────────────────────────────────────────
                    try
                    {
                        var gltfMeshList = new[]
                        {
         new { url = "iMeshes/SkullHand001.glb", name = "MechArm" },
           
    };

                        if (gltfMeshList.Length > 0)
                        {
                            var gltfMeshListJson = System.Text.Json.JsonSerializer.Serialize(gltfMeshList);
                            await JS.InvokeVoidAsync("SpectralGLTFHelper.loadAllAndUploadJson", gltfMeshListJson);

                            foreach (var m in gltfMeshList)
                                Engine.MeshLibrary.RegisterJSMesh(m.name);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[SpectralX] GLTF batch load failed: " + ex.ToString());
                    }



                }
                catch (Exception ex)
                {
                    Console.WriteLine("LOAD FAILED: " + ex.ToString());
                }

                Engine.Init();
                await Gamepad.InitAsync(); 
                _engineRef = DotNetObjectReference.Create(Engine);
                OnMeshesReady?.Invoke(); 
                await JS.InvokeVoidAsync("watchCanvasSize", ContainerId, _engineRef);
            }

            // Render loop start 
            if (!_loopStarted && _engineRef != null)
            {
                try
                {
                    while (!await JS.InvokeAsync<bool>("eval",
                        "window._SpectralShaders !== undefined"))
                        await Task.Delay(200); 
                    var size = await JS.InvokeAsync<ViewportSize>("getViewportSize", ContainerId);
                    if (size != null && size.Width > 0 && size.Height > 0)
                    {
                        Viewport.DynamicSize = CanvasClass == "Home-Viewport";
                   
                        Console.WriteLine($"[SpectralX] Viewport set to {size.Width}x{size.Height}");
                    }
                    await Engine.PushShadersToJS();
                    await JS.InvokeVoidAsync("SpectralGLInterop.startRenderLoop",
                        CanvasRef, _engineRef);
                    await JS.InvokeVoidAsync("registerFullscreenListener", _engineRef, ContainerId);
                    _loopStarted = true;
                    StartUiRefreshTimer(); 
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[SpectralX] Failed to start render loop: " + ex);
                }
            }
        }
        private void OnAAModeChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int mode))
                Engine.ActiveAA = (AAMode)mode;
        }
        private void OnCameraModeChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int mode))
                Engine.SetCameraMode(
                    (SpectralXGLX.SpectralXComponent.SpectralXEngine.CameraMode)mode);
        }
        private void OnShadowModeChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int mode))
                Engine.ActiveShadow = (SpectralXEngine.ShadowMode)mode;
        }
  
        public async Task ToggleViewport()
        {
            // Guard — prevent double-fire from competing pathways
            if (_isTogglingFullscreen) return;
            _isTogglingFullscreen = true;

            try
            {
                await JS.InvokeVoidAsync("toggleFullscreen", ContainerId);
                IsFullscreen = !IsFullscreen;

                // Wait for browser to finalize the fullscreen transition
                await Task.Delay(150);

                if (IsFullscreen)
                {
                    // Entering fullscreen — read actual screen dimensions
                    // isExiting = false (default, omit parameter)
                    var size = await JS.InvokeAsync<ViewportSize>(
                        "getViewportSize", ContainerId);
                    if (size != null && size.Width > 0 && size.Height > 0)
                    {
                        Viewport.SetSize(size.Width, size.Height);
                        await JS.InvokeVoidAsync("SpectralGLInterop.resizeCanvas",
                            size.Width, size.Height);
                        Console.WriteLine(
                            $"[SpectralX] Fullscreen ON: {size.Width}x{size.Height}");
                    }
                }
                else
                {
                    // Exiting fullscreen — extra delay, CSS needs longer to collapse
                    await Task.Delay(250);

                    // isExiting = true — reads container CSS rect, skips canvas
                    var size = await JS.InvokeAsync<ViewportSize>(
                        "getViewportSize", ContainerId, true);
                    if (size != null && size.Width > 0 && size.Height > 0)
                    {
                        Viewport.SetSize(size.Width, size.Height);
                        await JS.InvokeVoidAsync("SpectralGLInterop.resizeCanvas",
                            size.Width, size.Height);
                        Console.WriteLine(
                            $"[SpectralX] Fullscreen OFF: restored to {size.Width}x{size.Height}");
                    }
                    else
                    {
                        // Hard fallback only if DOM read fails
                        Viewport.SetSize(1024, 768);
                        await JS.InvokeVoidAsync(
                            "SpectralGLInterop.resizeCanvas", 1024, 768);
                        Console.WriteLine(
                            "[SpectralX] Fullscreen OFF: fallback 1024x768");
                    }
                }

                StateHasChanged();
            }
            finally
            {
                // Extra settle time before releasing guard —
                // swallows any ResizeObserver events still in flight
                await Task.Delay(200);
                _isTogglingFullscreen = false;
            }
        }
     

        public void ToggleBWPInventory()
        {
            _bwpInventoryVisible = !_bwpInventoryVisible;
            StateHasChanged();
        }
        public void ToggleUIHidden()
        {
            _uiHidden = !_uiHidden;
            StateHasChanged();
        }
        public void ToggleLandscapePanel()
        {
            _landscapePanelVisible = !_landscapePanelVisible;
            StateHasChanged();
        }
        public void ToggleBWPMenu()
        {
            _bwpMenuVisible = !_bwpMenuVisible;
            StateHasChanged();
        }
             
        public void ToggleBWPHUD()
        {
            _bwpHUDVisible = !_bwpHUDVisible;
            StateHasChanged();
        }

        public void Dispose()
        {
  
            _ = JS.InvokeVoidAsync("SpectralGLInterop.flush"); // replaces stopRenderLoop — flush covers it
                        _engineRef?.Dispose();
            _uiRefreshTimer?.Dispose(); 
        }


        public class ViewportSize
        {
            public int Width { get; set; }
            public int Height { get; set; }
        }      

        private void OnTimeOfDayChanged(ChangeEventArgs e)
        {
            if (float.TryParse(e.Value?.ToString(),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out float val))
            {
                _timeOfDay = val;
                Engine.SetTimeOfDay(val);
            }
        }

        private string GetTimeLabel()
        {
            float t = _timeOfDay;

            if (t < 0.05f) return "Midnight";
            else if (t < 0.22f) return "Late Night";
            else if (t < 0.27f) return "Sunrise";
            else if (t < 0.35f) return "Golden Hour";
            else if (t < 0.45f) return "Morning";
            else if (t < 0.55f) return "Noon";
            else if (t < 0.65f) return "Afternoon";
            else if (t < 0.73f) return "Golden Hour";
            else if (t < 0.78f) return "Sunset";
            else if (t < 0.85f) return "Dusk";
            else if (t < 0.95f) return "Evening";
            else return "Midnight";
        }


    }
}