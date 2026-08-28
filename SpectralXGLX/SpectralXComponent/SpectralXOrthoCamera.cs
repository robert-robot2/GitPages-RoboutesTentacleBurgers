using SpectralXGLX.SpectralGL.Math;

namespace SpectralXGLX.SpectralXComponent
{
    /// <summary>
    /// True orthographic top-down camera for BWP game demo scenes (Scene 4 and 5).
    /// Replaces SpectralXIsoCamera entirely.
    ///
    /// Projection: Orthographic — no perspective distortion, correct 2D sprite alignment.
    /// View: Looking straight down the -Z axis, Y forward, X right.
    /// Movement: WASD pans TargetX/Y on the world XY plane.
    /// Zoom: OrthoSize controls how many world units are visible half-height.
    /// Edge scroll: Mouse near canvas edge pans the target (same as old IsoCamera).
    /// Lock to player: When enabled, camera smoothly follows ActiveCharacter world position.
    /// </summary>
    public class SpectralXOrthoCamera
    {
        // ── Projection ───────────────────────────────────────────────────────
        /// <summary>
        /// Half-height of the visible world area in world units.
        /// Smaller = more zoomed in. Default 15 matches old IsoCamera ZoomLevel feel.
        /// </summary>
        public float OrthoSize { get; set; } = 10f;

        /// <summary>Minimum OrthoSize — most zoomed in.</summary>
        public float MinOrthoSize { get; set; } = 4f;

        /// <summary>Maximum OrthoSize — most zoomed out.</summary>
        public float MaxOrthoSize { get; set; } = 10f;

        /// <summary>Near clip plane for ortho projection.</summary>
        public float Near { get; set; } = -500f;

        /// <summary>Far clip plane for ortho projection.</summary>
        public float Far { get; set; } = 500f;

        /// <summary>
        /// Fixed camera height above the world XY plane.
        /// Camera position Z = this value always.
        /// </summary>
        public float CameraHeight { get; set; } = 100f;

        // ── Pan Target ───────────────────────────────────────────────────────
        /// <summary>World X coordinate the camera is centered on.</summary>
        public float TargetX { get; set; } = 0f;

        /// <summary>World Y coordinate the camera is centered on.</summary>
        public float TargetY { get; set; } = 0f;

        // ── Pan Speed ────────────────────────────────────────────────────────
        /// <summary>WASD pan speed in world units per second.</summary>
        public float PanSpeed { get; set; } = 15f;

        /// <summary>Edge scroll speed in world units per second.</summary>
        public float ScrollSpeed { get; set; } = 15f;

        /// <summary>Pixel margin from canvas edge that triggers edge scroll.</summary>
        public int EdgeScrollMargin { get; set; } = 50;

        // ── Player Lock ──────────────────────────────────────────────────────
        /// <summary>When true, camera smoothly follows the active character.</summary>
        public bool LockToPlayer { get; set; } = true;

        /// <summary>
        /// Lerp speed for player follow. Higher = snappier.
        /// Uses framerate-independent exponential lerp.
        /// </summary>
        public float FollowSpeed { get; set; } = 4f;

        // ── Mouse State ──────────────────────────────────────────────────────
        private float _mouseX = -1f;
        private float _mouseY = -1f;
        private bool _hasFirstMousePos = false;

        // ── Cached Matrices ──────────────────────────────────────────────────
        private CustomMat4 _cachedView = CustomMat4.Identity();
        private CustomMat4 _cachedProj = CustomMat4.Identity();
        private bool _viewDirty = true;
        private bool _projDirty = true;
        private float _lastAspect = 0f;

        // ── Mouse Feed ───────────────────────────────────────────────────────

        /// <summary>
        /// Called every frame from JS interop with raw canvas mouse coordinates.
        /// Guards against 0,0 DOM fire on page load.
        /// </summary>
        public void SetMousePosition(float x, float y)
        {
            if (x <= 0f && y <= 0f) return;
            _mouseX = x;
            _mouseY = y;
            _hasFirstMousePos = true;
        }

        /// <summary>
        /// Resets mouse tracking — call on scene switch or camera mode change
        /// to prevent stale coordinates triggering edge scroll on load.
        /// </summary>
        public void ResetMousePos()
        {
            _mouseX = -1f;
            _mouseY = -1f;
            _hasFirstMousePos = false;
        }

        // ── Zoom ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Adjusts OrthoSize by delta. Positive delta zooms out, negative zooms in.
        /// Clamps to MinOrthoSize / MaxOrthoSize.
        /// </summary>
        public void Zoom(float delta)
        {
            OrthoSize = Math.Clamp(OrthoSize + delta, MinOrthoSize, MaxOrthoSize);
            _projDirty = true;
        }

