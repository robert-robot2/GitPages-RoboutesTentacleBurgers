using SpectralXGLX.SpectralGL.Math;

namespace SpectralXGLX.SpectralXComponent
{
    /// <summary>
    /// Blender-style orbit camera for editor utility navigation.
    /// Maps to Ctrl+Numpad keybinds in your control scheme.
    ///
    /// Core concept: spherical coordinates around a pivot/target point.
    ///   - YawDeg   = horizontal rotation around pivot (left/right orbit)
    ///   - PitchDeg = vertical rotation around pivot (up/down orbit)
    ///   - Distance = radius from pivot to camera eye
    ///
    /// Preset views (Front/Back/Top/Bottom/Left/Right/Iso) snap these
    /// three values instantly and optionally switch projection mode.
    ///
    /// Entering Orbit from FreeCam or OrthoCamera initializes pivot and
    /// distance from the previous camera position so there is no jump.
    ///
    /// Projection: Perspective by default. Ctrl+5 toggles Ortho.
    /// When entering from OrthoCamera the projection starts as Ortho
    /// producing the "skewed ortho orbit" Blender behavior.
    /// Ctrl+7 (Top) snaps back to clean top-down ortho.
    /// </summary>
    public class SpectralXOrbitCamera
    {
        // ── Projection Mode ──────────────────────────────────────────────────
        public enum ProjectionMode { Perspective, Orthographic }

        /// <summary>Current projection mode. Ctrl+5 toggles.</summary>
        public ProjectionMode Projection { get; set; } = ProjectionMode.Perspective;

        // ── Named Views ───────────────────────────────────────────────────────
        public enum NamedView
        {
            Free,       // no preset — user has orbited freely
            Front,      // Ctrl+1 — looking from +Y toward -Y
            Back,       // Shift+Ctrl+1 — looking from -Y toward +Y
            Right,      // Ctrl+3 — looking from +X toward -X
            Left,       // Shift+Ctrl+3 — looking from -X toward +X
            Top,        // Ctrl+7 — looking from +Z toward -Z
            Bottom,     // Shift+Ctrl+7 — looking from -Z toward +Z
            Iso,        // standard isometric — 45 yaw, 35.26 pitch
            Camera,     // Ctrl+Slash — locked to scene active camera
        }

        /// <summary>Which named view is currently active.</summary>
        public NamedView ActiveView { get; private set; } = NamedView.Free;

        // ── Spherical Coordinates ─────────────────────────────────────────────
        /// <summary>Horizontal orbit angle in degrees. 0 = looking from +Y.</summary>
        public float YawDeg { get; set; } = 45f;

        /// <summary>
        /// Vertical orbit angle in degrees.
        /// 0 = equator (horizontal look), 90 = straight down, -90 = straight up.
        /// Clamped to -89/+89 to avoid gimbal flip.
        /// </summary>
        public float PitchDeg { get; set; } = 35.26f; // Blender default iso pitch

        /// <summary>Distance from pivot to camera eye in world units.</summary>
        public float Distance { get; set; } = 20f;

        public float MinDistance { get; set; } = 1f;
        public float MaxDistance { get; set; } = 500f;

        // ── Pivot / Target ────────────────────────────────────────────────────
        /// <summary>World space point the camera orbits around.</summary>
        public float PivotX { get; set; } = 0f;
        public float PivotY { get; set; } = 0f;
        public float PivotZ { get; set; } = 0f;

        // ── Ortho Size (used when Projection == Orthographic) ─────────────────
        /// <summary>
        /// Half-height of visible area in ortho mode.
        /// Linked to Distance so zooming feels consistent across modes.
        /// </summary>
        public float OrthoSize { get; set; } = 15f;
        public float MinOrthoSize { get; set; } = 1f;
        public float MaxOrthoSize { get; set; } = 200f;

        // ── Perspective Settings ──────────────────────────────────────────────
        public float FovDegrees { get; set; } = 90f;
        public float Near { get; set; } = 0.1f;
        public float Far { get; set; } = 2000f;

        // ── Orbit Step Size (Ctrl+2/4/6/8) ───────────────────────────────────
        /// <summary>Degrees to orbit per numpad step press.</summary>
        public float OrbitStepDeg { get; set; } = 15f;

