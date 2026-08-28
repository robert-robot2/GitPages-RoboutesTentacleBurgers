using System.Numerics;
using SpectralXGLX.SpectralXComponent;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// Static object spawn system for BloodWyrm Protocol Scene4.
    /// Spawns billboard PrimSquare meshes sized relative to warrior baseline 84x84 = 1x1 world unit.
    /// All objects face the isometric camera using warrior rotation.
    /// </summary>
    public class SpectralXBWPStaticObjects
    {
        // ── Warrior Baseline ─────────────────────────────────────
        // Warrior is 84x84 pixels = 1x1 world units
        private const float WarriorPixelSize = 84f;

        // ── Pixel Size Map ────────────────────────────────────────
        // Original 2D pixel dimensions per object type
        public static readonly Dictionary<string, (float w, float h)> PixelSizeMap = new()
        {
            { "Tree",           (164f, 188f) },
            { "Rock",           (74f,  74f)  },
            { "FenceBroken",    (100f, 64f)  },
            { "TorchNew01",     (48f,  64f)  },
            { "Chest",          (48f,  48f)  },
            { "GStone",         (36f,  48f)  },
            { "GStoneCross",    (36f,  48f)  },
            { "Mushroom",       (36f,  36f)  },
            { "Skelcorpse001",  (112f, 48f)  },
            { "Grass01",        (32f,  32f)  },
            { "SkullONStick",   (48f,  64f)  },
            { "Urn",            (36f,  36f)  },
            { "Rose01",         (16f,  16f)  },
            { "Barrel01",       (64f,  64f)  },
            { "Bush01",         (88f,  88f)  },
        };

        // ── Forest Spawn Dictionary ───────────────────────────────
        // Type -> count, scaled down from 2048x2048 original to 128x128 world units
        public static Dictionary<string, int> ForestDictionary() => new()
        {
            { "Tree",           75 },
            { "Rock",           50  },
            { "FenceBroken",    10  },
            { "TorchNew01",     2  },
            { "Chest",          2  },
            { "GStone",         4  },
            { "GStoneCross",    4  },
            { "Mushroom",       4  },
            { "Skelcorpse001",  1  },
            { "Grass01",        15 },
            { "SkullONStick",   3  },
            { "Urn",            3  },
            { "Rose01",         4  },
            { "Barrel01",       3  },
            { "Bush01",         35  },
        };

        // ── World Scale Calculator ────────────────────────────────
        /// <summary>
        /// Converts pixel dimensions to world scale relative to warrior 84x84 = 1x1.
        /// </summary>
        public static Vector3 GetWorldScale(string type)
        {
            if (!PixelSizeMap.TryGetValue(type, out var px))
                return new Vector3(1f, 1f, 1f);

            float scaleX = px.w / WarriorPixelSize;
            float scaleY = px.h / WarriorPixelSize;

            // Use max dimension to keep proportions clean on billboard
            float scale = MathF.Max(scaleX, scaleY);
            return new Vector3(scale, scale, scale);
        }

        // ── Collision Radius Map ──────────────────────────────────
        // Simple world-space radius per type for overlap checking
        public static readonly Dictionary<string, float> CollisionRadiusMap = new()
        {
            { "Tree",           1.2f },
            { "Rock",           0.5f },
            { "FenceBroken",    0.8f },
            { "TorchNew01",     0.3f },
            { "Chest",          0.3f },
            { "GStone",         0.3f },
            { "GStoneCross",    0.3f },
            { "Mushroom",       0.2f },
            { "Skelcorpse001",  0.5f },
            { "Grass01",        0.2f },
            { "SkullONStick",   0.3f },
            { "Urn",            0.2f },
            { "Rose01",         0.15f},
            { "Barrel01",       0.4f },
            { "Bush01",         0.6f },
        };

        // ── Spawned Object Record ─────────────────────────────────
        public class StaticObjectRecord
        {
            public string Type { get; set; } = "";
            public string MeshId { get; set; } = "";
            public float WorldX { get; set; }
            public float WorldY { get; set; }
            public float Radius { get; set; }
        }

        // ── Spawn Registry ────────────────────────────────────────
        public List<StaticObjectRecord> Spawned { get; } = new();

        // ── Main Spawn Method ─────────────────────────────────────
        /// <summary>
        /// Spawns all static objects into Scene4 as billboard PrimSquare meshes.
        /// Call from InitScene4() after warrior is initialized.
        /// </summary>
        public void SpawnAll(
            SpectralXMeshLibrary meshLibrary,
            SpectralXScene scene,
            float spawnRadius = 64f,
            int seed = 99)
        {
            Spawned.Clear();
            var rand = new Random(seed);
            var dict = ForestDictionary();
            int globalIndex = 0;
            const int maxAttempts = 100;

            // Keep warrior spawn clear — no objects within 3 units of origin
            const float warriorClearRadius = 3f;

            foreach (var kvp in dict)
            {
                string type = kvp.Key;
                int count = kvp.Value;
                float radius = CollisionRadiusMap.TryGetValue(type, out var r) ? r : 0.5f;
                Vector3 worldScale = GetWorldScale(type);

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

                        // Keep warrior spawn area clear
                        float dw = MathF.Sqrt(wx * wx + wy * wy);
                        if (dw < warriorClearRadius)
                        {
                            valid = false;
                            attempts++;
                            continue;
                        }

                        // Check overlap against already spawned objects
                        foreach (var existing in Spawned)
                        {
                            float dx = wx - existing.WorldX;
                            float dy = wy - existing.WorldY;
                            float dist = MathF.Sqrt(dx * dx + dy * dy);
                            float minDist = radius + existing.Radius;
                            if (dist < minDist)
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
                        Console.WriteLine($"[StaticObjects] Could not place {type} after {maxAttempts} attempts — skipping");
                        continue;
                    }

                    // Build unique mesh ID
                    string meshId = $"Static_{type}_{globalIndex}";
                    globalIndex++;

                    // Clone PrimSquare
                    var mesh = meshLibrary.GetMesh("PrimSquare") as SpectralXMesh;
                    if (mesh == null)
                    {
                        Console.WriteLine($"[StaticObjects] PrimSquare not found in MeshLibrary — skipping {meshId}");
                        continue;
                    }

                    // Create new mesh instance
                    var source = meshLibrary.GetMesh("PrimSquare") as SpectralXMesh;
                    if (source == null) continue;

                    var staticMesh = new SpectralXMesh(meshId);
                    staticMesh.Vertices.AddRange(source.Vertices);
                    staticMesh.Normals.AddRange(source.Normals);
                    staticMesh.UVs.AddRange(source.UVs);
                    foreach (var face in source.Faces)
                        staticMesh.Faces.Add(face);

                    // Size relative to warrior baseline
                    staticMesh.Size = worldScale;

                    // Position — Z lifted so base sits on ground
                    // Warrior Z is 1.0, scale by object height scale so taller objects lift correctly
                    // PrimSquare is centered at origin — lift by half height so base sits at Z=0
                    // worldScale.Z represents height, 0.5f centers it, then add small ground offset
                    float heightScale = (PixelSizeMap.TryGetValue(type, out var pxZ) ? pxZ.h : WarriorPixelSize) / WarriorPixelSize;
                    staticMesh.Position = new Vector3(wx, wy, heightScale);

                    // Rotation — match warrior exactly so billboard faces iso camera
                    staticMesh.Rotation = new Vector3(
          5f * (MathF.PI / 180f),
         0f,
          0f
      );


                    // Texture
                    //  staticMesh.HasTexture = true;
                    staticMesh.TextureDataUrl = $"/iAssets/{type}.png";
                    staticMesh.TextureDirty = true;
                    staticMesh.Color = new Vector4(1f, 1f, 1f, 1f);
                    staticMesh.CastsShadow = false;
                    staticMesh.IsAnimated = false;

                    // UV scale — single frame full texture
                    staticMesh.UVScaleX = 1f;
                    staticMesh.UVScaleY = 1f;
                    staticMesh.UVOffsetX = 0f;
                    staticMesh.UVOffsetY = 0f;

                    staticMesh.TransformDirty = true;

                    scene.AddMesh(staticMesh);

                    // Record for overlap checking
                    Spawned.Add(new StaticObjectRecord
                    {
                        Type = type,
                        MeshId = meshId,
                        WorldX = wx,
                        WorldY = wy,
                        Radius = radius,
                    });

                    Console.WriteLine($"[StaticObjects] Spawned {meshId} at ({wx:F1},{wy:F1}) scale:{worldScale.X:F2}");
                }
            }

            Console.WriteLine($"[StaticObjects] Total spawned: {Spawned.Count}");
        }
    }
}