
namespace SpectralXGLX.Services
{
    /// <summary>
    /// Gamepad service - receives state from JavaScript and provides it to game code
    /// </summary>
    public class GamepadService : IAsyncDisposable
    {
        private readonly IJSRuntime _js;
        private DotNetObjectReference<GamepadService>? _objRef;

        // Current gamepad state
        public GamepadState State { get; private set; } = new();

        // Previous frame state for detecting button presses
        private GamepadState _previousState = new();

        public GamepadService(IJSRuntime js)
        {
            _js = js;
        }

        /// <summary>
        /// Initialize gamepad polling
        /// </summary>
        public async Task InitAsync()
        {
            try
            {
                _objRef = DotNetObjectReference.Create(this);
                await _js.InvokeVoidAsync("GamepadAPI.init", _objRef);
                Console.WriteLine("[GamepadService] Initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GamepadService] Init failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Called by JavaScript every frame with gamepad state
        /// </summary>
        [JSInvokable]
        public void UpdateGamepadState(GamepadState state)
        {
            _previousState = State;
            State = state;
        }

        /// <summary>
        /// Check if button was just pressed this frame
        /// </summary>
        public bool IsButtonPressed(string button)
        {
            return button switch
            {
                "A" => State.ButtonA && !_previousState.ButtonA,
                "B" => State.ButtonB && !_previousState.ButtonB,
                "X" => State.ButtonX && !_previousState.ButtonX,
                "Y" => State.ButtonY && !_previousState.ButtonY,
                "Start" => State.ButtonStart && !_previousState.ButtonStart,
                "Back" => State.ButtonBack && !_previousState.ButtonBack,
                "LeftBumper" => State.LeftBumper && !_previousState.LeftBumper,
                "RightBumper" => State.RightBumper && !_previousState.RightBumper,
                "LeftStick" => State.LeftStickButton && !_previousState.LeftStickButton,
                "RightStick" => State.RightStickButton && !_previousState.RightStickButton,
                "DpadUp" => State.DpadUp && !_previousState.DpadUp,
                "DpadDown" => State.DpadDown && !_previousState.DpadDown,
                "DpadLeft" => State.DpadLeft && !_previousState.DpadLeft,
                "DpadRight" => State.DpadRight && !_previousState.DpadRight,
                _ => false
            };
        }

        /// <summary>
        /// Check if button is currently held
        /// </summary>
        public bool IsButtonHeld(string button)
        {
            return button switch
            {
                "A" => State.ButtonA,
                "B" => State.ButtonB,
                "X" => State.ButtonX,
                "Y" => State.ButtonY,
                "Start" => State.ButtonStart,
                "Back" => State.ButtonBack,
                "LeftBumper" => State.LeftBumper,
                "RightBumper" => State.RightBumper,
                "LeftStick" => State.LeftStickButton,
                "RightStick" => State.RightStickButton,
                "DpadUp" => State.DpadUp,
                "DpadDown" => State.DpadDown,
                "DpadLeft" => State.DpadLeft,
                "DpadRight" => State.DpadRight,
                _ => false
            };
        }

        /// <summary>
        /// Get movement vector from left stick
        /// </summary>
        public Vector2 GetMovement()
        {
            return new Vector2(State.LeftStickX, State.LeftStickY);
        }

        /// <summary>
        /// Get look vector from right stick
        /// </summary>
        public Vector2 GetLook()
        {
            return new Vector2(State.RightStickX, State.RightStickY);
        }

        /// <summary>
        /// Check if any input is active
        /// </summary>
        public bool HasInput()
        {
            return State.LeftStickX != 0 || State.LeftStickY != 0 ||
                   State.RightStickX != 0 || State.RightStickY != 0 ||
                   State.ButtonA || State.ButtonB || State.ButtonX || State.ButtonY;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_js != null)
                {
                    await _js.InvokeVoidAsync("GamepadAPI.dispose");
                }
                _objRef?.Dispose();
            }
            catch { }
        }
    }

    /// <summary>
    /// Gamepad state snapshot
    /// </summary>
    public class GamepadState
    {
        // Left stick (movement)
        public float LeftStickX { get; set; }
        public float LeftStickY { get; set; }

        // Right stick (camera)
        public float RightStickX { get; set; }
        public float RightStickY { get; set; }

        // Face buttons
        public bool ButtonA { get; set; }
        public bool ButtonB { get; set; }
        public bool ButtonX { get; set; }
        public bool ButtonY { get; set; }

        // Shoulder buttons
        public bool LeftBumper { get; set; }
        public bool RightBumper { get; set; }

        // Triggers (0-1)
        public float LeftTrigger { get; set; }
        public float RightTrigger { get; set; }

        // Menu buttons
        public bool ButtonBack { get; set; }
        public bool ButtonStart { get; set; }

        // Stick buttons
        public bool LeftStickButton { get; set; }
        public bool RightStickButton { get; set; }

        // D-pad
        public bool DpadUp { get; set; }
        public bool DpadDown { get; set; }
        public bool DpadLeft { get; set; }
        public bool DpadRight { get; set; }
    }
}