using SpectralXGLX.SpectralXComponent.SpectralXDebug;
using static SpectralXGLX.SpectralXComponent.SpectralXEngine;

namespace SpectralXGLX.SpectralXComponent
{
    public class SpectralXInput
    {
        public readonly SpectralXCanvas SpectralXGLX;
        public readonly IJSRuntime JS;
        public readonly SpectralXCamera Camera;
        public readonly SpectralXViewport Viewport;
        public SpectralXDebugRender Debug { get; set; } = default!;

        private readonly Dictionary<string, Func<Task>> binds = new(StringComparer.OrdinalIgnoreCase);

        private readonly List<SequenceBind> sequenceBinds = new();

        private readonly List<RecentKey> recentKeys = new();

        private readonly TimeSpan DefaultSequenceTimeout = TimeSpan.FromSeconds(2);

        private readonly HashSet<string> _heldKeys = new();

        private int _draggingLightIndex = -1;

        private bool _isShiftHeld = false;

        private bool isRightMouseDown = false;
        private bool isLeftMouseDown = false;
        private SpectralXEngine? _engine => SpectralXGLX.Engine;

        private readonly GamepadService _gamepad;
        public void ToggleDebugOverlay()
        {
            Debug.Enabled = !Debug.Enabled;

        }


        public SpectralXInput(SpectralXCanvas spectralX, SpectralXViewport viewport, SpectralXCamera camera, IJSRuntime js, GamepadService gamepad)
        {
            SpectralXGLX = spectralX;
            Viewport = viewport;
            Camera = camera;
            JS = js;
            _gamepad = gamepad;
            RegisterDefaultBinds();
        }

        private bool _scrollbarDragging = false;

        public void HandleScrollbarMouseDown(MouseEventArgs e)
        {
            if (_engine?.ActiveCameraMode != SpectralXEngine.CameraMode.WebpageView) return;

            // Only respond on Scene 3 (Home) or Scene 1
            if (_engine?.ActiveScene != SpectralXEngine.SceneID.Home) return;
            if (e.Button != 0) return;

            // OffsetX is in CSS pixels — compare against canvas CSS width
            float canvasCssWidth = Viewport.ViewportWidth;  // if Viewport tracks CSS px, use that
            float scrollbarStartX = canvasCssWidth * 0.96f;

            if (e.OffsetX >= scrollbarStartX)
            {
                _scrollbarDragging = true;
                float t = 1.0f - ((float)e.OffsetY / Viewport.ViewportHeight);
                float range = Camera.MaxZ - Camera.MinZ;
                Camera.TargetZ = Camera.MinZ + t * range;
                Console.WriteLine($"[Scrollbar] MouseDown hit — OffsetX:{e.OffsetX} t:{t} TargetZ:{Camera.TargetZ}");
            }
        }

        public void HandleScrollbarMouseMove(MouseEventArgs e)
        {
            if (_engine?.ActiveScene != SpectralXEngine.SceneID.Home) return;

            if (!_scrollbarDragging) return;
            if (_engine?.ActiveCameraMode != SpectralXEngine.CameraMode.WebpageView) return;

            float t = 1.0f - ((float)e.OffsetY / Viewport.ViewportHeight);
            Camera.TargetZ = Math.Clamp(Camera.MinZ + t * (Camera.MaxZ - Camera.MinZ), Camera.MinZ, Camera.MaxZ);
        }

        public void HandleScrollbarMouseUp(MouseEventArgs e)
        {
            _scrollbarDragging = false;
        }

        public void HandleWheel(WheelEventArgs e)
        {
            if ((_engine?.ActiveCameraMode == SpectralXEngine.CameraMode.Orthographic ||
      _engine?.ActiveCameraMode == SpectralXEngine.CameraMode.Orbit) &&
     _engine != null)
            {
                _engine.HandleIsoCameraScroll((float)e.DeltaY * 0.1f);
                return;
            }

            if (_engine?.ActiveCameraMode != SpectralXEngine.CameraMode.WebpageView) return;
            if (_engine?.ActiveScene != SpectralXEngine.SceneID.Home) return;

            Camera?.ScrollRail((float)e.DeltaY * -0.1f);
        }



