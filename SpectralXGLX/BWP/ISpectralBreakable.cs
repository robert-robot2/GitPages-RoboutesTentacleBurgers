using System.Numerics;
using SpectralXGLX.SpectralXComponent;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// Interface for all breakable/destructible world objects in BWP Scene4.
    ///
    /// Mirrors IDynamicO structure but for objects that take damage rather than
    /// being picked up. Collision is sphere-based in XY world space.
    ///
    /// Warrior baseline: 84 px = 1 world unit.
    ///
    /// Phase lifecycle:
    ///   Alive     → normal texture, normal mesh color
    ///   HitEffect → hit texture + red tint, clears after ~0.15s back to Alive
    ///   Dead      → cooked texture, stays in world for ~60s (threshold tunable later)
    ///
    /// Tick lifecycle:
    ///   1. SpectralBreakManager.SpawnAll() creates objects and registers meshes in Scene4.
    ///   2. SpectralBreakManager.TickAll() calls DynTickUpdate() each frame.
    ///   3. On death, phase transitions to Dead — mesh texture swaps, object stays in world.
    /// </summary>
    public interface ISpectralBreakable
    {
        // ── World Position ────────────────────────────────────────
        float BreakX { get; set; }
        float BreakY { get; set; }

        /// <summary>
        /// World Z of the billboard centre.
        /// Set to (pixelHeight / 84) so the base of the sprite sits on the ground,
        /// matching IDynamicO and SpectralXBWPStaticObjects patterns.
        /// </summary>
        float BreakZ { get; set; }

        // ── World Size (84 px = 1 world unit) ────────────────────
        float BreakWidth { get; set; }
        float BreakHeight { get; set; }

        // ── Collision ─────────────────────────────────────────────
        /// <summary>
        /// Sphere hit radius in world units.
        /// Hit fires when: distance(warrior.PunchCenter, breakable.XY) lt warrior.PunchRadius + BreakCollisionRadius
        /// </summary>
        float BreakCollisionRadius { get; }

        // ── Mesh ──────────────────────────────────────────────────
        /// <summary>
        /// PrimSquare billboard mesh registered in Scene4 by SpawnAll().
        /// Null only before SpawnAll() runs.
        /// </summary>
        SpectralXMesh? BreakMesh { get; set; }

        // ── Lifecycle ─────────────────────────────────────────────
        int BreakHitPoints { get; set; }
        int BreakMaxHP { get; set; }

        /// <summary>True while BreakHitPoints > 0.</summary>
        bool BreakIsAlive { get; }

        /// <summary>Current visual phase — drives texture and tint each tick.</summary>
        BreakPhase Phase { get; }

        /// <summary>True during the brief hit flash window.</summary>
        bool BreakIsShowingHitEffect { get; }

        // ── Damage ────────────────────────────────────────────────
        void BreakTakeDamage(int amount);

        /// <summary>Manually clear hit flash — normally handled by DynTickUpdate timer.</summary>
        void BreakClearHitEffects();

        // ── Per-frame Tick ────────────────────────────────────────
        /// <summary>
        /// Called every frame from SpectralBreakManager.TickAll().
        /// Handles hit flash timer, dead timer, and mesh visual state sync.
        /// Delta based — no DateTime, no threading.
        /// </summary>
        void DynTickUpdate(ISpectralCharacter character, float delta);

        // ── Textures ──────────────────────────────────────────────
        string BreakTexturePath { get; }   // alive sprite
        string BreakHitTexturePath { get; }   // hit flash sprite
        string BreakDeadTexturePath { get; }   // death / cooked sprite

        // ── Emissive ──────────────────────────────────────────────
        bool BreakIsEmissive { get; }
        Vector4 BreakEmissiveColor { get; }
        float BreakEmissiveIntensity { get; }

       // void AddXp();



    }

    /// <summary>Visual phase state for breakable objects.</summary>
    public enum BreakPhase
    {
        Alive,
        HitEffect,
        Dead
    }
}