        // ── Cached Matrices ───────────────────────────────────────────────────
        private CustomMat4 _cachedView = CustomMat4.Identity();
        private CustomMat4 _cachedProj = CustomMat4.Identity();
        private bool _viewDirty = true;
        private bool _projDirty = true;
        private float _lastAspect = 0f;

        // ── Initialization from Another Camera ───────────────────────────────

        /// <summary>
        /// Initialize orbit state from FreeCam position and forward vector.
        /// Derives pivot point at a reasonable distance in front of the camera
        /// so there is no positional jump when switching modes.
        /// Call from SetCameraMode() when switching to Orbit from FreeCam.
        /// </summary>
        public void InitFromFreeCam(CustomVec3 camPos, CustomVec3 camForward, float pivotDistance = 20f)
        {
            // Place pivot in front of the freecam at pivotDistance
            PivotX = camPos.X + camForward.X * pivotDistance;
            PivotY = camPos.Y + camForward.Y * pivotDistance;
            PivotZ = camPos.Z + camForward.Z * pivotDistance;

            // Derive yaw and pitch from the forward vector
            // so the view direction is preserved on mode switch
            float flatLen = MathF.Sqrt(
                camForward.X * camForward.X +
                camForward.Y * camForward.Y);

            PitchDeg = MathF.Atan2(-camForward.Z, flatLen) * (180f / MathF.PI);
            YawDeg = MathF.Atan2(camForward.X, camForward.Y) * (180f / MathF.PI);

            Distance = pivotDistance;
            OrthoSize = pivotDistance * 0.75f;
            ActiveView = NamedView.Free;
            MarkDirty();
        }

        /// <summary>
        /// Initialize orbit state from OrthoCamera target and size.
        /// Camera enters "skewed ortho orbit" — Blender behavior when
        /// orbiting out of a flat orthographic view.
        /// Call from SetCameraMode() when switching to Orbit from Ortho.
        /// </summary>
        public void InitFromOrthoCamera(float targetX, float targetY,
            float orthoSize, ProjectionMode inheritedProjection)
        {
            PivotX = targetX;
            PivotY = targetY;
            PivotZ = 0f;

            // Start from top-down orientation — user can then orbit freely
            YawDeg = 0f;
            PitchDeg = 89f; // near-top-down, slight tilt so it's not degenerate

            Distance = orthoSize * 2f;
            OrthoSize = orthoSize;

            // Inherit the ortho projection from the camera we came from
            // This produces the skewed ortho orbit Blender shows
            Projection = inheritedProjection;

            ActiveView = NamedView.Free;
            MarkDirty();
        }

        // ── Preset View Snaps ─────────────────────────────────────────────────

        /// <summary>Ctrl+1 — Front view. Looking from +Y toward -Y.</summary>
        public void SetFrontView()
        {
            YawDeg = 0f;
            PitchDeg = 0f;
            ActiveView = NamedView.Front;
            MarkDirty();
        }

        /// <summary>Shift+Ctrl+1 — Back view. Opposite of front.</summary>
        public void SetBackView()
        {
            YawDeg = 180f;
            PitchDeg = 0f;
            ActiveView = NamedView.Back;
            MarkDirty();
        }

        /// <summary>Ctrl+3 — Right side view. Looking from +X toward -X.</summary>
        public void SetRightView()
        {
            YawDeg = 90f;
            PitchDeg = 0f;
            ActiveView = NamedView.Right;
            MarkDirty();
        }

        /// <summary>Shift+Ctrl+3 — Left side view. Opposite of right.</summary>
        public void SetLeftView()
        {
            YawDeg = -90f;
            PitchDeg = 0f;
            ActiveView = NamedView.Left;
            MarkDirty();
        }

        /// <summary>
        /// Ctrl+7 — Top view. Looking straight down -Z.
        /// Automatically switches to Orthographic — matches Blender behavior.
        /// This is also the "return to clean top-down" escape from skewed ortho orbit.
        /// </summary>
        public void SetTopView()
        {
            YawDeg = 0f;
            PitchDeg = 89.9f; // 90 is degenerate with Z-up, use near-90
            Projection = ProjectionMode.Orthographic;
            ActiveView = NamedView.Top;
            MarkDirty();
        }

        /// <summary>Shift+Ctrl+7 — Bottom view. Looking straight up +Z.</summary>
        public void SetBottomView()
        {
            YawDeg = 0f;
            PitchDeg = -89.9f;
            Projection = ProjectionMode.Orthographic;
            ActiveView = NamedView.Bottom;
            MarkDirty();
        }