        public async Task Register()
        {
            // Registration handled by SpectralXGLX.OnAfterRenderAsync
            await Task.CompletedTask;
        }

        /* ─────────────── BIND SYSTEM ─────────────── */
        public void Bind(string key, Func<Task> action)
        {
            binds[key] = action; // dictionary is case-insensitive now, no mangling needed
        }

        public void Unbind(string key)
        {
            binds.Remove(key);
        }

        public void BindSequence(
            string[] keys,
            Func<Task> action,
            TimeSpan? timeout = null,
            TimeSpan? cooldown = null)
        {
            sequenceBinds.Add(new SequenceBind
            {
                Keys = keys.Select(NormalizeKey).ToArray(),
                Action = action,
                Timeout = timeout ?? DefaultSequenceTimeout,
                LastTriggered = DateTime.MinValue,
                Cooldown = cooldown ?? TimeSpan.Zero,
                Progress = 0
            });
        }

      

        /* ─────────────── EXECUTE KEY / SEQUENCE ─────────────── */
        private async Task Execute(string key)
        {
            var now = DateTime.UtcNow;

          
            // Single key / combo binds
            if (binds.TryGetValue(key, out var action))
                await action.Invoke();

            // Add key to recent keys with timestamp
            recentKeys.Add(new RecentKey { Key = key, Time = now });

            // Remove old keys outside the longest timeout
            var maxTimeout = sequenceBinds.Any() ? sequenceBinds.Max(s => s.Timeout) : DefaultSequenceTimeout;
            recentKeys.RemoveAll(k => now - k.Time > maxTimeout);

            // Update sequence progress and trigger if complete
            foreach (var seqBind in sequenceBinds)
            {
                if (now - seqBind.LastTriggered < seqBind.Cooldown) continue;

                int seqLength = seqBind.Keys.Length;
                seqBind.Progress = 0;

                // Only check if we have enough keys
                if (recentKeys.Count >= 1)
                {
                    // Scan all possible start positions
                    for (int start = 0; start <= recentKeys.Count - 1; start++)
                    {
                        int progress = 0;

                        for (int i = 0; i < seqLength && (start + i) < recentKeys.Count; i++)
                        {
                            if (seqBind.Keys[i] == recentKeys[start + i].Key)
                                progress++;
                            else
                                break;
                        }

                        // Update the combo meter with max progress seen
                        if (progress > seqBind.Progress)
                            seqBind.Progress = progress;

                        if (progress == seqLength)
                        {
                            // Sequence completed!
                            await seqBind.Action.Invoke();
                            seqBind.LastTriggered = now;
                            recentKeys.Clear(); // ← ADD — prevents combo keys bleeding into next input
                            // ✅ Do NOT remove keys; allow overlapping sequences
                            break;
                        }
                    }
                }
            }



        }

        /* ─────────────── INPUT HANDLER ─────────────── */
        public async Task HandleKeyDown(KeyboardEventArgs e)
        {
            Console.WriteLine($"[Input] KeyDown: {e.Key}");
            if (e.ShiftKey) _isShiftHeld = true;

            var key = KeybindConfig.BuildCanonicalKey(e);
            var action = KeybindConfig.GetAction(key) ?? NormalizeKey(key);

            _heldKeys.Add(action);
            await Execute(action);
        }

        public void HandleKeyUp(KeyboardEventArgs e)
        {
            Console.WriteLine($"[Input] KeyUp: {e.Key}");
            if (!e.ShiftKey) _isShiftHeld = false;

            var key = KeybindConfig.BuildCanonicalKey(e);
            var action = KeybindConfig.GetAction(key) ?? NormalizeKey(key);
            _heldKeys.Remove(action);
        }



