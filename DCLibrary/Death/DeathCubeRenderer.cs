using System.Text;
using System.Diagnostics;

namespace DCLibrary.Death
{
    public class DeathCubeRenderer
    {
        private readonly DeathCubeState cubeState;

        public DeathCubeRenderer(DeathCubeState state)
        {
            cubeState = state;
        }

        public double cubeAngle = 0;
        public System.Timers.Timer? cubeTimer;
        public readonly Stopwatch stopwatch = new();

        // --- Rendering ---
        public string RenderCube(DeathLightManager lightManager, int cubeSize)
        {
            var sb = new StringBuilder();
            int half = cubeSize / 2; // half depth for translateZ

            sb.Append($@"<div style='width:{cubeSize}px; height:{cubeSize}px; position:absolute;
       top:{300 - half}px; left:{400 - half}px;
       transform-style:preserve-3d;
       transform:rotateY({cubeAngle}deg) rotateX({cubeAngle / 2}deg);'>");

            sb.Append(MakeFace(0, $"translateZ({half}px)", lightManager, cubeSize));                       // front
            sb.Append(MakeFace(1, $"rotateY(180deg) translateZ({half}px)", lightManager, cubeSize));       // back
            sb.Append(MakeFace(2, $"rotateY(90deg) translateZ({half}px)", lightManager, cubeSize));        // right
            sb.Append(MakeFace(3, $"rotateY(-90deg) translateZ({half}px)", lightManager, cubeSize));       // left
            sb.Append(MakeFace(4, $"rotateX(90deg) translateZ({half}px)", lightManager, cubeSize));        // top
            sb.Append(MakeFace(5, $"rotateX(-90deg) translateZ({half}px)", lightManager, cubeSize));       // bottom

            sb.Append("</div>");
            return sb.ToString();
        }

        private string MakeFace(int faceIndex, string transform, DeathLightManager lightManager, int cubeSize)
        {
            var sb = new StringBuilder();

            // Highlight glow
            string highlight = (faceIndex == cubeState.HighlightedFaceIndex)
                ? $"box-shadow:0 0 10px 5px {cubeState.HighlightColor};"
                : "box-shadow:inset 0 0 5px rgba(255,255,255,0.6);";

            sb.Append($"<div style='position:absolute;width:{cubeSize}px;height:{cubeSize}px;transform:{transform};'>");

            int tileSize = cubeSize / 3;
            for (int i = 0; i < 9; i++)
            {
                int row = i / 3;
                int col = i % 3;
                string baseColor = cubeState.faceColors[faceIndex, i];

                // Calculate shade per tile (preserves lighting effect)
                string shade = lightManager.CalculateShade(col * tileSize, row * tileSize);

                sb.Append($@"<div style='position:absolute;width:{tileSize}px;height:{tileSize}px;
          top:{row * tileSize}px;left:{col * tileSize}px;
          background:linear-gradient({shade},{baseColor});
          border:1px solid black;
          {highlight}'></div>");
            }

            sb.Append("</div>");
            return sb.ToString();
        }



        // --- Spin Logic ---
        public void StartSpin(Action updateScene)
        {
            stopwatch.Restart();
            cubeTimer = new System.Timers.Timer(16);
            cubeTimer.Elapsed += (s, e) =>
            {
                var elapsed = stopwatch.Elapsed.TotalSeconds;
                stopwatch.Restart();
                cubeAngle = (cubeAngle + 60 * elapsed) % 360;
                updateScene();
            };
            cubeTimer.Start();
        }

        public void ToggleSpinMode(Action updateScene)
        {
            if (cubeTimer != null)
            {
                cubeTimer.Stop();
                cubeTimer.Dispose();
                cubeTimer = null;
            }
            else
            {
                StartSpin(updateScene);
            }
        }

        public void StopSpin()
        {
            cubeTimer?.Stop();
            cubeTimer?.Dispose();
            cubeTimer = null;
        }

        public void Clear()
        {
            cubeTimer?.Stop();
            cubeTimer?.Dispose();
            cubeTimer = null;
            cubeAngle = 0;
        }
    }
}
