using System.Numerics;
using SpectralXGLX.SpectralXComponent;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// General-purpose static prop spawn system for BWP-style scenes.
    /// Successor to SpectralXBWPStaticObjects — same billboard/warrior-baseline
    /// scaling and overlap-avoidance logic, but the prop set is passed in per
    /// call instead of hardcoded, so each scene (forest, town, etc.) can define
    /// its own mix without duplicating the spawn loop.
    /// </summary>
    public class SpectralProps
    {
        // Warrior is 84x84 pixels = 1x1 world units — same baseline as before
        private const float WarriorPixelSize = 84f;

        public class StaticObjectRecord
        {
            public string Type { get; set; } = "";
            public string MeshId { get; set; } = "";
            public float WorldX { get; set; }
            public float WorldY { get; set; }
            public float Radius { get; set; }
        }

        public List<StaticObjectRecord> Spawned { get; } = new();

        public static Vector3 GetWorldScale(ISpectralProps def)
        {
            float scaleX = def.PixelWidth / WarriorPixelSize;
            float scaleY = def.PixelHeight / WarriorPixelSize;
            float scale = MathF.Max(scaleX, scaleY);
            return new Vector3(scale, scale, scale);
        }

        /// <summary>
        /// Random-scattered props with overlap avoidance — same behavior as the
        /// original forest spawner. Pass a typeCounts dictionary keyed by prop
        /// Type, and a propDefs lookup describing each type's size/collision.
        /// </summary>
        public void SpawnScattered(
            SpectralXMeshLibrary meshLibrary,
            SpectralXScene scene,
            Dictionary<string, int> typeCounts,
            IReadOnlyDictionary<string, ISpectralProps> propDefs,
            float spawnRadius = 64f,
            float clearRadius = 3f,
            int seed = 99,
            string meshIdPrefix = "Static_")
        {
            var rand = new Random(seed);
            int globalIndex = Spawned.Count;
            const int maxAttempts = 100;

            foreach (var kvp in typeCounts)
            {
                string type = kvp.Key;
                int count = kvp.Value;

                if (!propDefs.TryGetValue(type, out var def))
                {
                    Console.WriteLine($"[SpectralProps] No prop definition for type '{type}' — skipping");
                    continue;
                }

                float radius = def.CollisionRadius;
                Vector3 worldScale = GetWorldScale(def);

                for (int i = 0; i < count; i++)
                {
                    int attempts = 0;
                    float wx, wy;
                    bool valid;

                    do
                    {
                        wx = (float)(rand.NextDouble() * 2.0 - 1.0) * spawnRadius;
                        wy = (float)(rand.NextDouble() * 2.0 - 1.0) * spawnRadius;
                        valid = true;

                        float dw = MathF.Sqrt(wx * wx + wy * wy);
                        if (dw < clearRadius)
                        {
                            valid = false;
                            attempts++;
                            continue;
                        }

                        foreach (var existing in Spawned)
                        {
                            float dx = wx - existing.WorldX;
                            float dy = wy - existing.WorldY;
                            float dist = MathF.Sqrt(dx * dx + dy * dy);
                            if (dist < radius + existing.Radius)
                            {
                                valid = false;
                                break;
                            }
                        }

                        attempts++;
                    }
                    while (!valid && attempts < maxAttempts);

                    if (!valid)
                    {
                        Console.WriteLine($"[SpectralProps] Could not place {type} after {maxAttempts} attempts — skipping");
                        continue;
                    }

                    SpawnOne(meshLibrary, scene, def, wx, wy, worldScale, meshIdPrefix, ref globalIndex);
                }
            }

            Console.WriteLine($"[SpectralProps] Scattered spawn total: {Spawned.Count}");
        }

        /// <summary>
        /// Fixed-position props — for buildings/landmarks that need an exact
        /// spot rather than random scatter. Still registered into Spawned for
        /// overlap checks against any scattered props spawned afterward.
        /// </summary>
        public void SpawnFixed(
            SpectralXMeshLibrary meshLibrary,
            SpectralXScene scene,
            IEnumerable<SpectralPropPlacement> placements,
            IReadOnlyDictionary<string, ISpectralProps> propDefs,
            string meshIdPrefix = "Static_")
        {
            int globalIndex = Spawned.Count;

            foreach (var placement in placements)
            {
                if (!propDefs.TryGetValue(placement.Type, out var def))
                {
                    Console.WriteLine($"[SpectralProps] No prop definition for type '{placement.Type}' — skipping fixed placement");
                    continue;
                }

                Vector3 worldScale = placement.WorldScaleOverride ?? GetWorldScale(def);
                SpawnOne(meshLibrary, scene, def, placement.WorldX, placement.WorldY, worldScale, meshIdPrefix, ref globalIndex);
            }

            Console.WriteLine($"[SpectralProps] Fixed spawn total this call: {Spawned.Count}");
        }

        private void SpawnOne(
            SpectralXMeshLibrary meshLibrary,
            SpectralXScene scene,
            ISpectralProps def,
            float wx, float wy,
            Vector3 worldScale,
            string meshIdPrefix,
            ref int globalIndex)
        {
            string meshId = $"{meshIdPrefix}{def.Type}_{globalIndex}";
            globalIndex++;

            var mesh = meshLibrary.GetMesh("PrimSquare") as SpectralXMesh;
            if (mesh == null)
            {
                Console.WriteLine($"[SpectralProps] PrimSquare not found in MeshLibrary — skipping {meshId}");
                return;
            }

            var staticMesh = new SpectralXMesh(meshId);
            staticMesh.Vertices.AddRange(mesh.Vertices);
            staticMesh.Normals.AddRange(mesh.Normals);
            staticMesh.UVs.AddRange(mesh.UVs);
            foreach (var face in mesh.Faces)
                staticMesh.Faces.Add(face);

            staticMesh.Size = worldScale;

            float heightScale = (def.PixelHeight / WarriorPixelSize) * MathF.Sin(5f * MathF.PI / 180f);
            staticMesh.Position = new Vector3(wx, wy, 0.1f + heightScale);
            staticMesh.Rotation = new Vector3(
          5f * (MathF.PI / 180f),
         0f,
          0f
      );


            staticMesh.TextureDataUrl = $"/iAssets/{def.Type}.png";
            staticMesh.TextureDirty = true;
            staticMesh.Color = new Vector4(1f, 1f, 1f, 1f);
            staticMesh.CastsShadow = false;
            staticMesh.IsAnimated = false;

            staticMesh.UVScaleX = 1f;
            staticMesh.UVScaleY = 1f;
            staticMesh.UVOffsetX = 0f;
            staticMesh.UVOffsetY = 0f;
            staticMesh.TransformDirty = true;

            scene.AddMesh(staticMesh);

            Spawned.Add(new StaticObjectRecord
            {
                Type = def.Type,
                MeshId = meshId,
                WorldX = wx,
                WorldY = wy,
                Radius = def.CollisionRadius,
            });

            Console.WriteLine($"[SpectralProps] Spawned {meshId} at ({wx:F1},{wy:F1}) scale:{worldScale.X:F2}");
        }
    }
}