        public void ProcessHeldKeys()
        {
            if (_engine == null) return;

            bool isBWP = SpectralXEngine.SceneID.IsBWPScene(_engine.ActiveScene);

            // ── BWP Orthographic — character moves if spawned, camera pans if not ──
            if (isBWP && _engine.ActiveCameraMode == SpectralXEngine.CameraMode.Orthographic)
            {
                // Character movement takes priority when a character is spawned
                if (_engine.ActiveCharacter != null)
                {
                    var dir = Vector2.Zero;
                    if (_heldKeys.Contains("MoveUp")) dir.Y -= 1f;
                    if (_heldKeys.Contains("MoveDown")) dir.Y += 1f;
                    if (_heldKeys.Contains("MoveRight")) dir.X += 1f;
                    if (_heldKeys.Contains("MoveLeft")) dir.X -= 1f;

                    bool anyMovementKey = _heldKeys.Contains("MoveUp") || _heldKeys.Contains("MoveDown")
                                        || _heldKeys.Contains("MoveRight") || _heldKeys.Contains("MoveLeft");

                    if (dir != Vector2.Zero)
                    {
                        bool oneShotPlaying =
                            (_engine.ActiveCharacter is SpectralXBloodWarrior w && w.IsOneShotPlaying) ||
                            (_engine.ActiveCharacter is SpectralXRogue r && r.IsOneShotPlaying) ||
                            (_engine.ActiveCharacter is SpectralXMonk m && m.IsOneShotPlaying) ||
                            (_engine.ActiveCharacter is SpectralXMage g && g.IsOneShotPlaying);

                        if (!oneShotPlaying)
                        {
                            dir = Vector2.Normalize(dir);
                            _engine.ActiveCharacter.Move(dir);
                        }
                    }
                    else if (!anyMovementKey)
                    {
                        _engine.ActiveCharacter.Stop();
                    }
                }
                else
                {
                    // ── No character — WASD pans the ortho camera freely ──────────────
                    // Useful for inspecting lights and terrain before spawning
                    float panSpeed = 0.15f;
                    if (_heldKeys.Contains("MoveUp")) _engine.OrthoCamera.Pan(0f, -panSpeed);
                    if (_heldKeys.Contains("MoveDown")) _engine.OrthoCamera.Pan(0f, panSpeed);
                    if (_heldKeys.Contains("MoveLeft")) _engine.OrthoCamera.Pan(-panSpeed, 0f);
                    if (_heldKeys.Contains("MoveRight")) _engine.OrthoCamera.Pan(panSpeed, 0f);
                }
                return;
            }

            // ── BWP FreeCam — full freecam movement for light inspection ─────────────
            // Switch to freecam via keybind to fly around and check shadow/light positions
            if (isBWP && _engine.ActiveCameraMode == SpectralXEngine.CameraMode.FreeCam)
            {
                if (_heldKeys.Contains("MoveUp")) Camera?.MoveBackward();
                if (_heldKeys.Contains("MoveDown")) Camera?.MoveForward();
                if (_heldKeys.Contains("MoveLeft")) Camera?.StrafeLeft();
                if (_heldKeys.Contains("MoveRight")) Camera?.StrafeRight();
                return;
            }
            /*
            // ── Orbit camera — WASD pans orbit target ────────────────────────────────
            if (_engine.ActiveCameraMode == SpectralXEngine.CameraMode.Orbit)
            {
                float panSpeed = _engine.OrbitCamera.PanSpeed * 0.016f;
                if (_heldKeys.Contains("MoveUp")) _engine.OrbitCamera.Pan(0f, panSpeed);
                if (_heldKeys.Contains("MoveDown")) _engine.OrbitCamera.Pan(0f, -panSpeed);
                if (_heldKeys.Contains("MoveLeft")) _engine.OrbitCamera.Pan(-panSpeed, 0f);
                if (_heldKeys.Contains("MoveRight")) _engine.OrbitCamera.Pan(panSpeed, 0f);
                return;
            }
            */
            // ── FreeCam — standard movement ──────────────────────────────────────────
            if (_engine.ActiveCameraMode == SpectralXEngine.CameraMode.FreeCam)
            {
                if (_heldKeys.Contains("MoveUp")) Camera?.MoveBackward();
                if (_heldKeys.Contains("MoveDown")) Camera?.MoveForward();
                if (_heldKeys.Contains("MoveLeft")) Camera?.StrafeLeft();
                if (_heldKeys.Contains("MoveRight")) Camera?.StrafeRight();
            }
        }
        // ── Add this private helper to SpectralXInput ─────────────────────────────
        private IReadOnlyList<SpectralXLight> GetActiveLights()
        {
            if (_engine == null) return new List<SpectralXLight>();
            return _engine.ActiveScene switch
            {
                SpectralXEngine.SceneID.SpectralXDemo => _engine.Scene.Lights,
                SpectralXEngine.SceneID.SpectralXTown => _engine.Scene2.Lights,
                SpectralXEngine.SceneID.Home => _engine.Scene3.Lights,
                SpectralXEngine.SceneID.BWPScene1 => _engine.Scene4.Lights,
                SpectralXEngine.SceneID.BWPScene2 => _engine.Scene5.Lights,
                SpectralXEngine.SceneID.BWPScene3 => _engine.Scene6.Lights,
                SpectralXEngine.SceneID.BWPScene4 => _engine.Scene7.Lights,
                SpectralXEngine.SceneID.BWPScene5 => _engine.Scene8.Lights,
                SpectralXEngine.SceneID.BWPScene6 => _engine.Scene9.Lights,
                SpectralXEngine.SceneID.BWPScene7 => _engine.Scene10.Lights,
                SpectralXEngine.SceneID.BWPScene8 => _engine.Scene11.Lights,
                SpectralXEngine.SceneID.BWPScene9 => _engine.Scene12.Lights,
                SpectralXEngine.SceneID.BWPScene10 => _engine.Scene13.Lights,
                SpectralXEngine.SceneID.BWPScene11 => _engine.Scene14.Lights,
                _ => _engine.Scene.Lights,
            };
        }