        // ── Tick ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Per-frame update. Handles player lock lerp and edge scroll.
        /// Call from SpectralXEngine.TickAndGetFrame() instead of IsoCamera.Tick().
        /// </summary>
        /// <param name="delta">Frame delta time in seconds.</param>
        /// <param name="viewW">Canvas width in pixels.</param>
        /// <param name="viewH">Canvas height in pixels.</param>
        /// <param name="followX">Player world X — only used when LockToPlayer is true.</param>
        /// <param name="followY">Player world Y — only used when LockToPlayer is true.</param>
        public void Tick(float delta, int viewW, int viewH,
            float? followX = null, float? followY = null)
        {
            if (LockToPlayer && followX.HasValue && followY.HasValue)
            {
                // Framerate-independent exponential lerp toward player position
                float t = 1f - MathF.Exp(-FollowSpeed * delta);
                TargetX += (followX.Value - TargetX) * t;
                TargetY += (followY.Value - TargetY) * t;
                _viewDirty = true;
                return;
            }

            // Edge scroll — only fires when we have a real mouse position
            if (!_hasFirstMousePos) return;
            if (_mouseX < 0 || _mouseY < 0) return;

            bool scrolled = false;

            if (_mouseX < EdgeScrollMargin)
            { TargetX -= ScrollSpeed * delta; scrolled = true; }
            if (_mouseX > viewW - EdgeScrollMargin)
            { TargetX += ScrollSpeed * delta; scrolled = true; }
            if (_mouseY < EdgeScrollMargin)
            { TargetY += ScrollSpeed * delta; scrolled = true; }  // was -=
            if (_mouseY > viewH - EdgeScrollMargin)
            { TargetY -= ScrollSpeed * delta; scrolled = true; }  // was +=

            if (scrolled) _viewDirty = true;
        }

        /// <summary>
        /// Pan the camera target directly in world space.
        /// Used by WASD in ProcessHeldKeys when in Orthographic mode.
        /// </summary>
        public void Pan(float dx, float dy)
        {
            TargetX += dx;
            TargetY += dy;
            _viewDirty = true;
        }

        // ── View Matrix ──────────────────────────────────────────────────────

        /// <summary>
        /// Orthographic view matrix — camera sits directly above TargetX/Y
        /// looking straight down the -Z axis. Z up, Y forward, X right.
        /// </summary>
        public CustomMat4 GetViewMatrix()
        {
            if (!_viewDirty) return _cachedView;
            _viewDirty = false;

            // Eye is directly above the target at fixed height
            var eye = new CustomVec3(TargetX, TargetY, CameraHeight);
            var target = new CustomVec3(TargetX, TargetY, 0f);
            // Up vector points in +Y (north on the map)
            var up = new CustomVec3(0f, 1f, 0f);

            _cachedView = CustomMat4.CreateLookAt(eye, target, up);
            return _cachedView;
        }

        // ── Projection Matrix ─────────────────────────────────────────────────

        /// <summary>
        /// True orthographic projection matrix.
        /// Half-height = OrthoSize, half-width = OrthoSize * aspect.
        /// No perspective distortion — sprites and tilemap align correctly.
        /// </summary>
        public CustomMat4 GetProjectionMatrix(float aspect)
        {
            if (!_projDirty && MathF.Abs(aspect - _lastAspect) < 0.0001f)
                return _cachedProj;

            _projDirty = false;
            _lastAspect = aspect;

            float halfH = OrthoSize;
            float halfW = OrthoSize * aspect;

            _cachedProj = CustomMat4.CreateOrthographic(
                -halfW, halfW,
                -halfH, halfH,
                Near, Far);

            return _cachedProj;
        }

        /// <summary>
        /// Returns both view and projection matrices in one call.
        /// Used by BuildWebGLFrame() as the single matrix source.
        /// </summary>
        public (CustomMat4 view, CustomMat4 proj) GetMatrices(float aspect)
        {
            return (GetViewMatrix(), GetProjectionMatrix(aspect));
        }

        // ── Camera World Position ─────────────────────────────────────────────

        /// <summary>
        /// Returns the camera eye position in world space.
        /// Used by BuildWebGLFrame() for CamX/Y/Z frame data fields.
        /// </summary>
        public (float x, float y, float z) GetPosition()
        {
            return (TargetX, TargetY, CameraHeight);
        }

        /// <summary>Vec3 version of GetPosition for engine sync.</summary>
        public CustomVec3 Position => new CustomVec3(TargetX, TargetY, CameraHeight);

        // ── Projection Array (for frameData.ProjMatrix) ───────────────────────

        /// <summary>
        /// Returns projection matrix as float array for WebGLFrameData.ProjMatrix.
        /// Matches the signature pattern of SpectralXCamera.GetProjectionMatrixArray().
        /// </summary>
        public float[] GetProjectionMatrixArray(float aspect)
        {
            return GetProjectionMatrix(aspect).M;
        }

        // ── Dirty Flags ───────────────────────────────────────────────────────

        /// <summary>
        /// Marks both matrices dirty — call after any property change that
        /// affects projection or view but doesn't go through the property setters.
        /// </summary>
        public void MarkDirty()
        {
            _viewDirty = true;
            _projDirty = true;
        }

        /// <summary>
        /// Resets camera to default state for a fresh scene load.
        /// Call from SwitchToScene() before InitScene4/5.
        /// </summary>
        public void Reset(float targetX = 0f, float targetY = 0f, float orthoSize = 10f)
        {
            TargetX = targetX;
            TargetY = targetY;
            OrthoSize = orthoSize;
            LockToPlayer = true;
            ResetMousePos();
            MarkDirty();
        }
    }
}