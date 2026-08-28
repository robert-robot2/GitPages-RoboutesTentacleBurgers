using System;
using System.Collections.Generic;
using System.Text;

namespace WarCraftLibrary
{
    // ===== ANIMATION STATE ENUM =====
    public enum AnimationState
    {
        Idle,
        Move,
        Attack,
        Death
    }

    // ===== DIRECTION ENUM (8 directions) =====
    public enum Direction
    {
        N,   // North (up)
        NE,  // Northeast
        E,   // East (right)
        SE,  // Southeast
        S,   // South (down)
        SW,  // Southwest
        W,   // West (left)
        NW   // Northwest
    }

    // ===== ANIMATION CONFIGURATION =====
    public class AnimationConfig
    {
        public int FrameCount { get; set; } = 5;           // Number of frames in sprite sheet
        public int FrameDuration { get; set; } = 5;        // Ticks per frame (5 ticks = 12fps at 60fps game)
        public bool Loop { get; set; } = true;             // Does animation loop?
        public int FrameWidth { get; set; } = 32;          // Width of each frame
        public int FrameHeight { get; set; } = 32;         // Height of each frame

        // Default configs for each animation type
        public static AnimationConfig Idle => new AnimationConfig
        {
            FrameCount = 1,
            FrameDuration = 10,
            Loop = true
        };

        public static AnimationConfig Move => new AnimationConfig
        {
            FrameCount = 1,
            FrameDuration = 5,
            Loop = true
        };

        public static AnimationConfig Attack => new AnimationConfig
        {
            FrameCount = 1,
            FrameDuration = 4,
            Loop = true  // Loops until attack finishes
        };

        public static AnimationConfig Death => new AnimationConfig
        {
            FrameCount = 1,
            FrameDuration = 8,
            Loop = false  // Plays once and stops on last frame
        };
    }

    // ===== ANIMATION MANAGER =====
    public static class WarAnimations
    {
       
        // ===== GET SPRITE PATH FOR UNIT ANIMATION =====
        public static string GetAnimatedSpritePath(WarUnit unit)
        {
            // If unit is dead and death animation finished, stay on last frame
            if (unit.CurrentAnimationState == AnimationState.Death && !unit.IsDeathAnimationPlaying)
            {
                return GetSpriteSheetPath(unit, AnimationState.Death, unit.CurrentDirection);
            }

            // Try to get animated sprite sheet path
            string animatedPath = GetSpriteSheetPath(unit, unit.CurrentAnimationState, unit.CurrentDirection);

            // ===== TRY ANIMATED SPRITE, FALLBACK TO STATIC IF NEEDED =====
            // For now, we'll construct the path and try to use it
            // The browser will show broken image if file doesn't exist
            // In production, you'd want to check file existence server-side

            // Only use animated sprites for specific combinations we've created
            bool hasAnimatedSprite = HasAnimatedSprite(unit, unit.CurrentAnimationState, unit.CurrentDirection);

            if (hasAnimatedSprite)
            {
                return animatedPath;
            }
            else
            {
                // Fallback to original static sprite
                return unit.SpritePath;
            }

            // When you have sprite sheets, uncomment this instead:
            // return animatedPath;
        }

        // ===== CONSTRUCT SPRITE SHEET PATH =====
        private static string GetSpriteSheetPath(WarUnit unit, AnimationState state, Direction direction)
        {
            // Get base unit type name
            string unitType = unit.PlaceholderName.Split('_')[0]; // "Peasant_Human" -> "Peasant"

            // Construct path: /wc1sprites/units/Peon_Move_N.png
            string path = $"/wc1sprites/units/{unitType}_{state}_{direction}.png";

            return path;
        }