        private double lastMouseX = 0;
        private double lastMouseY = 0;
        private Vector3 _dragStartWorldPos;

        public async Task HandleMouseDown(MouseEventArgs e)
        {
            if (e.Button == 2)
            {
                isRightMouseDown = true;
                lastMouseX = e.ClientX;
                lastMouseY = e.ClientY;
            }

            if (e.Button == 0)
            {
                isLeftMouseDown = true;
                _draggingLightIndex = -1;

                if (_engine != null)
                {
                    var lights = GetActiveLights();

                    for (int i = 0; i < lights.Count; i++)
                    {
                        var (sx, sy) = _engine.ProjectToScreen(lights[i].Position);
                        double dx = e.OffsetX - sx;
                        double dy = e.OffsetY - sy;

                        if (dx * dx + dy * dy < 625) // 25px radius
                        {
                            _draggingLightIndex = i;
                            _dragStartWorldPos = lights[i].Position;
                            lastMouseX = e.ClientX;
                            lastMouseY = e.ClientY;
                            break;
                        }
                    }
                }

                // Only paint if not dragging a light
                if (_draggingLightIndex == -1 && _engine != null &&
                    (_engine.ActiveScene == 2 || SpectralXEngine.SceneID.IsBWPScene(_engine.ActiveScene) ||
                     _engine.ActiveScene == SpectralXEngine.SceneID.Home))
                    _engine.HandleTileMapMouseDown((float)e.OffsetX, (float)e.OffsetY);
            }

            await Task.CompletedTask;
        }

        public async Task HandleMouseUp(MouseEventArgs e)
        {

            if (e.Button == 2) isRightMouseDown = false;
            if (e.Button == 0)
            {
                isLeftMouseDown = false;
                bool wasDraggingLight = _draggingLightIndex >= 0;
                _draggingLightIndex = -1;
                if (!wasDraggingLight && _engine != null &&
       (_engine.ActiveScene == 2 ||
        SpectralXEngine.SceneID.IsBWPScene(_engine.ActiveScene) ||
          _engine.ActiveScene == SpectralXEngine.SceneID.Home))
                    _engine.HandleTileMapMouseUp();
            }

            await Task.CompletedTask;
        }


