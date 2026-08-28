using System.Numerics;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// Describes a static prop type: its pixel size (for world-scale conversion)
    /// and its world-space collision radius for overlap avoidance during scatter.
    /// Mirrors the old engine's IStatic concept but scoped to the
    /// billboard/PrimSquare pattern the new WebGL2 engine uses.
    /// </summary>
    public interface ISpectralProps
    {
        string Type { get; }
        float PixelWidth { get; }
        float PixelHeight { get; }
        float CollisionRadius { get; }
    }

    /// <summary>
    /// Default plain-data implementation of ISpectralProps.
    /// </summary>
    public readonly struct SpectralPropDef : ISpectralProps
    {
        public string Type { get; }
        public float PixelWidth { get; }
        public float PixelHeight { get; }
        public float CollisionRadius { get; }

        public SpectralPropDef(string type, float pixelWidth, float pixelHeight, float collisionRadius)
        {
            Type = type;
            PixelWidth = pixelWidth;
            PixelHeight = pixelHeight;
            CollisionRadius = collisionRadius;
        }
    }

    /// <summary>
    /// A single fixed-position placement — used for buildings/landmarks that
    /// need an exact spot rather than random scatter (e.g. a specific tavern
    /// or house in Town). WorldScale is optional — if null, it's derived from
    /// the prop's PixelWidth/PixelHeight like scattered props are.
    /// </summary>
    public readonly struct SpectralPropPlacement
    {
        public string Type { get; }
        public float WorldX { get; }
        public float WorldY { get; }
        public Vector3? WorldScaleOverride { get; }

        public SpectralPropPlacement(string type, float worldX, float worldY, Vector3? worldScaleOverride = null)
        {
            Type = type;
            WorldX = worldX;
            WorldY = worldY;
            WorldScaleOverride = worldScaleOverride;
        }
    }
}
