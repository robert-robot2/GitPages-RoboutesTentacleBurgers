namespace SpectralXGLX.BWP
{
    /// <summary>
    /// Level and stat allocation system for SpectralX WebGL2 BWP.
    /// Ported from BloodLevel — no BloodWyrmService dependency.
    /// Works directly against ISpectralCharacter.
    /// </summary>
    public class SpectralLevel
    {
        private readonly Random _rng = new();

        // ── XP Gain ───────────────────────────────────────────────
        /// <summary>
        /// Add XP from a source. Call this when an enemy or breakable is defeated.
        /// </summary>
        public void AddXp(ISpectralCharacter character, string source, double multiplier = 1.0)
        {
            int baseXpGain = source switch
            {
                // ── Current enemy sources ─────────────────────────
                "Dummy" => 1,   // breakable training dummy

                // ── Placeholder enemy sources — wire when enemy system ready ──
                 "Skeleton"     => 1,
                 "ZombiePyscho" => 2,
                 "PsychoSkeleton"   => 3,
                 "SkeletonWar"      => 4,
                 "Goatman"      => 5,
                 "ScavBoss"         => 10,
                 "SkeletonBoss"     => 100,


                _ => 0
            };

            int finalXp = (int)(baseXpGain * multiplier);
            if (finalXp <= 0) return;

            character.CharXP += finalXp;
            CheckLevelUp(character);
        }

        // ── Level Up Check ────────────────────────────────────────
        private void CheckLevelUp(ISpectralCharacter character)
        {
            int threshold = GetXPThreshold(character);

            if (character.CharXP >= threshold &&
                character.CharLevel < character.CharLevelCap)
            {
                character.CharLevel++;
                int roll = _rng.Next(1, character.CharMaxHP);
                character.CharMaxHP += roll;
                character.CharHitPoints += roll;
                character.CharStatPoints += 2;

                Console.WriteLine($"[SpectralLevel] LEVEL UP → {character.CharLevel} " +
                                  $"HP+{roll} StatPoints:{character.CharStatPoints}");
            }
        }

        // ── XP Threshold ──────────────────────────────────────────
        public int GetXPThreshold(ISpectralCharacter character)
        {
            return (int)(character.CharXPPerLevel * Math.Pow(character.CharLevel, 2));
        }

        // ── Stat Allocation ───────────────────────────────────────
        public void AllocateStat(ISpectralCharacter character, string stat)
        {
            if (character.CharStatPoints <= 0) return;

            switch (stat)
            {
                case "Strength": character.CharStrength++; break;
                case "Alacrity": character.CharAlacrity++; break;
                case "Celerity": character.CharCelerity++; break;
                case "Limenity": character.CharLimenity++; break;
                case "Intelligence": character.CharIntelligence++; break;
                case "uniqueS1": character.CharResourceValue++; break;
                case "uniqueS2": character.CharRegenValue++; break;
                case "Life Regen": character.CharLifeRegen++; break;

            }

            character.CharStatPoints--;
        }
    }
}