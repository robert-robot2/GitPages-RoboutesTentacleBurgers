using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

using DCLibrary.Death;

namespace DCLibrary
{
    public partial class DeathCube2 : ComponentBase
    {
        [Inject] public IJSRuntime JS { get; set; } = default!;

        // Shared cube state
        private readonly DeathCubeState cubeState = new();

        // Renderer and controller now depend on cubeState
        private readonly DeathCubeRenderer cubeRenderer;
        private readonly DeathLightManager lightManager = new();
        private readonly DeathInputController inputController;

        public DeathCube2()
        {
            cubeRenderer = new DeathCubeRenderer(cubeState);
            inputController = new DeathInputController(cubeRenderer, cubeState);
        }

        private string cubeOutput = string.Empty;
        private string lightOutput = string.Empty;
        public bool spawned = false;
        private int cubeSize = 200; // default size in pixels

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JS.InvokeVoidAsync("disableContextMenu", "RenderSurface");
                await JS.InvokeVoidAsync("registerCube", DotNetObjectReference.Create(this));

                cubeOutput = cubeRenderer.RenderCube(lightManager, cubeSize);

                lightOutput = lightManager.RenderLight();
                StateHasChanged();
            }
        }

        // --- Cube Controls ---
        public void RunCubeTest() => cubeRenderer.StartSpin(UpdateScene);
        public void ToggleSpinMode() => cubeRenderer.ToggleSpinMode(UpdateScene);

        // --- Face Selection ---
        public void SelectFrontFace()
        {
            inputController.SelectFrontFace();
            cubeState.SetHighlightedFace(0, "blue"); // front face highlight
            cubeOutput = cubeRenderer.RenderCube(lightManager, cubeSize);

            StateHasChanged();
        }

        public void SelectRightFace()
        {
            inputController.SelectRightFace();
            cubeState.SetHighlightedFace(2, "red"); // right face highlight
            cubeOutput = cubeRenderer.RenderCube(lightManager, cubeSize);

            StateHasChanged();
        }
        public void SelectBackFace()
        {
            inputController.SelectBackFace();
            cubeState.SetHighlightedFace(1, "green"); // back face highlight
            cubeOutput = cubeRenderer.RenderCube(lightManager, cubeSize);

            StateHasChanged();
        }
        public void SelectLeftFace()
        {
            inputController.SelectLeftFace();
            cubeState.SetHighlightedFace(3, "orange"); // left face highlight
            cubeOutput = cubeRenderer.RenderCube(lightManager, cubeSize);

            StateHasChanged();
        }
        public void SelectTopFace()
        {
            inputController.SelectTopFace();
            cubeState.SetHighlightedFace(4, "white"); // top face highlight
            cubeOutput = cubeRenderer.RenderCube(lightManager, cubeSize);

            StateHasChanged();
        }
        public void SelectBottomFace()
        {
            inputController.SelectBottomFace();
            cubeState.SetHighlightedFace(5, "yellow"); // bottom face highlight
            cubeOutput = cubeRenderer.RenderCube(lightManager, cubeSize);

            StateHasChanged();
        }
        public void ToggleScramble()
        {
            cubeState.Scramble();
            cubeOutput = cubeRenderer.RenderCube(lightManager, cubeSize);

            StateHasChanged();


        }

        public void ToggleAutoComplete()
        {
            cubeState.ResetToInitial();
            cubeOutput = cubeRenderer.RenderCube(lightManager, cubeSize);

            StateHasChanged();
            // Popup message

        }
        private void OnCubeSizeChanged(ChangeEventArgs e)
        {
            cubeSize = int.Parse(e.Value.ToString());
            UpdateScene();
        }

        // --- Lighting Controls ---
        public void ToggleLight() { lightManager.ToggleLight(); UpdateScene(); }
        public void MoveLightLeft() { lightManager.MoveLight(-20, 0); UpdateScene(); }
        public void MoveLightRight() { lightManager.MoveLight(20, 0); UpdateScene(); }
        public void MoveLightUp() { lightManager.MoveLight(0, -20); UpdateScene(); }
        public void MoveLightDown() { lightManager.MoveLight(0, 20); UpdateScene(); }

        // --- Mouse Events ---
        public void OnMouseDown(MouseEventArgs e)
        {
            inputController.OnMouseDown(e);
            cubeOutput = cubeRenderer.RenderCube(lightManager, cubeSize);

            StateHasChanged();
        }

        public void OnMouseUp(MouseEventArgs e) => inputController.OnMouseUp(e);

        public void OnMouseMove(MouseEventArgs e)
        {
            inputController.OnMouseMove(e);
            cubeOutput = cubeRenderer.RenderCube(lightManager, cubeSize);

            StateHasChanged();
        }

        // --- Scene Management ---
        public void ClearScene()
        {
            spawned = false;
            cubeRenderer.Clear();
            lightManager.Clear();
            cubeOutput = string.Empty;
            lightOutput = string.Empty;
            StateHasChanged();
        }

        private void ClearMoveLog()
        {
            cubeState.MoveLog.Clear();
            cubeState.MoveCount = 0;
            moveInfo = string.Empty;
            StateHasChanged();
        }


        private string moveInfo = string.Empty;
        private string solvedMessage = string.Empty;

        private void UpdateScene()
        {
            spawned = true;
            cubeOutput = cubeRenderer.RenderCube(lightManager, cubeSize);

            lightOutput = lightManager.RenderLight();

            moveInfo = $"Moves: {cubeState.MoveCount}<br/>" +
                       string.Join("<br/>", cubeState.MoveLog);

            // Solved check
            if (cubeState.IsSolved())
            {
                solvedMessage = "<div class='DiabloGlowRed'>Now you’re a Lizard Barry</div>";
            }
            else
            {
                solvedMessage = string.Empty;
            }

            StateHasChanged();
        }



    }
}