        public async Task HandleMouseMove(MouseEventArgs e)
        {
            if (_engine?.ActiveCameraMode == SpectralXEngine.CameraMode.Orthographic &&
                _engine != null && SpectralXEngine.SceneID.IsBWPScene(_engine.ActiveScene) &&
                e.OffsetX > 0 && e.OffsetY > 0)
            {
                _engine.HandleIsoCameraMouseMove((float)e.OffsetX, (float)e.OffsetY);
            }

            if (isRightMouseDown)
            {
                double deltaX = e.ClientX - lastMouseX;
                double deltaY = e.ClientY - lastMouseY;
                Camera?.Look((float)-deltaX, (float)-deltaY);
                lastMouseX = e.ClientX;
                lastMouseY = e.ClientY;
            }
            else if (isLeftMouseDown && _draggingLightIndex >= 0 && _engine != null)
            {
                var lights = GetActiveLights();

                if (_draggingLightIndex < lights.Count)
                {
                    var light = lights[_draggingLightIndex];

                    float deltaX = (float)(e.ClientX - lastMouseX);
                    float deltaY = (float)(e.ClientY - lastMouseY);

                    float sensitivity = 0.02f;

                    light.Position = new Vector3(
                        light.Position.X + deltaX * sensitivity,
                        light.Position.Y + (_isShiftHeld ? deltaY * -sensitivity : 0f),
                        light.Position.Z + (!_isShiftHeld ? deltaY * -sensitivity : 0f)
                    );

                    lastMouseX = e.ClientX;
                    lastMouseY = e.ClientY;
                }
            }

            if (_draggingLightIndex == -1 && _engine != null &&
                (_engine.ActiveScene == 2 ||
                 SpectralXEngine.SceneID.IsBWPScene(_engine.ActiveScene) ||
                 _engine.ActiveScene == SpectralXEngine.SceneID.Home))
            {
                _engine.HandleTileMapMouseMove((float)e.OffsetX, (float)e.OffsetY);
            }

            await Task.CompletedTask;
        }

        public Task PreventContextMenu(MouseEventArgs e)
        {
          
            return Task.CompletedTask;
        }


        private string BuildKey(KeyboardEventArgs e)
        {
            string key = e.Key switch
            {
                "Escape" => "ESCAPE",
                "ArrowUp" => "UP",
                "ArrowDown" => "DOWN",
                "ArrowLeft" => "LEFT",
                "ArrowRight" => "RIGHT",
                "w" => "W",
                "a" => "A",
                "s" => "S",
                "d" => "D",
                "3" => "3",
                _ => e.Key.ToUpper()
            };

            List<string> keys = new();
            if (e.CtrlKey) keys.Add("CTRL");
            if (e.ShiftKey) keys.Add("SHIFT");
            if (e.AltKey) keys.Add("ALT");

            keys.Add(key);
            return string.Join("+", keys);
        }


        private string NormalizeKey(string key)
        {
            return key
                .ToUpper()
                .Replace(" ", "")
                .Replace("_", "+")
                .Replace("ESC", "ESCAPE");
        }

        public int GetComboProgressPercent()
        {
            if (!sequenceBinds.Any()) return 0;
            var maxSeq = sequenceBinds.OrderByDescending(s => s.Progress).First();
            return maxSeq.Keys.Length == 0 ? 0 : (int)(100.0 * maxSeq.Progress / maxSeq.Keys.Length);
        }

        public List<string> DebugRecentKeys => recentKeys
                                        .Skip(Math.Max(0, recentKeys.Count - 10))
                                        .Select(k => k.Key)
                                        .ToList();

        public List<string> DebugActiveSequences()
        {
            return sequenceBinds
                   .Where(s => s.Progress > 0)
                   .Select(s => string.Join(" + ", s.Keys))
                   .ToList();
        }
        public List<string> DebugMessages { get; } = new();

        /* ─────────────── DEFAULT BINDS ─────────────── */