        // ===== CALCULATE DIRECTION FROM MOVEMENT VECTOR =====
        public static Direction CalculateDirection(int deltaX, int deltaY)
        {
            // Handle no movement (shouldn't happen, but safety)
            if (deltaX == 0 && deltaY == 0)
                return Direction.S; // Default to South

            // Calculate angle in degrees (0° = East, 90° = North)
            double angle = Math.Atan2(-deltaY, deltaX) * (180.0 / Math.PI);

            // Normalize to 0-360
            if (angle < 0) angle += 360;

            // Map angle to 8 directions (45° per direction)
            // E=0°, NE=45°, N=90°, NW=135°, W=180°, SW=225°, S=270°, SE=315°

            if (angle >= 337.5 || angle < 22.5)
                return Direction.E;
            else if (angle >= 22.5 && angle < 67.5)
                return Direction.NE;
            else if (angle >= 67.5 && angle < 112.5)
                return Direction.N;
            else if (angle >= 112.5 && angle < 157.5)
                return Direction.NW;
            else if (angle >= 157.5 && angle < 202.5)
                return Direction.W;
            else if (angle >= 202.5 && angle < 247.5)
                return Direction.SW;
            else if (angle >= 247.5 && angle < 292.5)
                return Direction.S;
            else // 292.5 to 337.5
                return Direction.SE;
        }

        // ===== UPDATE ANIMATION FRAME =====
        public static void UpdateAnimationFrame(WarUnit unit)
        {
            string unitType = unit.PlaceholderName.Split('_')[0];
            AnimationConfig config = AnimationDatabase.Get(unitType, unit.CurrentAnimationState, unit.CurrentDirection);

            unit.AnimationTickCounter++;

            // Time to change frame?
            if (unit.AnimationTickCounter >= config.FrameDuration)
            {
                unit.AnimationTickCounter = 0;
                unit.AnimationFrame++;

                // Handle looping vs non-looping
                if (unit.AnimationFrame >= config.FrameCount)
                {
                    if (config.Loop)
                    {
                        unit.AnimationFrame = 0; // Loop back to start
                    }
                    else
                    {
                        // Death animation - stay on last frame
                        unit.AnimationFrame = config.FrameCount - 1;
                        unit.IsDeathAnimationPlaying = false;
                    }
                }
            }
        }

        // ===== GET CONFIG FOR ANIMATION STATE =====
        private static AnimationConfig GetConfigForState(AnimationState state)
        {
            return state switch
            {
                AnimationState.Idle => AnimationConfig.Idle,
                AnimationState.Move => AnimationConfig.Move,
                AnimationState.Attack => AnimationConfig.Attack,
                AnimationState.Death => AnimationConfig.Death,
                _ => AnimationConfig.Idle
            };
        }

        // ===== RESET ANIMATION (when changing states) =====
        public static void ResetAnimation(WarUnit unit)
        {
            unit.AnimationFrame = 0;
            unit.AnimationTickCounter = 0;
        }

        // ===== SET ANIMATION STATE (with auto-reset) =====
        public static void SetAnimationState(WarUnit unit, AnimationState newState)
        {
            if (unit.CurrentAnimationState != newState)
            {
                unit.CurrentAnimationState = newState;
                ResetAnimation(unit);

                // Special handling for death
                if (newState == AnimationState.Death)
                {
                    unit.IsDeathAnimationPlaying = true;
                }
            }
        }


