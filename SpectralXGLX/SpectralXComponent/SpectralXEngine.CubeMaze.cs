using System;
using System.Collections.Generic;
using System.Text;

namespace SpectralXGLX.SpectralXComponent
{
    public partial class SpectralXEngine
    {
        private Random _mazeRand = new Random();

        private readonly List<CubeMazeCell> _mazeRemoveBuffer = new();
        private void TickCubeMaze(float delta)
        {
            if (_cubeMazeCells.Count == 0) return;

            float now = (float)(DateTime.UtcNow - _startTime).TotalSeconds;
            var rand = _mazeRand;

            // --- Effect scheduler ---
            if (!_mazeEffectActive)
            {
                _mazeNextEffectTimer -= delta;
                _mazeExplodeTimer += delta; // keep idle bump logic ticking

                if (_mazeNextEffectTimer <= 0f)
                {
                    // pick a random effect 0-7
                    _currentMazeEffect = rand.Next(8);
                    _mazeEffectActive = true;
                    _mazeEffectTimer = 0f;
                    _mazeNextEffectTimer = 10f + (float)rand.NextDouble() * 8f;

                    // snapshot home positions for effects that need them
                    int cellCount = _cubeMazeCells.Count;
                    for (int i = 0; i < cellCount; i++)
                        _effectHomeSnapshot[i] = _cubeMazeCells[i].HomePos;

                    var center = new Vector3(4.5f, 0f, 4.5f);

                    // ---- Per-effect setup ----
                    if (_currentMazeEffect == 0)
                    {
                        // EFFECT 0 – "EXPLOSION" (original)
                        // Cubes get fed up, detonate outward, drift back
                        _mazeEffectDuration = ExplodeDuration + ReformDuration;
                        foreach (var cell in _cubeMazeCells)
                        {
                            var dir = Vector3.Normalize(cell.Mesh.Position - center + new Vector3(
                                (float)(rand.NextDouble() - 0.5f) * 0.5f, 1f,
                                (float)(rand.NextDouble() - 0.5f) * 0.5f));
                            float speed = 4f + (float)rand.NextDouble() * 6f;
                            cell.ExplodeVel = dir * speed;
                        }
                    }
                    else if (_currentMazeEffect == 1)
                    {
                        // EFFECT 1 – "RAGE SCATTER"
                        // The cubes have had ENOUGH. Every single one bolts in a random direction.
                        // They run as far as they can, panic, then shamefully slink back home.
                        _mazeEffectDuration = 3.5f;
                        foreach (var cell in _cubeMazeCells)
                        {
                            float angle = (float)rand.NextDouble() * MathF.PI * 2f;
                            float dist = 6f + (float)rand.NextDouble() * 6f;
                            cell.ExplodeVel = new Vector3(
                                MathF.Cos(angle) * dist,
                                (float)(rand.NextDouble()) * 3f,
                                MathF.Sin(angle) * dist);
                            cell.Mesh.Color = new Vector4(1f, 0.1f, 0.1f, 1f); // rage red
                        }
                    }
                    else if (_currentMazeEffect == 2)
                    {
                        // EFFECT 2 – "VOID INVASION"
                        // Dark void cubes silently spawn at the edges and slide inward.
                        // They slam into regular cubes and send them flying. Then dissolve.
                        _mazeEffectDuration = 4f;
                        int voidCount = 5 + rand.Next(5);
                        for (int v = 0; v < voidCount; v++)
                        {
                            var voidCube = MeshLibrary.GetMesh("PrimCube") as SpectralXMesh;
                            if (voidCube == null) continue;

                            // spawn on a random edge
                            float ex = (float)(rand.NextDouble() < 0.5 ? -3 : 13);
                            float ez = (float)(rand.NextDouble() * 10);
                            voidCube.Position = new Vector3(ex, 0f, ez);
                            voidCube.Size = new Vector3(0.7f, 0.7f, 0.7f);
                            voidCube.Color = new Vector4(0.05f, 0f, 0.2f, 1f); // void purple-black

                            // target: random existing cube's home
                            var target = _cubeMazeCells[rand.Next(_cubeMazeCells.Count)];
                            var dir = Vector3.Normalize(target.HomePos - voidCube.Position);

                            Scene3.AddMesh(voidCube);
                            _cubeMazeCells.Add(new CubeMazeCell
                            {
                                Mesh = voidCube,
                                HomePos = voidCube.Position,          // void cubes don't have a real home
                                EffectTargetPos = target.HomePos,
                                Velocity = dir * (5f + (float)rand.NextDouble() * 3f),
                                ExplodeVel = dir * (5f + (float)rand.NextDouble() * 3f),
                                BumpTimer = 9999f,
                                BumpDuration = 0f,
                                BumpVelocity = Vector3.Zero,
                                EffectPhase = 0f,
                                IsVoid = true,
                            });
                        }
                        // re-snapshot now that void cubes are added (they'll be cleaned up on effect end)
                    }
                    else if (_currentMazeEffect == 3)
                    {
                        // EFFECT 3 – "SPACESHIP ATTEMPT"
                        // The cubes huddle together into a vaguely ship-shaped clump,
                        // slowly ascend like they're launching, then lose cohesion mid-air
                        // and explode dramatically. They've seen too many sci-fi movies.
                        _mazeEffectDuration = 5f;
                        // assign formation targets: tight cluster + rough nose shape
                        float cx = 4.5f, cz = 4.5f;
                        int count = _cubeMazeCells.Count;
                        for (int i = 0; i < count; i++)
                        {
                            var cell = _cubeMazeCells[i];
                            float angle = (float)i / count * MathF.PI * 2f;
                            float r = (i % 3 == 0) ? 0.6f : (i % 3 == 1 ? 1.3f : 2.0f);
                            cell.EffectTargetPos = new Vector3(
                                cx + MathF.Cos(angle) * r,
                                0f,
                                cz + MathF.Sin(angle) * r * 0.5f); // flattened ellipse = ship silhouette
                            cell.Mesh.Color = new Vector4(0.4f, 0.8f, 1f, 0.9f); // sci-fi blue
                            cell.ExplodeVel = Vector3.Zero;
                        }
                    }
                    else if (_currentMazeEffect == 4)
                    {
                        // EFFECT 4 – "MILITIA FORMATION"
                        // A bugle sounds in the cubes' hearts. They march into disciplined lines.
                        // Rows face the same direction, standing at attention, ready for war.
                        // Nobody knows what they're fighting. That's fine. Neither do they.
                        _mazeEffectDuration = 5f;
                        int count = _cubeMazeCells.Count;
                        int cols = 7;
                        for (int i = 0; i < count; i++)
                        {
                            int row = i / cols;
                            int col = i % cols;
                            _cubeMazeCells[i].EffectTargetPos = new Vector3(
                                1f + col * 1.1f,
                                0f,
                                1f + row * 1.1f);
                            _cubeMazeCells[i].Mesh.Color = new Vector4(0.2f, 0.9f, 0.2f, 1f); // army green
                            _cubeMazeCells[i].ExplodeVel = Vector3.Zero;
                        }
                    }
                    else if (_currentMazeEffect == 5)
                    {
                        // EFFECT 5 – "WAVE DANCE"
                        // A mysterious signal ripples through the grid. The cubes begin to
                        // undulate in a mesmerizing sine wave. They don't know why.
                        // It just feels right. They will never speak of this again.
                        _mazeEffectDuration = 4f;
                        foreach (var cell in _cubeMazeCells)
                        {
                            cell.ExplodeVel = Vector3.Zero;
                            // rainbow color based on home position
                            float hue = (cell.HomePos.X + cell.HomePos.Z) / 20f;
                            cell.Mesh.Color = new Vector4(
                                0.5f + 0.5f * MathF.Sin(hue * MathF.PI * 2f),
                                0.5f + 0.5f * MathF.Sin(hue * MathF.PI * 2f + 2.09f),
                                0.5f + 0.5f * MathF.Sin(hue * MathF.PI * 2f + 4.19f),
                                1f);
                        }
                    }
                    else if (_currentMazeEffect == 6)
                    {
                        // EFFECT 6 – "BLACK HOLE"
                        // A gravitational anomaly materializes at the center.
                        // All cubes spiral inward in a vortex, compressed to a single point,
                        // then are flung outward in a massive nova burst.
                        // Physics is more of a suggestion here.
                        _mazeEffectDuration = 4f;
                        foreach (var cell in _cubeMazeCells)
                        {
                            cell.ExplodeVel = Vector3.Zero;
                            cell.Mesh.Color = new Vector4(0.8f, 0.2f, 1f, 1f); // event horizon purple
                        }
                    }
                    else if (_currentMazeEffect == 7)
                    {
                        // EFFECT 7 – "SNAKE PARADE"
                        // The cubes receive an ancient instinct: move in a line.
                        // They chain together and slither across the floor in a long winding
                        // parade, looping the grid like a snake eating its own tail.
                        // Eventually they get dizzy and return home.
                        _mazeEffectDuration = 5f;
                        // assign order positions along a snake path
                        int count = _cubeMazeCells.Count;
                        for (int i = 0; i < count; i++)
                        {
                            float t = (float)i / count;
                            float angle = t * MathF.PI * 6f; // 3 full loops
                            float radius = 1.5f + t * 2.5f;
                            _cubeMazeCells[i].EffectTargetPos = new Vector3(
                                4.5f + MathF.Cos(angle) * radius,
                                0f,
                                4.5f + MathF.Sin(angle) * radius);
                            // rainbow snake
                            _cubeMazeCells[i].Mesh.Color = new Vector4(
                                0.5f + 0.5f * MathF.Sin(t * MathF.PI * 2f),
                                0.5f + 0.5f * MathF.Cos(t * MathF.PI * 2f),
                                1f - t * 0.7f,
                                1f);
                            _cubeMazeCells[i].ExplodeVel = Vector3.Zero;
                        }
                    }
                }
            }

            // ---- Active effect tick ----
            if (_mazeEffectActive)
            {
                _mazeEffectTimer += delta;
                float et = _mazeEffectTimer;
                bool effectDone = et >= _mazeEffectDuration;

                if (_currentMazeEffect == 0) // EXPLOSION
                {
                    if (et < ExplodeDuration)
                    {
                        foreach (var cell in _cubeMazeCells)
                        {
                            cell.Mesh.Position += cell.ExplodeVel * delta;
                            cell.ExplodeVel *= (1f - delta * 1.5f);
                            cell.Mesh.Rotation += cell.ExplodeVel * delta * 2f;
                            cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                        }
                    }
                    else
                    {
                        float t = (et - ExplodeDuration) / ReformDuration;
                        float ease = 1f - (1f - t) * (1f - t);
                        foreach (var cell in _cubeMazeCells)
                        {
                            cell.Mesh.Position = Vector3.Lerp(cell.Mesh.Position, cell.HomePos, ease * delta * 6f);
                            cell.Mesh.Rotation = Vector3.Lerp(cell.Mesh.Rotation, Vector3.Zero, ease * delta * 6f);
                            cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                        }
                    }
                }
                else if (_currentMazeEffect == 1) // RAGE SCATTER
                {
                    float halfDur = _mazeEffectDuration * 0.45f;
                    if (et < halfDur)
                    {
                        foreach (var cell in _cubeMazeCells)
                        {
                            cell.Mesh.Position += cell.ExplodeVel * delta;
                            cell.ExplodeVel *= (1f - delta * 0.8f);
                            cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                        }
                    }
                    else
                    {
                        // panic-return: cubes cool off (color fades back to white) and slink home
                        float t = (et - halfDur) / (_mazeEffectDuration - halfDur);
                        float ease = t * t;
                        foreach (var cell in _cubeMazeCells)
                        {
                            cell.Mesh.Position = Vector3.Lerp(cell.Mesh.Position, cell.HomePos, ease * delta * 5f);
                            cell.Mesh.Color = Vector4.Lerp(cell.Mesh.Color, new Vector4(1f, 1f, 1f, 0.7f), delta * 2f);
                            cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                        }
                    }
                }
                else if (_currentMazeEffect == 2) // VOID INVASION
                {
                    float attackPhase = _mazeEffectDuration * 0.5f;
                    if (et < attackPhase)
                    {
                        // void cubes advance; check for "collisions" with regular cubes
                        foreach (var cell in _cubeMazeCells)
                        {
                            if (cell.IsVoid)
                            {
                                cell.Mesh.Position += cell.Velocity * delta;
                                cell.Velocity *= (1f - delta * 1.2f);
                                // pulse dark
                                float pulse = 0.5f + 0.5f * MathF.Sin(et * 8f);
                                cell.Mesh.Color = new Vector4(pulse * 0.2f, 0f, pulse * 0.4f, 1f);
                                cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;

                                // proximity knock-back: hit nearby regular cubes
                                if (cell.Velocity.LengthSquared() < 0.05f) continue; // skip slow voids entirely
                                foreach (var other in _cubeMazeCells)
                                {
                                    if (other.IsVoid) continue;
                                    // Replace with:
                                    float distSq = Vector3.DistanceSquared(cell.Mesh.Position, other.Mesh.Position);
                                    if (distSq < 1.0f)
                                    {
                                        float dist = MathF.Sqrt(distSq); // only compute sqrt when we actually need it
                                        var knock = Vector3.Normalize(other.Mesh.Position - cell.Mesh.Position + Vector3.UnitY * 0.3f);
                                        other.BumpVelocity = knock * (4f / (dist + 0.1f));
                                        other.BumpDuration = 0.4f;
                                        other.Mesh.Color = new Vector4(0.3f, 0f, 1f, 1f); // void-touched blue
                                        other.Bumped = true;
                                    }
                                }
                            }
                            else
                            {
                                // regular cubes react to bumps
                                if (cell.BumpDuration > 0f)
                                {
                                    cell.Mesh.Position += cell.BumpVelocity * delta;
                                    cell.BumpVelocity *= (1f - delta * 6f);
                                    cell.BumpDuration -= delta;
                                }
                                else
                                {
                                    // drift back home
                                    cell.Mesh.Position = Vector3.Lerp(cell.Mesh.Position, cell.HomePos, delta * 3f);
                                }
                                cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                            }
                        }
                    }
                    else
                    {
                        // void cubes dissolve; regular cubes return home
                        float t = (et - attackPhase) / (_mazeEffectDuration - attackPhase);
                        foreach (var cell in _cubeMazeCells)
                        {
                            if (cell.IsVoid)
                            {
                                cell.Mesh.Color = new Vector4(0f, 0f, 0f, 1f - t);
                                cell.Mesh.Size = Vector3.Lerp(cell.Mesh.Size, Vector3.Zero, delta * 3f);
                                cell.Mesh.TransformDirty =
     Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
     cell.ExplodeVel.LengthSquared() > 0.00001f ||
     cell.Velocity.LengthSquared() > 0.00001f;
                            }
                            else
                            {
                                cell.Mesh.Position = Vector3.Lerp(cell.Mesh.Position, cell.HomePos, t * delta * 6f);
                                cell.Mesh.Color = Vector4.Lerp(cell.Mesh.Color, new Vector4(1f, 1f, 1f, 0.7f), delta * 2f);
                                cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                            }
                        }
                    }
                }
                else if (_currentMazeEffect == 3) // SPACESHIP ATTEMPT
                {
                    float gatherPhase = 1.5f;
                    float liftPhase = gatherPhase + 1.5f;
                    float explodePhase = liftPhase + 0.3f;

                    if (et < gatherPhase)
                    {
                        // gather into formation
                        float t = et / gatherPhase;
                        foreach (var cell in _cubeMazeCells)
                        {
                            cell.Mesh.Position = Vector3.Lerp(cell.Mesh.Position, cell.EffectTargetPos, t * delta * 5f);
                            cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                        }
                    }
                    else if (et < liftPhase)
                    {
                        // ascend together like a launch — slowly, majestically
                        float liftT = (et - gatherPhase) / (liftPhase - gatherPhase);
                        float liftY = liftT * 6f;
                        foreach (var cell in _cubeMazeCells)
                        {
                            var target = new Vector3(cell.EffectTargetPos.X, liftY, cell.EffectTargetPos.Z);
                            cell.Mesh.Position = Vector3.Lerp(cell.Mesh.Position, target, delta * 4f);
                            // engine glow brightens
                            cell.Mesh.Color = Vector4.Lerp(cell.Mesh.Color, new Vector4(1f, 0.6f, 0.1f, 1f), delta * 3f);
                            cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                        }
                    }
                    else if (et < explodePhase)
                    {
                        // catastrophic mid-flight disintegration
                        foreach (var cell in _cubeMazeCells)
                        {
                            if (cell.ExplodeVel == Vector3.Zero)
                            {
                                cell.ExplodeVel = new Vector3(
                                    (float)(rand.NextDouble() - 0.5f) * 12f,
                                    (float)rand.NextDouble() * 8f + 2f,
                                    (float)(rand.NextDouble() - 0.5f) * 12f);
                            }
                            cell.Mesh.Position += cell.ExplodeVel * delta;
                            cell.ExplodeVel *= (1f - delta * 1.2f);
                            cell.Mesh.Color = new Vector4(1f, 0.2f + (float)rand.NextDouble() * 0.4f, 0f, 1f);
                            cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                        }
                    }
                    else
                    {
                        // drift back home
                        float t = (et - explodePhase) / (_mazeEffectDuration - explodePhase);
                        foreach (var cell in _cubeMazeCells)
                        {
                            cell.Mesh.Position = Vector3.Lerp(cell.Mesh.Position, cell.HomePos, t * delta * 5f);
                            cell.Mesh.Color = Vector4.Lerp(cell.Mesh.Color, new Vector4(1f, 1f, 1f, 0.7f), delta * 2f);
                            cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                        }
                    }
                }
                else if (_currentMazeEffect == 4) // MILITIA FORMATION
                {
                    float marchPhase = 2.0f;
                    float holdPhase = marchPhase + 1.5f;
                    if (et < marchPhase)
                    {
                        float t = et / marchPhase;
                        foreach (var cell in _cubeMazeCells)
                        {
                            cell.Mesh.Position = Vector3.Lerp(cell.Mesh.Position, cell.EffectTargetPos, t * delta * 6f);
                            // march stutter: slight Y bounce in step
                            float bounce = MathF.Abs(MathF.Sin(et * 8f + cell.EffectPhase)) * 0.15f * (1f - t);
                            cell.Mesh.Position = new Vector3(cell.Mesh.Position.X, bounce, cell.Mesh.Position.Z);
                            cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                        }
                    }
                    else if (et < holdPhase)
                    {
                        // stand at attention — barely move, look intimidating
                        foreach (var cell in _cubeMazeCells)
                        {
                            cell.Mesh.Position = Vector3.Lerp(cell.Mesh.Position, cell.EffectTargetPos, delta * 8f);
                            cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                        }
                    }
                    else
                    {
                        // disband, go home
                        float t = (et - holdPhase) / (_mazeEffectDuration - holdPhase);
                        foreach (var cell in _cubeMazeCells)
                        {
                            cell.Mesh.Position = Vector3.Lerp(cell.Mesh.Position, cell.HomePos, t * delta * 5f);
                            cell.Mesh.Color = Vector4.Lerp(cell.Mesh.Color, new Vector4(1f, 1f, 1f, 0.7f), delta * 2f);
                            cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                        }
                    }
                }
                else if (_currentMazeEffect == 5) // WAVE DANCE
                {
                    if (et < _mazeEffectDuration - 1.0f)
                    {
                        foreach (var cell in _cubeMazeCells)
                        {
                            float wave = MathF.Sin(et * 3f + cell.HomePos.X * 0.8f + cell.HomePos.Z * 0.5f + cell.EffectPhase);
                            var target = new Vector3(cell.HomePos.X, wave * 1.2f, cell.HomePos.Z);
                            cell.Mesh.Position = Vector3.Lerp(cell.Mesh.Position, target, delta * 6f);
                            // color pulse along the wave
                            float hue = (cell.HomePos.X + cell.HomePos.Z + et) / 12f;
                            cell.Mesh.Color = new Vector4(
                                0.5f + 0.5f * MathF.Sin(hue * MathF.PI * 2f),
                                0.5f + 0.5f * MathF.Sin(hue * MathF.PI * 2f + 2.09f),
                                0.5f + 0.5f * MathF.Sin(hue * MathF.PI * 2f + 4.19f),
                                1f);
                            cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                        }
                    }
                    else
                    {
                        float t = (et - (_mazeEffectDuration - 1.0f));
                        foreach (var cell in _cubeMazeCells)
                        {
                            cell.Mesh.Position = Vector3.Lerp(cell.Mesh.Position, cell.HomePos, t * delta * 6f);
                            cell.Mesh.Color = Vector4.Lerp(cell.Mesh.Color, new Vector4(1f, 1f, 1f, 0.7f), delta * 3f);
                            cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                        }
                    }
                }
                else if (_currentMazeEffect == 6) // BLACK HOLE
                {
                    var center = new Vector3(4.5f, 0f, 4.5f);
                    float pullPhase = 1.8f;
                    float novaPhase = pullPhase + 0.4f;
                    if (et < pullPhase)
                    {
                        // spiral inward with increasing speed
                        float t = et / pullPhase;
                        float pullStrength = 3f + t * 12f;
                        foreach (var cell in _cubeMazeCells)
                        {
                            var toCenter = center - cell.Mesh.Position;
                            float dist = toCenter.Length() + 0.1f;
                            // spiral component: add tangential velocity
                            var tangent = new Vector3(-toCenter.Z, 0f, toCenter.X) / dist;
                            cell.Velocity += Vector3.Normalize(toCenter) * pullStrength * delta
                                           + tangent * pullStrength * 0.4f * delta;
                            cell.Velocity *= (1f - delta * 0.3f);
                            cell.Mesh.Position += cell.Velocity * delta;
                            // event horizon color shift
                            cell.Mesh.Color = Vector4.Lerp(cell.Mesh.Color, new Vector4(0.6f, 0f, 1f, 1f), t * delta * 4f);
                            cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                        }
                    }
                    else if (et < novaPhase)
                    {
                        // nova burst
                        foreach (var cell in _cubeMazeCells)
                        {
                            if (MathF.Abs(cell.ExplodeVel.X) < 0.01f)
                            {
                                var dir = Vector3.Normalize(cell.Mesh.Position - center + new Vector3(
                                    (float)(rand.NextDouble() - 0.5f), 0.5f, (float)(rand.NextDouble() - 0.5f)));
                                cell.ExplodeVel = dir * (8f + (float)rand.NextDouble() * 6f);
                            }
                            cell.Mesh.Position += cell.ExplodeVel * delta;
                            cell.ExplodeVel *= (1f - delta * 1.5f);
                            cell.Mesh.Color = new Vector4(1f, 0.8f, 0f, 1f); // nova gold
                            cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                        }
                    }
                    else
                    {
                        float t = (et - novaPhase) / (_mazeEffectDuration - novaPhase);
                        foreach (var cell in _cubeMazeCells)
                        {
                            cell.Mesh.Position = Vector3.Lerp(cell.Mesh.Position, cell.HomePos, t * delta * 5f);
                            cell.Velocity = Vector3.Zero;
                            cell.Mesh.Color = Vector4.Lerp(cell.Mesh.Color, new Vector4(1f, 1f, 1f, 0.7f), delta * 2f);
                            cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                        }
                    }
                }
                else if (_currentMazeEffect == 7) // SNAKE PARADE
                {
                    float formPhase = 2f;
                    float slitherPhase = formPhase + 2f;
                    if (et < formPhase)
                    {
                        float t = et / formPhase;
                        int cellCount = _cubeMazeCells.Count;
                        for (int i = 0; i < cellCount; i++)
                        {
                            var cell = _cubeMazeCells[i];
                            // stagger entry: each cube joins the snake slightly after the last
                            float delay = (float)i / _cubeMazeCells.Count * formPhase * 0.7f;
                            float localT = Math.Max(0f, (et - delay) / (formPhase - delay));
                            cell.Mesh.Position = Vector3.Lerp(cell.Mesh.Position, cell.EffectTargetPos, localT * delta * 5f);
                            cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                        }
                    }
                    else if (et < slitherPhase)
                    {
                        // the snake slithers: offset each cube's Y with a wave
                        float st = et - formPhase;
                        int cellCount = _cubeMazeCells.Count;
                        for (int i = 0; i < cellCount; i++)
                        {
                            var cell = _cubeMazeCells[i];
                            float phase = (float)i / _cubeMazeCells.Count * MathF.PI * 2f;
                            var slitherTarget = new Vector3(
                                cell.EffectTargetPos.X,
                                MathF.Sin(st * 4f + phase) * 0.5f,
                                cell.EffectTargetPos.Z);
                            cell.Mesh.Position = Vector3.Lerp(cell.Mesh.Position, slitherTarget, delta * 8f);
                            cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                        }
                    }
                    else
                    {
                        // dizziness sets in — everybody go home
                        float t = (et - slitherPhase) / (_mazeEffectDuration - slitherPhase);
                        foreach (var cell in _cubeMazeCells)
                        {
                            cell.Mesh.Position = Vector3.Lerp(cell.Mesh.Position, cell.HomePos, t * delta * 5f);
                            cell.Mesh.Color = Vector4.Lerp(cell.Mesh.Color, new Vector4(1f, 1f, 1f, 0.7f), delta * 2f);
                            cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                        }
                    }
                }

                // ---- Effect cleanup ----
                if (effectDone)
                {
                    // hard-snap all non-void cubes home; remove void cubes
                    // Replace with:
                    _mazeRemoveBuffer.Clear();
                    var toRemove = _mazeRemoveBuffer;
                    foreach (var cell in _cubeMazeCells)
                    {
                        if (cell.IsVoid)
                        {
                            Scene3.RemoveMesh(cell.Mesh);
                            toRemove.Add(cell);
                        }
                        else
                        {
                            cell.Mesh.Position = cell.HomePos;
                            cell.Mesh.Rotation = Vector3.Zero;
                            cell.Mesh.Size = new Vector3(0.5f, 0.5f, 0.5f);
                            cell.Mesh.Color = new Vector4(1f, 1f, 1f, 0.7f);
                            cell.Velocity = Vector3.Zero;
                            cell.ExplodeVel = Vector3.Zero;
                            cell.Mesh.TransformDirty =
    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.00001f ||
    cell.ExplodeVel.LengthSquared() > 0.00001f ||
    cell.Velocity.LengthSquared() > 0.00001f;
                        }
                    }
                    foreach (var v in toRemove) _cubeMazeCells.Remove(v);

                    // re-snapshot after cleanup
                    if (_effectHomeSnapshot.Length != _cubeMazeCells.Count)
                        _effectHomeSnapshot = new Vector3[_cubeMazeCells.Count];

                    _mazeEffectActive = false;
                }

                return; // don't run idle bumps during an effect
            }

            // ---- Idle bump logic (runs when no effect is active) ----
            for (int i = 0; i < _cubeMazeCells.Count; i++)
            {
                var cell = _cubeMazeCells[i];

                cell.Mesh.Position += cell.Velocity * delta;

                var offset = cell.HomePos - cell.Mesh.Position;
                cell.Velocity += offset * 0.8f * delta;
                cell.Velocity *= (1f - delta * 0.4f);

                cell.BumpTimer -= delta;
                if (cell.BumpDuration > 0f)
                {
                    cell.Mesh.Position += cell.BumpVelocity * delta;
                    cell.BumpVelocity *= (1f - delta * 8f);
                    cell.BumpDuration -= delta;
                }

                if (cell.BumpTimer <= 0f)
                {
                    cell.BumpTimer = 1.5f + (float)rand.NextDouble() * 4f;

                    int targetIdx = rand.Next(_cubeMazeCells.Count);
                    if (targetIdx != i)
                    {
                        var target = _cubeMazeCells[targetIdx];
                        var bumpDir = Vector3.Normalize(target.HomePos - cell.HomePos + Vector3.UnitY * 0.1f);
                        float bumpForce = 1.5f + (float)rand.NextDouble() * 2f;

                        cell.BumpVelocity = bumpDir * bumpForce;
                        cell.BumpDuration = 0.3f;
                        target.BumpVelocity = bumpDir * -bumpForce * 0.7f;
                        target.BumpDuration = 0.25f;
                        target.Mesh.Color = new Vector4(1f, 0.3f, 0f, 0.9f);
                        target.Bumped = true;
                    }
                }

                if (cell.Bumped)
                {
                    var c = cell.Mesh.Color;
                    cell.Mesh.Color = new Vector4(
                        Math.Max(0f, c.X - delta * 1.5f),
                        Math.Max(0f, c.Y - delta * 0.5f),
                        Math.Min(1f, c.Z + delta * 1.5f),
                        c.W);
                }

                // Replace with:
                if (cell.BumpDuration > 0f ||
                    cell.Velocity.LengthSquared() > 0.0001f ||
                    Vector3.DistanceSquared(cell.Mesh.Position, cell.HomePos) > 0.0001f)
                {
                    cell.Mesh.TransformDirty = true;
                }
            }
        }



    }
}