        /// <summary>
        /// Standard isometric view — 45 degree yaw, 35.26 degree pitch.
        /// Matches the classic Blender iso angle.
        /// </summary>
        public void SetIsoView()
        {
            YawDeg = 45f;
            PitchDeg = 35.26f;
            ActiveView = NamedView.Iso;
            MarkDirty();
        }

        /// <summary>
        /// Ctrl+9 — Opposite view. Flips current named view to its opposite.
        /// Front↔Back, Right↔Left, Top↔Bottom. Free view flips yaw 180.
        /// </summary>
        public void SetOppositeView()
        {
            switch (ActiveView)
            {
                case NamedView.Front: SetBackView(); break;
                case NamedView.Back: SetFrontView(); break;
                case NamedView.Right: SetLeftView(); break;
                case NamedView.Left: SetRightView(); break;
                case NamedView.Top: SetBottomView(); break;
                case NamedView.Bottom: SetTopView(); break;
                default:
                    // Free view — flip yaw 180 degrees
                    YawDeg = (YawDeg + 180f) % 360f;
                    PitchDeg = -PitchDeg;
                    MarkDirty();
                    break;
            }
        }

        // ── Orbit Steps (Ctrl+2/4/6/8) ────────────────────────────────────────

        /// <summary>Ctrl+8 — Orbit up (camera moves up, pivot stays fixed).</summary>
        public void OrbitUp()
        {
            PitchDeg = Math.Clamp(PitchDeg + OrbitStepDeg, -89f, 89f);
            ActiveView = NamedView.Free;
            MarkDirty();
        }

        /// <summary>Ctrl+2 — Orbit down.</summary>
        public void OrbitDown()
        {
            PitchDeg = Math.Clamp(PitchDeg - OrbitStepDeg, -89f, 89f);
            ActiveView = NamedView.Free;
            MarkDirty();
        }

        /// <summary>Ctrl+4 — Orbit left (camera moves left around pivot).</summary>
        public void OrbitLeft()
        {
            YawDeg = (YawDeg - OrbitStepDeg) % 360f;
            ActiveView = NamedView.Free;
            MarkDirty();
        }

        /// <summary>Ctrl+6 — Orbit right.</summary>
        public void OrbitRight()
        {
            YawDeg = (YawDeg + OrbitStepDeg) % 360f;
            ActiveView = NamedView.Free;
            MarkDirty();
        }

        // ── Projection Toggle (Ctrl+5) ────────────────────────────────────────

        /// <summary>
        /// Ctrl+5 — Toggle between Perspective and Orthographic projection.
        /// Does not change view direction — only affects how geometry is projected.
        /// </summary>
        public void ToggleProjection()
        {
            Projection = Projection == ProjectionMode.Perspective
                ? ProjectionMode.Orthographic
                : ProjectionMode.Perspective;
            _projDirty = true;
        }

        // ── Zoom ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Zoom in or out by adjusting Distance and OrthoSize together
        /// so switching projection modes feels consistent.
        /// Positive delta = zoom out, negative = zoom in.
        /// </summary>
        public void Zoom(float delta)
        {
            Distance = Math.Clamp(Distance + delta, MinDistance, MaxDistance);
            OrthoSize = Math.Clamp(OrthoSize + delta * 0.5f, MinOrthoSize, MaxOrthoSize);
            MarkDirty();
        }

        // ── Align Camera to Viewport (Ctrl+Alt+0) ────────────────────────────

        /// <summary>
        /// Ctrl+Alt+0 — Align camera to current viewport.
        /// Snaps the orbit pivot to the scene origin and resets to
        /// a clean perspective view at current distance.
        /// </summary>
        public void AlignToViewport()
        {
            PivotX = 0f;
            PivotY = 0f;
            PivotZ = 0f;
            Projection = ProjectionMode.Perspective;
            ActiveView = NamedView.Free;
            MarkDirty();
        }

        // ── View Matrix ───────────────────────────────────────────────────────