        private void RegisterDefaultBinds()
        {
           
            Bind("Debug", async () => {
                ToggleDebugOverlay();
                await Task.CompletedTask;
            });

            Bind("UIToggle", async () => {
                SpectralXGLX.ToggleUIHidden();
                await Task.CompletedTask;
            });

            Bind("Menu", async () => {
                SpectralXGLX.ToggleBWPMenu();
                await Task.CompletedTask;
            });

            Bind("HUDToggle", async () => {   
                SpectralXGLX.ToggleBWPHUD();
                await Task.CompletedTask;
            });

            Bind("Inventory", async () => {
                SpectralXGLX.ToggleBWPInventory();
                await Task.CompletedTask;
            });
            Bind("LandscapeToggle", async () => {
                SpectralXGLX.ToggleLandscapePanel();
                await Task.CompletedTask;
            });
            Bind("Attack", async () => {
                if (_engine != null && SpectralXEngine.SceneID.IsBWPScene(_engine.ActiveScene))
                    _engine.ActiveCharacter?.CharAttack(_engine.LevelSystem, _engine.GetAllAttackableTargets());
                await Task.CompletedTask;
            });

            Bind("SPAttack", async () => {
                if (_engine != null && SpectralXEngine.SceneID.IsBWPScene(_engine.ActiveScene))
                    _engine.ActiveCharacter?.CharSpecialAttack(_engine.LevelSystem, _engine.GetAllAttackableTargets());
                await Task.CompletedTask;
            });
            BindSequence(new[] { "MoveRight", "Attack" }, async () => {
                if (_engine != null && SpectralXEngine.SceneID.IsBWPScene(_engine.ActiveScene))
                    _engine.ActiveCharacter?.CharAttack(_engine.LevelSystem, _engine.GetAllAttackableTargets(), forceRight: false); 
                await Task.CompletedTask;
            }, timeout: TimeSpan.FromMilliseconds(300));

            BindSequence(new[] { "MoveLeft", "Attack" }, async () => {
                if (_engine != null && SpectralXEngine.SceneID.IsBWPScene(_engine.ActiveScene))
                    _engine.ActiveCharacter?.CharAttack(_engine.LevelSystem, _engine.GetAllAttackableTargets(), forceRight: true);  
                await Task.CompletedTask;
            }, timeout: TimeSpan.FromMilliseconds(300));

            BindSequence(new[] { "MoveRight", "SPAttack" }, async () => {
                if (_engine != null && SpectralXEngine.SceneID.IsBWPScene(_engine.ActiveScene))
                    _engine.ActiveCharacter?.CharSpecialAttack(_engine.LevelSystem, _engine.GetAllAttackableTargets(), forceRight: false);  
                await Task.CompletedTask;
            }, timeout: TimeSpan.FromMilliseconds(300));

            BindSequence(new[] { "MoveLeft", "SPAttack" }, async () => {
                if (_engine != null && SpectralXEngine.SceneID.IsBWPScene(_engine.ActiveScene))
                    _engine.ActiveCharacter?.CharSpecialAttack(_engine.LevelSystem, _engine.GetAllAttackableTargets(), forceRight: true);  
                await Task.CompletedTask;
            }, timeout: TimeSpan.FromMilliseconds(300));
            // Example combos
            BindSequence(new[] { "UP", "UP" }, async () => {
                DebugMessages.Add("Double UP!");
                await Task.CompletedTask;
            });

            BindSequence(new[] { "UP", "UP", "DOWN", "DOWN" }, async () => {
                DebugMessages.Add("Full Konami!");
                await Task.CompletedTask;
            }, timeout: TimeSpan.FromSeconds(3));

            BindSequence(new[] { "LEFT", "RIGHT" }, async () => {
                DebugMessages.Add("Left-Right combo!");
                await Task.CompletedTask;
            }, cooldown: TimeSpan.FromSeconds(1));

            // ── Camera Mode Switches ──────────────────────────────────────────────
            Bind("CamModeFreeCam", async () => {
                _engine?.SetCameraMode(SpectralXEngine.CameraMode.FreeCam);
                await Task.CompletedTask;
            });

            Bind("CamModeOrbit", async () => {
                _engine?.SetCameraMode(SpectralXEngine.CameraMode.Orbit);
                await Task.CompletedTask;
            });

            Bind("CamModeOrtho", async () => {
                _engine?.SetCameraMode(SpectralXEngine.CameraMode.Orthographic);
                await Task.CompletedTask;
            });

            Bind("CamModeWebpage", async () => {
                _engine?.SetCameraMode(SpectralXEngine.CameraMode.WebpageView);
                await Task.CompletedTask;
            });

            // ── Orbit Preset Views ────────────────────────────────────────────────
            Bind("CamFront", async () => {
                if (_engine?.ActiveCameraMode != SpectralXEngine.CameraMode.Orbit)
                    _engine?.SetCameraMode(SpectralXEngine.CameraMode.Orbit);
                _engine?.OrbitCamera.SetFrontView();
                await Task.CompletedTask;
            });

            Bind("CamBack", async () => {
                if (_engine?.ActiveCameraMode != SpectralXEngine.CameraMode.Orbit)
                    _engine?.SetCameraMode(SpectralXEngine.CameraMode.Orbit);
                _engine?.OrbitCamera.SetBackView();
                await Task.CompletedTask;
            });

            Bind("CamRight", async () => {
                if (_engine?.ActiveCameraMode != SpectralXEngine.CameraMode.Orbit)
                    _engine?.SetCameraMode(SpectralXEngine.CameraMode.Orbit);
                _engine?.OrbitCamera.SetRightView();
                await Task.CompletedTask;
            });

            Bind("CamLeft", async () => {
                if (_engine?.ActiveCameraMode != SpectralXEngine.CameraMode.Orbit)
                    _engine?.SetCameraMode(SpectralXEngine.CameraMode.Orbit);
                _engine?.OrbitCamera.SetLeftView();
                await Task.CompletedTask;
            });

            Bind("CamTop", async () => {
                if (_engine?.ActiveCameraMode != SpectralXEngine.CameraMode.Orbit)
                    _engine?.SetCameraMode(SpectralXEngine.CameraMode.Orbit);
                _engine?.OrbitCamera.SetTopView();
                await Task.CompletedTask;
            });

            Bind("CamBottom", async () => {
                if (_engine?.ActiveCameraMode != SpectralXEngine.CameraMode.Orbit)
                    _engine?.SetCameraMode(SpectralXEngine.CameraMode.Orbit);
                _engine?.OrbitCamera.SetBottomView();
                await Task.CompletedTask;
            });

            Bind("CamToggleOrtho", async () => {
                if (_engine?.ActiveCameraMode == SpectralXEngine.CameraMode.Orbit)
                    _engine?.OrbitCamera.ToggleProjection();
                await Task.CompletedTask;
            });

            Bind("CamOppositeView", async () => {
                if (_engine?.ActiveCameraMode == SpectralXEngine.CameraMode.Orbit)
                    _engine?.OrbitCamera.SetOppositeView();
                await Task.CompletedTask;
            });

            Bind("CamAlignToViewport", async () => {
                if (_engine?.ActiveCameraMode == SpectralXEngine.CameraMode.Orbit)
                    _engine?.OrbitCamera.AlignToViewport();
                await Task.CompletedTask;
            });

            // ── Orbit Step Binds ─────────────────────────────────────────────────
            Bind("CamOrbitUp", async () => {
                if (_engine?.ActiveCameraMode == SpectralXEngine.CameraMode.Orbit)
                    _engine?.OrbitCamera.OrbitUp();
                await Task.CompletedTask;
            });

            Bind("CamOrbitDown", async () => {
                if (_engine?.ActiveCameraMode == SpectralXEngine.CameraMode.Orbit)
                    _engine?.OrbitCamera.OrbitDown();
                await Task.CompletedTask;
            });

            Bind("CamOrbitLeft", async () => {
                if (_engine?.ActiveCameraMode == SpectralXEngine.CameraMode.Orbit)
                    _engine?.OrbitCamera.OrbitLeft();
                await Task.CompletedTask;
            });

            Bind("CamOrbitRight", async () => {
                if (_engine?.ActiveCameraMode == SpectralXEngine.CameraMode.Orbit)
                    _engine?.OrbitCamera.OrbitRight();
                await Task.CompletedTask;
            });

            // ── Ortho Player Lock Toggle ─────────────────────────────────────────
            Bind("CamOrthoLockPlayer", async () => {
                if (_engine?.ActiveCameraMode == SpectralXEngine.CameraMode.Orthographic)
                    _engine.OrthoCamera.LockToPlayer = !_engine.OrthoCamera.LockToPlayer;
                await Task.CompletedTask;
            });

            // ── Zoom (Orbit + Ortho) ─────────────────────────────────────────────
            Bind("CamZoomIn", async () => {
                if (_engine?.ActiveCameraMode == SpectralXEngine.CameraMode.Orbit)
                    _engine?.OrbitCamera.Zoom(-2f);
                else if (_engine?.ActiveCameraMode == SpectralXEngine.CameraMode.Orthographic)
                    _engine?.OrthoCamera.Zoom(-2f);
                await Task.CompletedTask;
            });

            Bind("CamZoomOut", async () => {
                if (_engine?.ActiveCameraMode == SpectralXEngine.CameraMode.Orbit)
                    _engine?.OrbitCamera.Zoom(2f);
                else if (_engine?.ActiveCameraMode == SpectralXEngine.CameraMode.Orthographic)
                    _engine?.OrthoCamera.Zoom(2f);
                await Task.CompletedTask;
            });


        }



