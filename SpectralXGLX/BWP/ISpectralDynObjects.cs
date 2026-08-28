using System.Numerics;
using SpectralXGLX.SpectralXComponent;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// Interface for all dynamic interactable world objects in BWP Scene4.
    ///
    /// Ported and redesigned from BloodDynamicObj (2D CSS pixel positions + Rectangle collision)
    /// → WebGL2 3D billboard system (world-space floats + sphere collision).
    ///
    /// Objects render as PrimSquare billboards, matching the warrior and static-object pattern.
    /// Warrior baseline: 84 px = 1 world unit.
    ///
    /// Collision: sphere intersection in XY world-space (Z is ignored — terrain is flat for pickup).
    ///
    /// Lifecycle:
    ///   1. SpectralDynManager.SpawnAll() creates objects and registers their meshes in Scene4.
    ///   2. SpectralDynManager.TickAll() calls DynTickUpdate() on every active object each frame.
    ///   3. On pickup, DynIsActive → false and DynMesh alpha → 0 to hide it.
    /// </summary>
    public interface IDynamicO
    {
        // ── World Position ────────────────────────────────────────
        float DynX { get; set; }
        float DynY { get; set; }

        /// <summary>
        /// World Z of the billboard centre.
        /// Set to (pixelHeight / 84) so the base of the sprite sits on the ground,
        /// matching the pattern used in SpectralXBWPStaticObjects.
        /// </summary>
        float DynZ { get; set; }

        // ── World Size (84 px = 1 world unit) ────────────────────
        float DynWidth { get; set; }
        float DynHeight { get; set; }

        // ── Collision ─────────────────────────────────────────────
        /// <summary>
        /// Sphere pickup radius in world units.
        /// Pickup fires when:  distance(character.WorldXY, dynObj.XY) &lt; character.CollisionRadius + DynCollisionRadius
        /// </summary>
        float DynCollisionRadius { get; }

        // ── Mesh ──────────────────────────────────────────────────
        /// <summary>
        /// PrimSquare billboard mesh that was added to Scene4 by SpawnAll().
        /// Null only before SpawnAll() runs.
        /// </summary>
        SpectralXMesh? DynMesh { get; set; }

        // ── Lifecycle ─────────────────────────────────────────────
        /// <summary>False once the object has been picked up or destroyed.</summary>
        bool DynIsActive { get; }

        // ── Per-frame Tick ────────────────────────────────────────
        /// <summary>
        /// Called every frame from SpectralDynManager.TickAll().
        /// Handles collision checks, pickups, and visual state (e.g. campfire flicker).
        /// </summary>
        void DynTickUpdate(ISpectralCharacter character);

        // ── Visual ────────────────────────────────────────────────
        /// <summary>Asset path for the billboard texture, e.g. "/iAssets/Campfire0003.png".</summary>
        string DynTexturePath { get; }

        /// <summary>
        /// Whether this object should use the emissive rendering path (skips scene lighting).
        /// True for campfire — false for all pickups.
        /// </summary>
        bool DynIsEmissive { get; }

        /// <summary>
        /// Current emissive tint colour. Updated every ~100 ms for campfire flicker.
        /// Directly applied to DynMesh.Color so changes propagate to JS via TickAndGetFrame().
        /// </summary>
        Vector4 DynEmissiveColor { get; }

        /// <summary>Emissive intensity multiplier. Varied by flicker for campfire glow.</summary>
        float DynEmissiveIntensity { get; }
    }
}