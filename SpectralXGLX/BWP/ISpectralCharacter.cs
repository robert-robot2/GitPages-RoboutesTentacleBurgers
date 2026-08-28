using System.Numerics;
using SpectralXGLX.SpectralXComponent;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// Base interface for all SpectralX WebGL2 characters.
    /// Stripped of old div engine dependencies.
    /// All positions are world space floats not CSS pixels.
    /// </summary>
    public interface ISpectralCharacter
    {
        // ── Identity ─────────────────────────────────────────────
        string CharClassName { get; }
        bool CharIsAlive { get; }

        // ── World Position ────────────────────────────────────────
        // Float world space — not CSS pixels
        float WorldX { get; set; }
        float WorldY { get; set; }
        float WorldZ { get; set; }

        // ── Core Stats ────────────────────────────────────────────
        int CharHitPoints { get; set; }
        int CharMaxHP { get; set; }
        int CharLevel { get; set; }
        int CharXP { get; set; }
        int CharXPPerLevel { get; set; }
        int CharLevelCap { get; set; }

        // ── Combat Stats ──────────────────────────────────────────
        int CharStrength { get; set; }
        int CharAlacrity { get; set; }
        int CharCelerity { get; set; }
        int CharLimenity { get; set; }
        int CharIntelligence { get; set; }
        int CharLifeRegen { get; set; }
        int CharStatPoints { get; set; }
        string CharHPColor { get; }
        string CharInvColor { get; }
        string CharEnergyColor { get; }
        int CharHungerCurrent { get; set; }
        int CharHungerFull { get; set; }
        int CharHungerDurationSeconds { get; set; }
        // these are specific stats unique to certain character classes
        string CharResourceName { get; }     // "Rage", "Mana", etc.
        int CharResourceValue { get; set; }    // "35", "120", etc.
        string CharRegenLabel { get; }       // "Rage on Hit", "Mana Regen"
        int CharRegenValue { get; set; }

        string CharMaxResourceName { get; }
        int CharMaxResourceValue { get; set; }

     
        string CharHitTexturePath { get; }   // hit flash sprite
        string CharDeadTexturePath { get; }   // death / cooked sprite
        string CharHitOverlayTexturePath { get; }

        // ── Movement ──────────────────────────────────────────────
        void Move(Vector2 isoDir);
        void Stop();

        // ── Animation ────────────────────────────────────────────
        void Tick(float delta);

        // ── Mesh Reference ────────────────────────────────────────
        // The PrimSquare billboard mesh this character renders on
        SpectralXMesh? CharMesh { get; }

        // ── Damage ───────────────────────────────────────────────
        void TakeDamage(int amount);

        // ── Collision ────────────────────────────────────────────
        // World space collision radius — replaces Rectangle for 3D
        float CollisionRadius { get; }

        // ── Combat ────────────────────────────────────────────────  ← ADD THIS BLOCK
        
        void CharAttack(SpectralLevel spectralLevel, IEnumerable<ISpectralEnemy>? enemies = null, bool? forceRight = null);
        void CharSpecialAttack(SpectralLevel spectralLevel, IEnumerable<ISpectralEnemy>? enemies = null, bool? forceRight = null);

        // TODO: CharAttack when combat system is ready
        // TODO: CharSpecialAttack when rage system is ready
        // TODO: CharCollisionBox as 3D bounds when needed
        // TODO: SplatterPuddles when FX system is ported
        // TODO: CharHunger when survival system is ported
    }
}