        public void HandleGamepadInput()
        {
            if (_engine == null) return;

            // ── Free cam — left stick moves camera, right stick looks ──────────
            if (_engine.ActiveCameraMode == SpectralXEngine.CameraMode.FreeCam)
            {
                var movement = _gamepad.GetMovement();
                if (movement.Y < -0.3f) Camera.MoveForward();
                if (movement.Y > 0.3f) Camera.MoveBackward();
                if (movement.X < -0.3f) Camera.StrafeRight();
                if (movement.X > 0.3f) Camera.StrafeLeft();

                var look = _gamepad.GetLook();
                if (System.Math.Abs(look.X) > 0.1f || System.Math.Abs(look.Y) > 0.1f)
                    Camera.Look(look.X * 5f, look.Y * 5f);

                return;
            }

            // ── Ortho / BWP — left stick moves character, face buttons attack ──
            if (_engine.ActiveCameraMode == SpectralXEngine.CameraMode.Orthographic &&
                SpectralXEngine.SceneID.IsBWPScene(_engine.ActiveScene) &&
                _engine.ActiveCharacter != null)
            {
                var movement = _gamepad.GetMovement();
                var dir = Vector2.Zero;

                if (MathF.Abs(movement.X) > 0.2f) dir.X = -movement.X;
                if (MathF.Abs(movement.Y) > 0.2f) dir.Y = -movement.Y;

                if (dir != Vector2.Zero)
                {
                    dir = Vector2.Normalize(dir);

                    bool oneShotPlaying =
                        (_engine.ActiveCharacter is SpectralXBloodWarrior w && w.IsOneShotPlaying) ||
                        (_engine.ActiveCharacter is SpectralXRogue r && r.IsOneShotPlaying) ||
                        (_engine.ActiveCharacter is SpectralXMonk m && m.IsOneShotPlaying) ||
                        (_engine.ActiveCharacter is SpectralXMage g && g.IsOneShotPlaying);

                    if (!oneShotPlaying)
                        _engine.ActiveCharacter.Move(dir);
                }
                else
                {
                    _engine.ActiveCharacter.Stop();
                }

                if (_gamepad.IsButtonPressed(GamepadBindsConfig.GetBinding("Attack")))
                    _engine.ActiveCharacter.CharAttack(_engine.LevelSystem, _engine.GetAllAttackableTargets());

                if (_gamepad.IsButtonPressed(GamepadBindsConfig.GetBinding("SPAttack")))
                    _engine.ActiveCharacter.CharSpecialAttack(_engine.LevelSystem, _engine.GetAllAttackableTargets());
            }
        }








        /* ─────────────── HELPERS ─────────────── */
        private class RecentKey
        {
            public string Key { get; set; } = string.Empty;
            public DateTime Time { get; set; }
        }

        private class SequenceBind
        {
            public string[] Keys { get; set; } = Array.Empty<string>();
            public Func<Task> Action { get; set; } = null!;
            public TimeSpan Timeout { get; set; }
            public DateTime LastTriggered { get; set; }
            public TimeSpan Cooldown { get; set; }
            public int Progress { get; set; } = 0;
        }
    }
}
