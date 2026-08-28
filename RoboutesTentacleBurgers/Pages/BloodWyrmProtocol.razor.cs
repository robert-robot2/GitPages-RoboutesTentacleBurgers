using SpectralXGLX.BWP;
using SpectralXGLX.Services;

namespace RoboutesTentacleBurgers.Pages
{
    public partial class BloodWyrmProtocol : ComponentBase
    {

        private SpectralXGLX.SpectralXCanvas? EngineComponent;
        private SpectralXGLX.SpectralXComponent.SpectralXEngine? Engine => EngineComponent?.Engine;
        private SpectralLevel _spectralLevel = new();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {               

                if (EngineComponent != null)
                {
                    EngineComponent.OnMeshesReady += OnEngineReady;
                    {                      
                    
                        if (!EngineComponent.BWPMenuVisible)
                            EngineComponent.ToggleBWPMenu();
                    };
                }
            }
        }
     

        private void OnEngineReady()
        {
            EngineComponent?.Engine?.SwitchToScene(
                SpectralXGLX.SpectralXComponent.SpectralXEngine.SceneID.BWPScene1);
        }

        public void Dispose()
        {
           

            if (EngineComponent != null)
                EngineComponent.OnMeshesReady -= OnEngineReady; // prevent accumulating handlers on re-nav
        }


        private bool _showCharSelect = false;

        // Replace old HandleSinglePlayer
        private void HandleSinglePlayer()
        {
            _showCharSelect = true;
        }

        private void HandleCharSelectBack()
        {
            EngineComponent?.Engine?.DespawnActiveCharacter();
            _showCharSelect = false;
        }

        private void HandleCharSelected(string charKey)
        {
            EngineComponent?.Engine?.DespawnActiveCharacter(); // clear previous if any
            switch (charKey)
            {
                case "Warrior": EngineComponent?.Engine?.SpawnWarrior(); break;
                case "Rogue": EngineComponent?.Engine?.SpawnRogue(); break;
                case "Monk": EngineComponent?.Engine?.SpawnMonk(); break;
                case "Mage": EngineComponent?.Engine?.SpawnMage(); break;
            }
        }

     
        private void HandleCharStart()
        {
            _showCharSelect = false;
            EngineComponent?.ToggleBWPMenu();
        }

        private bool _showOptions = false;

        private void HandleBWPOptions()
        {
            _showOptions = true;
        }
        private void HandleOptionsBack()
        {
            _showOptions = false;
        }

        private bool _showControls = false;

        private void HandleBWPControls() { _showControls = true; }
        private void HandleControlsBack() { _showControls = false; }

        private void HandleBWPQuit()
        {
            NavManager.NavigateTo("/");
        }


    }
}
