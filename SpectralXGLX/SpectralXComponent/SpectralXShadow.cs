namespace SpectralXGLX.SpectralXComponent
{
    /// <summary>
    /// SpectralX Shadow System — tuneable shadow properties for SpectralXS modes
    /// Per-scene global shadow settings, sent to GPU each frame as uniforms
    /// </summary>
    public class SpectralXShadow
    {
        /// <summary>Controls penumbra spread — higher = softer wider shadows</summary>
        public float SoftnessBias { get; set; } = 0.015f;

        /// <summary>Blocker search radius — how wide to sample for shadow casters</summary>
        public float BlockerSearchRadius { get; set; } = 0.015f;

        /// <summary>PCF kernel scale — controls final shadow edge blur size</summary>
        public float KernelSize { get; set; } = 2.5f;

        /// <summary>Keeps contact shadows sharp — clamps minimum penumbra width</summary>
        public float ContactSharpness { get; set; } = 0.0005f;

        /// <summary>Depth bias — prevents shadow acne on surfaces</summary>
        public float DepthBias { get; set; } = 0.005f;

        /// <summary>Shadow color tint — RGB, default black, can be light-influenced</summary>
        public float TintR { get; set; } = 0.0f;
        public float TintG { get; set; } = 0.0f;
        public float TintB { get; set; } = 0.0f;

        /// <summary>Tint strength — 0 = pure black shadow, 1 = fully tinted</summary>
        public float TintStrength { get; set; } = 0.0f;

        /// <summary>Penumbra tint strength — how much the casting light color bleeds into shadow edges</summary>
        public float PenumbraTintStrength { get; set; } = 0.4f;
    }
}