        // ===== CHECK IF ANIMATED SPRITE EXISTS =====
        private static bool HasAnimatedSprite(WarUnit unit, AnimationState state, Direction direction)
        {
            // Get base unit type name
            string unitType = unit.PlaceholderName.Split('_')[0];

            // ===== WHITELIST: Only these sprite sheets exist =====
            // Uncomment each line as you add the corresponding sprite sheet!

            var existingSprites = new HashSet<string>
    {
        // ========== PEASANT (Human Worker) ==========
        
        // PEASANT - MOVE (8 directions)
        "Peasant_Move_S",
        "Peasant_Move_N",
        "Peasant_Move_E",
        "Peasant_Move_W",
         "Peasant_Move_NE",
         "Peasant_Move_SE",
         "Peasant_Move_SW",
         "Peasant_Move_NW",
        
        // PEASANT - IDLE (8 directions)
         "Peasant_Idle_S",
         "Peasant_Idle_N",
         "Peasant_Idle_E",
         "Peasant_Idle_W",
        "Peasant_Idle_NE",
         "Peasant_Idle_SE",
         "Peasant_Idle_SW",
         "Peasant_Idle_NW",
        
        // PEASANT - ATTACK (8 directions)
         "Peasant_Attack_S",
         "Peasant_Attack_N",
         "Peasant_Attack_E",
         "Peasant_Attack_W",
        // "Peasant_Attack_NE",
        // "Peasant_Attack_SE",
        // "Peasant_Attack_SW",
        // "Peasant_Attack_NW",
        
        // PEASANT - DEATH (8 directions)
         "Peasant_Death_S",
         "Peasant_Death_N",
         "Peasant_Death_E",
         "Peasant_Death_W",
         "Peasant_Death_NE",
         "Peasant_Death_SE",
         "Peasant_Death_SW",
         "Peasant_Death_NW",
        
        
        // ========== PEON (Orc Worker) ==========
        
        // PEON - MOVE (8 directions)
         "Peon_Move_S",
         "Peon_Move_N",
         "Peon_Move_E",
         "Peon_Move_W",
         "Peon_Move_NE",
         "Peon_Move_SE",
         "Peon_Move_SW",
         "Peon_Move_NW",
        
        // PEON - IDLE (8 directions)
         "Peon_Idle_S",
         "Peon_Idle_N",
         "Peon_Idle_E",
         "Peon_Idle_W",
         "Peon_Idle_NE",
         "Peon_Idle_SE",
         "Peon_Idle_SW",
         "Peon_Idle_NW",
        
        // PEON - ATTACK (8 directions)
         "Peon_Attack_S",
         "Peon_Attack_N",
         "Peon_Attack_E",
         "Peon_Attack_W",
        // "Peon_Attack_NE",
        // "Peon_Attack_SE",
        // "Peon_Attack_SW",
        // "Peon_Attack_NW",
        
        // PEON - DEATH (8 directions)
         "Peon_Death_S",
         "Peon_Death_N",
         "Peon_Death_E",
         "Peon_Death_W",
         "Peon_Death_NE",
         "Peon_Death_SE",
         "Peon_Death_SW",
         "Peon_Death_NW",
        
        
        // ========== FOOTMAN (Human Soldier) ==========
        
        // FOOTMAN - MOVE (8 directions)
         "Footman_Move_S",
         "Footman_Move_N",
         "Footman_Move_E",
         "Footman_Move_W",
        // "Footman_Move_NE",
        // "Footman_Move_SE",
        // "Footman_Move_SW",
        // "Footman_Move_NW",
        
        // FOOTMAN - IDLE (8 directions)
         "Footman_Idle_S",
        "Footman_Idle_N",
         "Footman_Idle_E",
         "Footman_Idle_W",
         "Footman_Idle_NE",
         "Footman_Idle_SE",
         "Footman_Idle_SW",
         "Footman_Idle_NW",
        
        // FOOTMAN - ATTACK (8 directions)
         "Footman_Attack_S",
         "Footman_Attack_N",
         "Footman_Attack_E",
         "Footman_Attack_W",
       //  "Footman_Attack_NE",
        // "Footman_Attack_SE",
        // "Footman_Attack_SW",
        // "Footman_Attack_NW",
        
        // FOOTMAN - DEATH (8 directions)
         "Footman_Death_S",
         "Footman_Death_N",
         "Footman_Death_E",
         "Footman_Death_W",
         "Footman_Death_NE",
         "Footman_Death_SE",
         "Footman_Death_SW",
         "Footman_Death_NW",
        
        
        // ========== GRUNT (Orc Soldier) ==========
        
        // GRUNT - MOVE (8 directions)
         "Grunt_Move_S",
         "Grunt_Move_N",
         "Grunt_Move_E",
         "Grunt_Move_W",
        // "Grunt_Move_NE",
        // "Grunt_Move_SE",
        // "Grunt_Move_SW",
        // "Grunt_Move_NW",
        
        // GRUNT - IDLE (8 directions)
         "Grunt_Idle_S",
        "Grunt_Idle_N",
         "Grunt_Idle_E",
         "Grunt_Idle_W",
         "Grunt_Idle_NE",
         "Grunt_Idle_SE",
         "Grunt_Idle_SW",
         "Grunt_Idle_NW",
        
        // GRUNT - ATTACK (8 directions)
         "Grunt_Attack_S",
         "Grunt_Attack_N",
         "Grunt_Attack_E",
         "Grunt_Attack_W",
        // "Grunt_Attack_NE",
        // "Grunt_Attack_SE",
        // "Grunt_Attack_SW",
        // "Grunt_Attack_NW",
        
        // GRUNT - DEATH (8 directions)
         "Grunt_Death_S",
         "Grunt_Death_N",
         "Grunt_Death_E",
         "Grunt_Death_W",
         "Grunt_Death_NE",
         "Grunt_Death_SE",
         "Grunt_Death_SW",
         "Grunt_Death_NW",

         // ========== ARCHER ==========

// ARCHER - MOVE (8 directions)
 "Archer_Move_S",
 "Archer_Move_N",
 "Archer_Move_E",
 "Archer_Move_W",
// "Archer_Move_NE",
// "Archer_Move_SE",
// "Archer_Move_SW",
// "Archer_Move_NW",

// ARCHER - IDLE (8 directions)
 "Archer_Idle_S",
 "Archer_Idle_N",
 "Archer_Idle_E",
 "Archer_Idle_W",
 "Archer_Idle_NE",
 "Archer_Idle_SE",
 "Archer_Idle_SW",
 "Archer_Idle_NW",

// ARCHER - ATTACK (8 directions)
 "Archer_Attack_S",
 "Archer_Attack_N",
 "Archer_Attack_E",
 "Archer_Attack_W",
// "Archer_Attack_NE",
// "Archer_Attack_SE",
// "Archer_Attack_SW",
// "Archer_Attack_NW",

// ARCHER - DEATH (8 directions)
 "Archer_Death_S",
 "Archer_Death_N",
 "Archer_Death_E",
 "Archer_Death_W",
 "Archer_Death_NE",
 "Archer_Death_SE",
 "Archer_Death_SW",
 "Archer_Death_NW",


// ========== TROLL AXE THROWER ==========

// TROLL AXE THROWER - MOVE (8 directions)
 "TrollAxeThrower_Move_S",
 "TrollAxeThrower_Move_N",
 "TrollAxeThrower_Move_E",
 "TrollAxeThrower_Move_W",
// "TrollAxeThrower_Move_NE",
// "TrollAxeThrower_Move_SE",
// "TrollAxeThrower_Move_SW",
// "TrollAxeThrower_Move_NW",

// TROLL AXE THROWER - IDLE (8 directions)
"TrollAxeThrower_Idle_S",
"TrollAxeThrower_Idle_N",
 "TrollAxeThrower_Idle_E",
 "TrollAxeThrower_Idle_W",
 "TrollAxeThrower_Idle_NE",
 "TrollAxeThrower_Idle_SE",
 "TrollAxeThrower_Idle_SW",
 "TrollAxeThrower_Idle_NW",

// TROLL AXE THROWER - ATTACK (8 directions)
 "TrollAxeThrower_Attack_S",
 "TrollAxeThrower_Attack_N",
 "TrollAxeThrower_Attack_E",
 "TrollAxeThrower_Attack_W",
// "TrollAxeThrower_Attack_NE",
// "TrollAxeThrower_Attack_SE",
// "TrollAxeThrower_Attack_SW",
// "TrollAxeThrower_Attack_NW",

// TROLL AXE THROWER - DEATH (8 directions)
 "TrollAxeThrower_Death_S",
 "TrollAxeThrower_Death_N",
 "TrollAxeThrower_Death_E",
 "TrollAxeThrower_Death_W",
 "TrollAxeThrower_Death_NE",
 "TrollAxeThrower_Death_SE",
 "TrollAxeThrower_Death_SW",
 "TrollAxeThrower_Death_NW",
// ========== BRIGAND ==========

// BRIGAND - MOVE (8 directions)
 "Brigand_Move_S",
 "Brigand_Move_N",
 "Brigand_Move_E",
 "Brigand_Move_W",
// "Brigand_Move_NE",
// "Brigand_Move_SE",
// "Brigand_Move_SW",
// "Brigand_Move_NW",

// BRIGAND - IDLE (8 directions)
 "Brigand_Idle_S",
 "Brigand_Idle_N",
 "Brigand_Idle_E",
 "Brigand_Idle_W",
 "Brigand_Idle_NE",
 "Brigand_Idle_SE",
 "Brigand_Idle_SW",
 "Brigand_Idle_NW",

// BRIGAND - ATTACK (8 directions)
 "Brigand_Attack_S",
 "Brigand_Attack_N",
 "Brigand_Attack_E",
 "Brigand_Attack_W",
// "Brigand_Attack_NE",
// "Brigand_Attack_SE",
// "Brigand_Attack_SW",
// "Brigand_Attack_NW",

// BRIGAND - DEATH (8 directions)
 "Brigand_Death_S",
 "Brigand_Death_N",
 "Brigand_Death_E",
 "Brigand_Death_W",
 "Brigand_Death_NE",
 "Brigand_Death_SE",
 "Brigand_Death_SW",
 "Brigand_Death_NW",


// ========== OGRE ==========

// OGRE - MOVE (8 directions)
 "Ogre_Move_S",
 "Ogre_Move_N",
 "Ogre_Move_E",
 "Ogre_Move_W",
// "Ogre_Move_NE",
// "Ogre_Move_SE",
// "Ogre_Move_SW",
// "Ogre_Move_NW",

// OGRE - IDLE (8 directions)
 "Ogre_Idle_S",
 "Ogre_Idle_N",
 "Ogre_Idle_E",
 "Ogre_Idle_W",
 "Ogre_Idle_NE",
 "Ogre_Idle_SE",
 "Ogre_Idle_SW",
 "Ogre_Idle_NW",

// OGRE - ATTACK (8 directions)
 "Ogre_Attack_S",
 "Ogre_Attack_N",
 "Ogre_Attack_E",
 "Ogre_Attack_W",
// "Ogre_Attack_NE",
// "Ogre_Attack_SE",
// "Ogre_Attack_SW",
// "Ogre_Attack_NW",

// OGRE - DEATH (8 directions)
 "Ogre_Death_S",
 "Ogre_Death_N",
 "Ogre_Death_E",
 "Ogre_Death_W",
 "Ogre_Death_NE",
 "Ogre_Death_SE",
 "Ogre_Death_SW",
 "Ogre_Death_NW",

 // ========== CLERIC ==========

// CLERIC - MOVE (8 directions)
 "Cleric_Move_S",
 "Cleric_Move_N",
 "Cleric_Move_E",
 "Cleric_Move_W",
// "Cleric_Move_NE",
// "Cleric_Move_SE",
// "Cleric_Move_SW",
// "Cleric_Move_NW",

// CLERIC - IDLE (8 directions)
// "Cleric_Idle_S",
// "Cleric_Idle_N",
// "Cleric_Idle_E",
// "Cleric_Idle_W",
// "Cleric_Idle_NE",
// "Cleric_Idle_SE",
// "Cleric_Idle_SW",
// "Cleric_Idle_NW",

// CLERIC - ATTACK (8 directions)
// "Cleric_Attack_S",
// "Cleric_Attack_N",
// "Cleric_Attack_E",
// "Cleric_Attack_W",
// "Cleric_Attack_NE",
// "Cleric_Attack_SE",
// "Cleric_Attack_SW",
// "Cleric_Attack_NW",

// CLERIC - DEATH (8 directions)
// "Cleric_Death_S",
// "Cleric_Death_N",
// "Cleric_Death_E",
// "Cleric_Death_W",
// "Cleric_Death_NE",
// "Cleric_Death_SE",
// "Cleric_Death_SW",
// "Cleric_Death_NW",

 // ========== CULTIST ==========

// CULTIST - MOVE (8 directions)
 "Cultist_Move_S",
 "Cultist_Move_N",
 "Cultist_Move_E",
 "Cultist_Move_W",
// "Cultist_Move_NE",
// "Cultist_Move_SE",
// "Cultist_Move_SW",
// "Cultist_Move_NW",

// CULTIST - IDLE (8 directions)
// "Cultist_Idle_S",
// "Cultist_Idle_N",
// "Cultist_Idle_E",
// "Cultist_Idle_W",
// "Cultist_Idle_NE",
// "Cultist_Idle_SE",
// "Cultist_Idle_SW",
// "Cultist_Idle_NW",

// CULTIST - ATTACK (8 directions)
// "Cultist_Attack_S",
// "Cultist_Attack_N",
// "Cultist_Attack_E",
// "Cultist_Attack_W",
// "Cultist_Attack_NE",
// "Cultist_Attack_SE",
// "Cultist_Attack_SW",
// "Cultist_Attack_NW",

// CULTIST - DEATH (8 directions)
// "Cultist_Death_S",
// "Cultist_Death_N",
// "Cultist_Death_E",
// "Cultist_Death_W",
// "Cultist_Death_NE",
// "Cultist_Death_SE",
// "Cultist_Death_SW",
// "Cultist_Death_NW",




    };

            string key = $"{unitType}_{state}_{direction}";
            return existingSprites.Contains(key);
        }




    }
}