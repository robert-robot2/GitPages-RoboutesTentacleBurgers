using SpectralXGLX.SpectralGL.Math;

using System.Numerics;

namespace SpectralXGLX.BWP
{
    public class SpectralXIsoCamera
    {
        // ── Configuration ────────────────────────────────────────────────────
        public float ScrollSpeed { get; set; } = 15f;
        public float ZoomLevel { get; set; } = 15f;      // fixed Z height
        public float PitchDeg { get; set; } = 60f;       // look down angle
        public float YawDeg { get; set; } = 45f;         // diamond orientation
        public int EdgeScrollMargin { get; set; } = 50;  // pixels from edge

        // ── State ────────────────────────────────────────────────────────────
        public float TargetX { get; set; } = 0f;
        public float TargetY { get; set; } = 0f;

        private float _mouseX = -1f;
        private float _mouseY = -1f;
        private bool _hasFirstMousePos = false;
        // ── Lock-to-Player State ────────────────────────────────────────────────
        public bool LockToPlayer { get; set; } = false;
        public float FollowSpeed { get; set; } = 4f; // higher = snappier follow

        // ── Derived Vectors (for debug / engine sync) ─────────────────────────
        public CustomVec3 Position
        {
            get
            {
                var (x, y, z) = GetPosition();
                return new CustomVec3(x, y, z);
            }
        }

        public CustomVec3 Forward
        {
            get
            {
                float pitchRad = PitchDeg * (MathF.PI / 180f);
                float yawRad = YawDeg * (MathF.PI / 180f);
                // Points from camera toward target
                return new CustomVec3(
                    MathF.Cos(yawRad) * MathF.Cos(pitchRad),
                    MathF.Sin(yawRad) * MathF.Cos(pitchRad),
                    -MathF.Sin(pitchRad)
                ).Normalized();
            }
        }

        public CustomVec3 Right
        {
            get
            {
                float yawRad = YawDeg * (MathF.PI / 180f);
                // Perpendicular to forward on the horizontal plane
                return new CustomVec3(
                    -MathF.Sin(yawRad),
                     MathF.Cos(yawRad),
                     0f
                ).Normalized();
            }
        }

        public CustomVec3 Up => Right.Cross(Forward).Normalized();

        // ── Mouse Feed ───────────────────────────────────────────────────────
        public void SetMousePosition(float x, float y)
        {
            // Guard against 0,0 DOM fire on load
            if (x <= 0f && y <= 0f) return;
            _mouseX = x;
            _mouseY = y;
            _hasFirstMousePos = true;
        }

        public void ResetMousePos()
        {
            _mouseX = -1f;
            _mouseY = -1f;
            _hasFirstMousePos = false;
        }
        // ── Tick ─────────────────────────────────────────────────────────────


        public void Tick(float delta, int viewW, int viewH, float? followX = null, float? followY = null)
        {
            if (LockToPlayer && followX.HasValue && followY.HasValue)
            {
                // Smooth lerp toward player world position — ignores edge-scroll entirely
                float t = 1f - MathF.Exp(-FollowSpeed * delta); // framerate-independent lerp
                TargetX += (followX.Value - TargetX) * t;
                TargetY += (followY.Value - TargetY) * t;
                return;
            }

            // Don't scroll until we have a real mouse position
            if (!_hasFirstMousePos) return;
            if (_mouseX < 0 || _mouseY < 0) return;

            if (_mouseX < EdgeScrollMargin)
                TargetY += ScrollSpeed * delta;   // left → up (flipped from down)
            if (_mouseX > viewW - EdgeScrollMargin)
                TargetY -= ScrollSpeed * delta;   // right → down

            if (_mouseY < EdgeScrollMargin)
                TargetX += ScrollSpeed * delta;   // up → right (flipped)
            if (_mouseY > viewH - EdgeScrollMargin)
                TargetX -= ScrollSpeed * delta;   // down → left
        }

        // ── View Matrix ──────────────────────────────────────────────────────
        public CustomMat4 GetViewMatrix()
        {
            float pitchRad = PitchDeg * (MathF.PI / 180f);
            float yawRad = YawDeg * (MathF.PI / 180f);

            // Camera position offset from target
            float cosYaw = MathF.Cos(yawRad);
            float sinYaw = MathF.Sin(yawRad);
            float cosPitch = MathF.Cos(pitchRad);
            float sinPitch = MathF.Sin(pitchRad);

            // Pull camera back and up from target point
            float camX = TargetX - cosYaw * cosPitch * ZoomLevel;
            float camY = TargetY - sinYaw * cosPitch * ZoomLevel;
            float camZ = ZoomLevel * sinPitch;

            var eye = new CustomVec3(camX, camY, camZ);
            var target = new CustomVec3(TargetX, TargetY, 0f);
            var up = new CustomVec3(0f, 0f, 1f);

            return CustomMat4.CreateLookAt(eye, target, up);
        }

        // ── Camera World Position (for engine sync) ───────────────────────
        public (float x, float y, float z) GetPosition()
        {
            float pitchRad = PitchDeg * (MathF.PI / 180f);
            float yawRad = YawDeg * (MathF.PI / 180f);

            float cosYaw = MathF.Cos(yawRad);
            float sinYaw = MathF.Sin(yawRad);
            float cosPitch = MathF.Cos(pitchRad);
            float sinPitch = MathF.Sin(pitchRad);

            float camX = TargetX - cosYaw * cosPitch * ZoomLevel;
            float camY = TargetY - sinYaw * cosPitch * ZoomLevel;
            float camZ = ZoomLevel * sinPitch;

            return (camX, camY, camZ);
        }
    }
}