using System.Numerics;
using SpectralXGLX.SpectralXComponent;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// Test wave system for SpectralXSkeleton. Folds in the skeleton registry
    /// directly since this is currently a single-enemy-type test harness.
    /// Once more enemy types are ported, this can be split into a dedicated
    /// per-enemy manager (mirroring SpectralDynManager / SpectralBreakManager)
    /// and a thinner wave orchestrator on top.
    ///
    /// Wave counts (test progression): 1, 2, 3, 10, 100.
    /// Spawn positions are randomized within SpectralXSkeleton.PatrolRadius (12f)
    /// of a fixed wave-spawn origin so all skeletons stay near the player
    /// during testing instead of scattering across the full map.
    ///
    /// Placeholder hooks are left throughout for future enemy types —
    /// look for "// TODO: next enemy type" comments.
    ///
    /// SkeletonBoss, ScavBoss, and SkeletonWar are zeroed-out in the wave
    /// counts on purpose — they're solo-spawn only (triggered directly by
    /// the engine, not through wave progression).
    /// </summary>
    public class SpectralWaveSys
    {
        // ── Enemy Registries ─────────────────────────────────────────────
        public List<SpectralXSkeleton> Skeletons { get; } = new();
        public List<SpectralXPsychoSkeleton> PsychoSkeletons { get; } = new();
        public List<SpectralXZombiePsycho> ZombiePsycho { get; } = new();
        public List<SpectralXSkeletonWar> SkeletonWar { get; } = new();
        public List<SpectralXGoatman> Goatman { get; } = new();
        public List<SpectralXScavBoss> ScavBoss { get; } = new();
        public List<SpectralXSkeletonBoss> SkeletonBoss { get; } = new();


        public IEnumerable<ISpectralEnemy> GetAllEnemies()
        {
            foreach (var s in Skeletons)
                yield return s;

            foreach (var p in PsychoSkeletons)
                yield return p;

            foreach (var z in ZombiePsycho)
                yield return z;

            foreach (var sw in SkeletonWar)
                yield return sw;

            foreach (var g in Goatman)
                yield return g;

            foreach (var sb in ScavBoss)
                yield return sb;

            foreach (var skb in SkeletonBoss)
                yield return skb;

        }

        // ── Wave Configuration ───────────────────────────────────────────
        // Test progression — starter enemies taper off as tougher ones phase in,
        // SkeletonBoss is a true boss encounter reserved for the final wave.
        private static readonly int[] WaveSkeletonCounts = { 2, 3, 4, 4, 3, 2, 1, 0, 0, 0 };
        private static readonly int[] WavePsychoSkeletonCounts = { 1, 2, 3, 3, 3, 2, 1, 0, 0, 0 };
        private static readonly int[] WaveZombiePsychoCounts = { 0, 0, 1, 2, 3, 3, 4, 4, 5, 5 };
        private static readonly int[] WaveGoatmanCounts = { 0, 0, 1, 2, 2, 3, 3, 4, 5, 5 };
        private static readonly int[] WaveSkeletonWarCounts = { 0, 0, 0, 0, 1, 2, 2, 3, 3, 4 };
        private static readonly int[] WaveScavBossCounts = { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3 };
        private static readonly int[] WaveSkeletonBossCounts = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };

        
        private const int LastWaveId = 10; // 1-indexed, matches WaveSkeletonCounts.Length

        private int _currentWave = 0; // 0 = no wave loaded yet
        public int CurrentWave => _currentWave;

        // ── Wave Spawn Origin ─────────────────────────────────────────────
        // All skeleton spawns are randomized within PatrolRadius of this point.
        // Set via SetSpawnOrigin() before the first LoadWave() call — typically
        // the engine sets this to (0,0,1) or near the character's start position.
        private float _spawnOriginX = 0f;
        private float _spawnOriginY = 0f;
        private float _spawnOriginZ = 1f;

        private static readonly Random _rng = new();

        // ── Respawn-after-last-wave timer ─────────────────────────────────
        private DateTime? _nextRespawnAt = null;

        // ── Spawn Callback ────────────────────────────────────────────────
        // The engine owns mesh creation (MeshLibrary.GetMesh, Scene4.AddMesh)
        // so this system calls back into the engine to actually spawn a
        // skeleton mesh + InitMesh rather than touching the scene directly.
        // Wired by the engine via SetSpawnCallback() during InitScene4.
        private Func<float, float, float, SpectralXSkeleton>? _spawnSkeletonCallback;
        private Action<SpectralXSkeleton>? _despawnSkeletonCallback;
        private Func<float, float, float, SpectralXPsychoSkeleton>? _spawnPsychoSkeletonCallback;
        private Action<SpectralXPsychoSkeleton>? _despawnPsychoSkeletonCallback;

        private Func<float, float, float, SpectralXZombiePsycho>? _spawnZombiePsychoCallback;
        private Action<SpectralXZombiePsycho>? _despawnZombiePsychoCallback;
        private Func<float, float, float, SpectralXSkeletonWar>? _spawnSkeletonWarCallback;
        private Action<SpectralXSkeletonWar>? _despawnSkeletonWarCallback;
        private Func<float, float, float, SpectralXGoatman>? _spawnGoatmanCallback;
        private Action<SpectralXGoatman>? _despawnGoatmanCallback;
        private Func<float, float, float, SpectralXScavBoss>? _spawnScavBossCallback;
        private Action<SpectralXScavBoss>? _despawnScavBossCallback;
        private Func<float, float, float, SpectralXSkeletonBoss>? _spawnSkeletonBossCallback;
        private Action<SpectralXSkeletonBoss>? _despawnSkeletonBossCallback;
      

        public void SetSpawnOrigin(float x, float y, float z)
        {
            _spawnOriginX = x;
            _spawnOriginY = y;
            _spawnOriginZ = z;
        }

        public void SetSpawnCallback(
           Func<float, float, float, SpectralXSkeleton> spawnSkeleton,
           Action<SpectralXSkeleton> despawnSkeleton,
           Func<float, float, float, SpectralXPsychoSkeleton> spawnPsychoSkeleton,
           Action<SpectralXPsychoSkeleton> despawnPsychoSkeleton)
        {
            _spawnSkeletonCallback = spawnSkeleton;
            _despawnSkeletonCallback = despawnSkeleton;

            _spawnPsychoSkeletonCallback = spawnPsychoSkeleton;
            _despawnPsychoSkeletonCallback = despawnPsychoSkeleton;
        }

        // TODO: next enemy type — added SetSpawnCallback overload below for
        // the new batch. Keeping the original 4-arg overload above intact
        // in case the engine still calls it anywhere.
        public void SetSpawnCallback(
      Func<float, float, float, SpectralXZombiePsycho> spawnZombiePsycho,
      Action<SpectralXZombiePsycho> despawnZombiePsycho,
      Func<float, float, float, SpectralXSkeletonWar> spawnSkeletonWar,
      Action<SpectralXSkeletonWar> despawnSkeletonWar,
      Func<float, float, float, SpectralXGoatman> spawnGoatman,
      Action<SpectralXGoatman> despawnGoatman,
      Func<float, float, float, SpectralXScavBoss> spawnScavBoss,
      Action<SpectralXScavBoss> despawnScavBoss,
      Func<float, float, float, SpectralXSkeletonBoss> spawnSkeletonBoss,
      Action<SpectralXSkeletonBoss> despawnSkeletonBoss)
        {
            _spawnZombiePsychoCallback = spawnZombiePsycho;
            _despawnZombiePsychoCallback = despawnZombiePsycho;

            _spawnSkeletonWarCallback = spawnSkeletonWar;
            _despawnSkeletonWarCallback = despawnSkeletonWar;

            _spawnGoatmanCallback = spawnGoatman;
            _despawnGoatmanCallback = despawnGoatman;

            _spawnScavBossCallback = spawnScavBoss;
            _despawnScavBossCallback = despawnScavBoss;

            _spawnSkeletonBossCallback = spawnSkeletonBoss;
            _despawnSkeletonBossCallback = despawnSkeletonBoss;
        }


        // ── Load Wave ─────────────────────────────────────────────────────
        public void LoadWave(int waveId)
        {
            if (waveId < 1) return;

            // Use per-scene wave defs if registered, fall back to static arrays
            if (_waveDefs.Length > 0)
            {
                if (waveId > _waveDefs.Length)
                {
                    Console.WriteLine($"[SpectralWaveSys] waveId {waveId} out of range for scene defs");
                    return;
                }

                var def = _waveDefs[waveId - 1];
                def.OnWaveStart?.Invoke();

                SpawnFromDef(def.Skeletons, waveId, (x, y) => { var e = _spawnSkeletonCallback!(x, y, _spawnOriginZ); Skeletons.Add(e); }, SpectralXSkeleton.PatrolRadius);
                SpawnFromDef(def.PsychoSkeletons, waveId, (x, y) => { var e = _spawnPsychoSkeletonCallback!(x, y, _spawnOriginZ); PsychoSkeletons.Add(e); }, SpectralXPsychoSkeleton.PatrolRadius);
                SpawnFromDef(def.ZombiePsycho, waveId, (x, y) => { var e = _spawnZombiePsychoCallback!(x, y, _spawnOriginZ); ZombiePsycho.Add(e); }, SpectralXZombiePsycho.PatrolRadius);
                SpawnFromDef(def.SkeletonWar, waveId, (x, y) => { var e = _spawnSkeletonWarCallback!(x, y, _spawnOriginZ); SkeletonWar.Add(e); }, SpectralXSkeletonWar.PatrolRadius);
                SpawnFromDef(def.Goatman, waveId, (x, y) => { var e = _spawnGoatmanCallback!(x, y, _spawnOriginZ); Goatman.Add(e); }, SpectralXGoatman.PatrolRadius);
                SpawnFromDef(def.ScavBoss, waveId, (x, y) => { var e = _spawnScavBossCallback!(x, y, _spawnOriginZ); ScavBoss.Add(e); }, SpectralXScavBoss.PatrolRadius);
                SpawnFromDef(def.SkeletonBoss, waveId, (x, y) => { var e = _spawnSkeletonBossCallback!(x, y, _spawnOriginZ); SkeletonBoss.Add(e); }, SpectralXSkeletonBoss.PatrolRadius);

                _currentWave = waveId;
                Console.WriteLine($"[SpectralWaveSys] Wave {waveId} loaded from scene defs");
                return;
            }

            if (waveId < 1 || waveId > WaveSkeletonCounts.Length)
            {
                Console.WriteLine($"[SpectralWaveSys] Invalid waveId {waveId} — ignoring");
                return;
            }

            if (_spawnSkeletonCallback == null || _spawnPsychoSkeletonCallback == null)
            {
                Console.WriteLine("[SpectralWaveSys] No spawn callback set — call SetSpawnCallback first");
                return;
            }

            int skeletonCount = WaveSkeletonCounts[waveId - 1];
            int psychoCount = WavePsychoSkeletonCounts[waveId - 1];
            int zombiePsychoCount = WaveZombiePsychoCounts[waveId - 1];
            int skeletonWarCount = WaveSkeletonWarCounts[waveId - 1];
            int goatmanCount = WaveGoatmanCounts[waveId - 1];
            int scavBossCount = WaveScavBossCounts[waveId - 1];
            int skeletonBossCount = WaveSkeletonBossCounts[waveId - 1];


            // ───────────────────────────────────────────────
            // Spawn Skeletons
            // ───────────────────────────────────────────────
            for (int i = 0; i < skeletonCount; i++)
            {
                var (sx, sy) = RandomPointInRadius(_spawnOriginX, _spawnOriginY, SpectralXSkeleton.PatrolRadius);
                var skeleton = _spawnSkeletonCallback(sx, sy, _spawnOriginZ);
                Skeletons.Add(skeleton);
            }

            // ───────────────────────────────────────────────
            // Spawn PsychoSkeletons (aggressive)
            // ───────────────────────────────────────────────
            for (int i = 0; i < psychoCount; i++)
            {
                var (px, py) = RandomPointInRadius(_spawnOriginX, _spawnOriginY, SpectralXPsychoSkeleton.PatrolRadius);
                var psycho = _spawnPsychoSkeletonCallback(px, py, _spawnOriginZ);
                PsychoSkeletons.Add(psycho);
            }

            // ───────────────────────────────────────────────
            // Spawn ZombiePsycho
            // ───────────────────────────────────────────────
            if (_spawnZombiePsychoCallback != null)
            {
                for (int i = 0; i < zombiePsychoCount; i++)
                {
                    var (zx, zy) = RandomPointInRadius(_spawnOriginX, _spawnOriginY, SpectralXZombiePsycho.PatrolRadius);
                    var zombiePsycho = _spawnZombiePsychoCallback(zx, zy, _spawnOriginZ);
                    ZombiePsycho.Add(zombiePsycho);
                }
            }

            // ───────────────────────────────────────────────
            // Spawn SkeletonWar (solo-spawn only — count is always 0 here)
            // ───────────────────────────────────────────────
            if (_spawnSkeletonWarCallback != null)
            {
                for (int i = 0; i < skeletonWarCount; i++)
                {
                    var (swx, swy) = RandomPointInRadius(_spawnOriginX, _spawnOriginY, SpectralXSkeletonWar.PatrolRadius);
                    var skeletonWar = _spawnSkeletonWarCallback(swx, swy, _spawnOriginZ);
                    SkeletonWar.Add(skeletonWar);
                }
            }

            // ───────────────────────────────────────────────
            // Spawn Goatman
            // ───────────────────────────────────────────────
            if (_spawnGoatmanCallback != null)
            {
                for (int i = 0; i < goatmanCount; i++)
                {
                    var (gx, gy) = RandomPointInRadius(_spawnOriginX, _spawnOriginY, SpectralXGoatman.PatrolRadius);
                    var goatman = _spawnGoatmanCallback(gx, gy, _spawnOriginZ);
                    Goatman.Add(goatman);
                }
            }

            // ───────────────────────────────────────────────
            // Spawn ScavBoss (solo-spawn only — count is always 0 here)
            // ───────────────────────────────────────────────
            if (_spawnScavBossCallback != null)
            {
                for (int i = 0; i < scavBossCount; i++)
                {
                    var (sbx, sby) = RandomPointInRadius(_spawnOriginX, _spawnOriginY, SpectralXScavBoss.PatrolRadius);
                    var scavBoss = _spawnScavBossCallback(sbx, sby, _spawnOriginZ);
                    ScavBoss.Add(scavBoss);
                }
            }

            // ───────────────────────────────────────────────
            // Spawn SkeletonBoss (solo-spawn only — count is always 0 here)
            // ───────────────────────────────────────────────
            if (_spawnSkeletonBossCallback != null)
            {
                for (int i = 0; i < skeletonBossCount; i++)
                {
                    var (skbx, skby) = RandomPointInRadius(_spawnOriginX, _spawnOriginY, SpectralXSkeletonBoss.PatrolRadius);
                    var skeletonBoss = _spawnSkeletonBossCallback(skbx, skby, _spawnOriginZ);
                    SkeletonBoss.Add(skeletonBoss);
                }
            }

         

            _currentWave = waveId;
            Console.WriteLine($"[SpectralWaveSys] Wave {waveId} loaded — {skeletonCount} skeletons, {psychoCount} psychoskeletons, {zombiePsychoCount} zombiePsycho, {goatmanCount} goatman");
        }

        private void SpawnFromDef(int[] counts, int waveId, Action<float, float> spawnAction, float patrolRadius = SpectralXSkeleton.PatrolRadius)
        {
            if (counts == null || counts.Length < waveId) return;
            int count = counts[waveId - 1];
            for (int i = 0; i < count; i++)
            {
                var (x, y) = RandomPointInRadius(_spawnOriginX, _spawnOriginY, patrolRadius);
                spawnAction(x, y);
            }
        }
        // ── Random Spawn Point Helper ────────────────────────────────────
        private (float x, float y) RandomPointInRadius(float originX, float originY, float radius)
        {
            float x, y;
            do
            {
                double angle = _rng.NextDouble() * Math.PI * 2.0;
                double dist = Math.Sqrt(_rng.NextDouble()) * radius;
                x = originX + (float)(Math.Cos(angle) * dist);
                y = originY + (float)(Math.Sin(angle) * dist);
            }
            while (x * x + y * y < 25f); // keep outside radius 5 of player spawn at (0,0)
            return (x, y);
        }

        // ── Wave Completion Check ─────────────────────────────────────────
        public bool IsWaveCooked()
        {
            if (_currentWave == 0) return false;

            foreach (var skeleton in Skeletons)
                if (skeleton.EnemyIsAlive) return false;

            foreach (var psycho in PsychoSkeletons)
                if (psycho.EnemyIsAlive) return false;

            foreach (var zombiePsycho in ZombiePsycho)
                if (zombiePsycho.EnemyIsAlive) return false;

            foreach (var skeletonWar in SkeletonWar)
                if (skeletonWar.EnemyIsAlive) return false;

            foreach (var goatman in Goatman)
                if (goatman.EnemyIsAlive) return false;

            foreach (var scavBoss in ScavBoss)
                if (scavBoss.EnemyIsAlive) return false;

            foreach (var skeletonBoss in SkeletonBoss)
                if (skeletonBoss.EnemyIsAlive) return false;

           

            return true;
        }


      
        // ── Advance / Loop Waves ──────────────────────────────────────────
        public void TryAdvanceWave()
        {
            if (!IsWaveCooked()) return;

            int lastWave = _waveDefs.Length > 0 ? _waveDefs.Length : LastWaveId;

            if (_currentWave < lastWave)
            {
                ClearDeadFromRegistry();
                LoadWave(_currentWave + 1);
            }
            else
            {
                // Final wave cleared — schedule a respawn of wave 1 after a delay
                if (_nextRespawnAt == null)
                {
                    int delaySeconds = _rng.Next(5, 31);
                    _nextRespawnAt = DateTime.Now.AddSeconds(delaySeconds);
                    Console.WriteLine($"[SpectralWaveSys] Final wave cooked — respawning wave 1 in {delaySeconds}s");
                }

                if (DateTime.Now >= _nextRespawnAt)
                {
                    ClearDeadFromRegistry();
                    LoadWave(1);
                    _nextRespawnAt = null;
                }
            }
        }

        // ── Cleanup ───────────────────────────────────────────────────────
        /// <summary>
        /// Removes dead skeletons from the registry and tells the engine to
        /// despawn their meshes. Called before loading the next wave so the
        /// scene doesn't accumulate cooked corpses indefinitely.
        /// </summary>
        private void ClearDeadFromRegistry()
        {
            // No longer despawns on wave-complete — corpses persist independently.
            // Kept as a no-op call site for now in case you want wave-boundary
            // behavior later; actual cleanup happens in ClearExpiredCorpses().
        }
        // ── Tick — call once per frame from the engine ───────────────────
        public void TickAll(ISpectralCharacter target, float delta)
        {
            // ───────────────────────────────────────────────
            // Patrol Skeletons
            // ───────────────────────────────────────────────
            foreach (var skeleton in Skeletons)
            {
                if (skeleton.EnemyIsAlive)
                    skeleton.EnemyMove(target);

                skeleton.Tick(delta);
            }

            // ───────────────────────────────────────────────
            // PsychoSkeletons (always aggressive)
            // ───────────────────────────────────────────────
            foreach (var psycho in PsychoSkeletons)
            {
                if (psycho.EnemyIsAlive)
                    psycho.EnemyMove(target);

                psycho.Tick(delta);
            }

            // ───────────────────────────────────────────────
            // ZombiePsycho
            // ───────────────────────────────────────────────
            foreach (var zombiePsycho in ZombiePsycho)
            {
                if (zombiePsycho.EnemyIsAlive)
                    zombiePsycho.EnemyMove(target);

                zombiePsycho.Tick(delta);
            }

            // ───────────────────────────────────────────────
            // SkeletonWar
            // ───────────────────────────────────────────────
            foreach (var skeletonWar in SkeletonWar)
            {
                if (skeletonWar.EnemyIsAlive)
                    skeletonWar.EnemyMove(target);

                skeletonWar.Tick(delta);
            }

            // ───────────────────────────────────────────────
            // Goatman
            // ───────────────────────────────────────────────
            foreach (var goatman in Goatman)
            {
                if (goatman.EnemyIsAlive)
                    goatman.EnemyMove(target);

                goatman.Tick(delta);
            }

            // ───────────────────────────────────────────────
            // ScavBoss
            // ───────────────────────────────────────────────
            foreach (var scavBoss in ScavBoss)
            {
                if (scavBoss.EnemyIsAlive)
                    scavBoss.EnemyMove(target);

                scavBoss.Tick(delta);
            }

            // ───────────────────────────────────────────────
            // SkeletonBoss
            // ───────────────────────────────────────────────
            foreach (var skeletonBoss in SkeletonBoss)
            {
                if (skeletonBoss.EnemyIsAlive)
                    skeletonBoss.EnemyMove(target);

                skeletonBoss.Tick(delta);
            }

        

            TryAdvanceWave();
            ClearExpiredCorpses();
        }

        public void ClearExpiredCorpses()
        {
            // Skeleton corpses
            for (int i = Skeletons.Count - 1; i >= 0; i--)
            {
                if (Skeletons[i].IsCorpseExpired)
                {
                    _despawnSkeletonCallback?.Invoke(Skeletons[i]);
                    Skeletons.RemoveAt(i);
                }
            }

            // PsychoSkeleton corpses
            for (int i = PsychoSkeletons.Count - 1; i >= 0; i--)
            {
                if (PsychoSkeletons[i].IsCorpseExpired)
                {
                    _despawnPsychoSkeletonCallback?.Invoke(PsychoSkeletons[i]);
                    PsychoSkeletons.RemoveAt(i);
                }
            }

            // ZombiePsycho corpses
            for (int i = ZombiePsycho.Count - 1; i >= 0; i--)
            {
                if (ZombiePsycho[i].IsCorpseExpired)
                {
                    _despawnZombiePsychoCallback?.Invoke(ZombiePsycho[i]);
                    ZombiePsycho.RemoveAt(i);
                }
            }

            // SkeletonWar corpses
            for (int i = SkeletonWar.Count - 1; i >= 0; i--)
            {
                if (SkeletonWar[i].IsCorpseExpired)
                {
                    _despawnSkeletonWarCallback?.Invoke(SkeletonWar[i]);
                    SkeletonWar.RemoveAt(i);
                }
            }

            // Goatman corpses
            for (int i = Goatman.Count - 1; i >= 0; i--)
            {
                if (Goatman[i].IsCorpseExpired)
                {
                    _despawnGoatmanCallback?.Invoke(Goatman[i]);
                    Goatman.RemoveAt(i);
                }
            }

            // ScavBoss corpses
            for (int i = ScavBoss.Count - 1; i >= 0; i--)
            {
                if (ScavBoss[i].IsCorpseExpired)
                {
                    _despawnScavBossCallback?.Invoke(ScavBoss[i]);
                    ScavBoss.RemoveAt(i);
                }
            }

            // SkeletonBoss corpses
            for (int i = SkeletonBoss.Count - 1; i >= 0; i--)
            {
                if (SkeletonBoss[i].IsCorpseExpired)
                {
                    _despawnSkeletonBossCallback?.Invoke(SkeletonBoss[i]);
                    SkeletonBoss.RemoveAt(i);
                }
            }

            // Cow corpses
          
        }


        /// <summary>
        /// Hard reset — clears all enemies and wave state. Call on scene switch.
        /// </summary>
        public void ClearAll()
        {
            foreach (var skeleton in Skeletons)
                _despawnSkeletonCallback?.Invoke(skeleton);

            foreach (var psycho in PsychoSkeletons)
                _despawnPsychoSkeletonCallback?.Invoke(psycho);

            foreach (var zombiePsycho in ZombiePsycho)
                _despawnZombiePsychoCallback?.Invoke(zombiePsycho);

            foreach (var skeletonWar in SkeletonWar)
                _despawnSkeletonWarCallback?.Invoke(skeletonWar);

            foreach (var goatman in Goatman)
                _despawnGoatmanCallback?.Invoke(goatman);

            foreach (var scavBoss in ScavBoss)
                _despawnScavBossCallback?.Invoke(scavBoss);

            foreach (var skeletonBoss in SkeletonBoss)
                _despawnSkeletonBossCallback?.Invoke(skeletonBoss);

  

            Skeletons.Clear();
            PsychoSkeletons.Clear();
            ZombiePsycho.Clear();
            SkeletonWar.Clear();
            Goatman.Clear();
            ScavBoss.Clear();
            SkeletonBoss.Clear();


            _currentWave = 0;
            _nextRespawnAt = null;

            Console.WriteLine("[SpectralWaveSys] All waves cleared");
        }

        // ── Per Scene Wave Definitions ─────────────────────────────────────
        private readonly Dictionary<int, WaveDefinition[]> _sceneWaveDefs = new();

        public void SetScene(int sceneId)
        {
            if (_sceneWaveDefs.TryGetValue(sceneId, out var defs))
            {
                _waveDefs = defs;
                _currentWave = 0;
                Console.WriteLine($"[SpectralWaveSys] Set scene {sceneId} - {defs.Length} waves loaded");
            }
            else
            {
                Console.WriteLine($"[SpectralWaveSys] No wave definition for scene {sceneId}");
                _waveDefs = Array.Empty<WaveDefinition>();
            }
        }

        // Keep the WaveDefinition struct from before
        public readonly struct WaveDefinition
        {
            public int[] Skeletons { get; init; }
            public int[] PsychoSkeletons { get; init; }
            public int[] ZombiePsycho { get; init; }
            public int[] SkeletonWar { get; init; }
            public int[] Goatman { get; init; }
            public int[] ScavBoss { get; init; }
            public int[] SkeletonBoss { get; init; }

            public Action? OnWaveStart { get; init; }   // scene call per wave

            public WaveDefinition()
            {
                Skeletons = Array.Empty<int>();
                PsychoSkeletons = Array.Empty<int>();
                ZombiePsycho = Array.Empty<int>();
                SkeletonWar = Array.Empty<int>();
                Goatman = Array.Empty<int>();
                ScavBoss = Array.Empty<int>();
                SkeletonBoss = Array.Empty<int>();
            }
        }

        private WaveDefinition[] _waveDefs = Array.Empty<WaveDefinition>();


        public void RegisterSceneWaves(int sceneId, WaveDefinition[] waves)
        {
            _sceneWaveDefs[sceneId] = waves;
            Console.WriteLine($"[WaveSys] Registered {waves.Length} waves for scene {sceneId}");
        }






    }
}