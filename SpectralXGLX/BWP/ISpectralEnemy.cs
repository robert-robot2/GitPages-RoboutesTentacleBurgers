using System.Numerics;
using SpectralXGLX.SpectralXComponent;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// Base interface for all SpectralX WebGL2 enemies.
    /// Mirrors ISpectralCharacter's shape — world space floats, mesh reference,
    /// delta-time Tick, and the same collision/damage pattern characters use.
    ///
    /// Special attacks are intentionally NOT part of this interface yet —
    /// no enemy animations exist for them. EnemyAttack covers the basic
    /// melee/proximity attack only. Future enemies can extend this once
    /// special-attack animations are ready.
    /// </summary>
    public interface ISpectralEnemy
    {
        // ── Identity ─────────────────────────────────────────────
        string EnemyClassName { get; }
        bool EnemyIsAlive { get; }

        // ── World Position ────────────────────────────────────────
        float WorldX { get; set; }
        float WorldY { get; set; }
        float WorldZ { get; set; }

        // ── Core Stats ────────────────────────────────────────────
        int EnemyHitPoints { get; set; }
        int EnemyMaxHP { get; set; }
        int EnemyLevel { get; set; }
        int EnemyXP { get; set; }
        int EnemyXPPerLevel { get; set; }
        int EnemyLevelCap { get; set; }

        // ── Combat Stats ──────────────────────────────────────────
        int EnemyStrength { get; set; }
        int EnemyAlacrity { get; set; }
        int EnemyCelerity { get; set; }
        int EnemyLimenity { get; set; }
        int EnemyIntelligence { get; set; }
        int EnemyLifeRegen { get; set; }
        int EnemyStatPoints { get; set; }

        // ── Hunger (kept for stat parity — most enemies won't use it) ──────
        int EnemyHungerCurrent { get; set; }
        int EnemyHungerFull { get; set; }
        int EnemyHungerDurationSeconds { get; set; }

        // ── Unique class resource (e.g. "Bone Rage") ───────────────────────
        string EnemyResourceName { get; }
        int EnemyResourceValue { get; set; }
        string EnemyRegenLabel { get; }
        int EnemyRegenValue { get; set; }
        string EnemyMaxResourceName { get; }
        int EnemyMaxResourceValue { get; set; }

        // ── Color Theme ───────────────────────────────────────────
        string EnemyHPColor { get; }
        string EnemyInvColor { get; }
        string EnemyEnergyColor { get; }

        // ── Mesh Reference ────────────────────────────────────────
        SpectralXMesh? EnemyMesh { get; }

        // ── Movement / AI ─────────────────────────────────────────
        /// <summary>
        /// Drives patrol AI and aggro/attack-range checks against the target.
        /// Called every tick from the engine's enemy update loop.
        /// </summary>
        void EnemyMove(ISpectralCharacter target);

        /// <summary>
        /// Proximity/collision based melee attack against the target character.
        /// No special attack variant yet — placeholder for future enemies.
        /// </summary>
        void EnemyAttack(ISpectralCharacter target);

        // ── Animation ────────────────────────────────────────────
        void Tick(float delta);

        // ── Damage ───────────────────────────────────────────────
        void TakeDamage(int amount);

        // ── Collision ────────────────────────────────────────────
        float CollisionRadius { get; }

        // ── Hit Flash (debug/testing visual feedback) ──────────────────────
        bool ShowHitFlash { get; }

        string EnemyHitOverlayTexturePath { get; }

        // ── Death / Render State ─────────────────────────────────────────
        // True once EnemyIsAlive flips false — engine/JS use this to swap
        // to the cooked/dead sprite and apply the faded/grayscale render.
        bool IsDead { get; }

        // TODO: EnemySpecialAttack — when special attack animations exist for any enemy
        // TODO: SetAggression — explicit aggro target system, currently handled via EnemyMove target param
        // TODO: SplatterPuddles when FX system is ported
    }
}