        /// <summary>
        /// Computes orbit view matrix from spherical coordinates.
        /// Camera eye position is derived from pivot + spherical offset.
        /// Uses CreateLookAt — clean, no gimbal issues at our angle ranges.
        /// </summary>
        public CustomMat4 GetViewMatrix()
        {
            if (!_viewDirty) return _cachedView;
            _viewDirty = false;

            float yawRad = YawDeg * (MathF.PI / 180f);
            float pitchRad = PitchDeg * (MathF.PI / 180f);

            float cosYaw = MathF.Cos(yawRad);
            float sinYaw = MathF.Sin(yawRad);
            float cosPitch = MathF.Cos(pitchRad);
            float sinPitch = MathF.Sin(pitchRad);

            // Spherical offset from pivot to eye
            float offsetX = Distance * sinYaw * cosPitch;
            float offsetY = Distance * cosYaw * cosPitch;
            float offsetZ = Distance * sinPitch;

            var eye = new CustomVec3(
                PivotX + offsetX,
                PivotY + offsetY,
                PivotZ + offsetZ);

            var target = new CustomVec3(PivotX, PivotY, PivotZ);

            // Safe up vector — avoid degenerate cross product near poles
            // When looking nearly straight down or up, use Y as up
            var up = MathF.Abs(sinPitch) > 0.95f
                ? new CustomVec3(0f, 1f, 0f)
                : new CustomVec3(0f, 0f, 1f);

            _cachedView = CustomMat4.CreateLookAt(eye, target, up);
            return _cachedView;
        }

        // ── Projection Matrix ─────────────────────────────────────────────────

        /// <summary>
        /// Returns perspective or orthographic projection matrix
        /// depending on current Projection mode.
        /// </summary>
        public CustomMat4 GetProjectionMatrix(float aspect)
        {
            if (!_projDirty && MathF.Abs(aspect - _lastAspect) < 0.0001f)
                return _cachedProj;

            _projDirty = false;
            _lastAspect = aspect;

            if (Projection == ProjectionMode.Orthographic)
            {
                float halfH = OrthoSize;
                float halfW = OrthoSize * aspect;
                _cachedProj = CustomMat4.CreateOrthographic(
                    -halfW, halfW, -halfH, halfH, -Far, Far);
            }
            else
            {
                _cachedProj = CustomMat4.CreatePerspective(
                    FovDegrees * (MathF.PI / 180f),
                    aspect, Near, Far);
            }

            return _cachedProj;
        }

        /// <summary>
        /// Returns both matrices in one call.
        /// Used by BuildWebGLFrame() as the single matrix source.
        /// </summary>
        public (CustomMat4 view, CustomMat4 proj) GetMatrices(float aspect)
        {
            return (GetViewMatrix(), GetProjectionMatrix(aspect));
        }

        // ── Camera World Position ─────────────────────────────────────────────

        /// <summary>
        /// Returns camera eye position in world space.
        /// Used by BuildWebGLFrame() for CamX/Y/Z.
        /// </summary>
        public (float x, float y, float z) GetPosition()
        {
            float yawRad = YawDeg * (MathF.PI / 180f);
            float pitchRad = PitchDeg * (MathF.PI / 180f);

            float cosYaw = MathF.Cos(yawRad);
            float sinYaw = MathF.Sin(yawRad);
            float cosPitch = MathF.Cos(pitchRad);
            float sinPitch = MathF.Sin(pitchRad);

            return (
                PivotX + Distance * sinYaw * cosPitch,
                PivotY + Distance * cosYaw * cosPitch,
                PivotZ + Distance * sinPitch
            );
        }

        /// <summary>Vec3 version of GetPosition for engine sync.</summary>
        public CustomVec3 Position
        {
            get
            {
                var (x, y, z) = GetPosition();
                return new CustomVec3(x, y, z);
            }
        }

        /// <summary>
        /// Returns projection matrix as float array for WebGLFrameData.ProjMatrix.
        /// </summary>
        public float[] GetProjectionMatrixArray(float aspect)
        {
            return GetProjectionMatrix(aspect).M;
        }

        // ── Dirty Flags ───────────────────────────────────────────────────────

        public void MarkDirty()
        {
            _viewDirty = true;
            _projDirty = true;
        }

        /// <summary>
        /// Resets orbit camera to clean default state.
        /// Call from SetCameraMode() when entering Orbit mode fresh.
        /// </summary>
        public void Reset()
        {
            YawDeg = 45f;
            PitchDeg = 35.26f;
            Distance = 20f;
            OrthoSize = 10f;
            PivotX = 0f;
            PivotY = 0f;
            PivotZ = 0f;
            Projection = ProjectionMode.Perspective;
            ActiveView = NamedView.Free;
            MarkDirty();
        }
    }
}