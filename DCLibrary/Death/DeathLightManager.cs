namespace DCLibrary.Death
{
    public class DeathLightManager
    {
        public bool LightOn { get; private set; } = false;
        public int LightX { get; private set; } = 400;
        public int LightY { get; private set; } = 300;
        public double LightRadius { get; private set; } = 200;

        public void ToggleLight() => LightOn = !LightOn;

        public void MoveLight(int dx, int dy)
        {
            LightX += dx;
            LightY += dy;
        }

        public string RenderLight()
        {
            if (!LightOn) return string.Empty;

            return $@"
            <div style='
                width:{LightRadius * 2}px;
                height:{LightRadius * 2}px;
                background:radial-gradient(circle,
                    rgba(255,255,0,0.8) 0%,
                    rgba(255,255,0,0.4) 40%,
                    rgba(0,0,0,0.0) 100%);
                border-radius:50%;
                position:absolute;
                top:{LightY - LightRadius}px;
                left:{LightX - LightRadius}px;
                filter:blur(40px);
                pointer-events:none;'>
            </div>
            <div style='
                width:30px;
                height:30px;
                background:radial-gradient(circle, white, yellow);
                border-radius:50%;
                position:absolute;
                top:{LightY}px;
                left:{LightX}px;
                box-shadow:0 0 40px yellow;'>
            </div>";
        }

        public string CalculateShade(int cubeX, int cubeY)
        {
            double dx = LightX - cubeX;
            double dy = LightY - cubeY;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            double intensity = 0;
            if (LightOn && dist < LightRadius)
                intensity = 1.0 - (dist / LightRadius);

            return $"rgba(255,255,255,{intensity * 0.6})";
        }

        public void Clear()
        {
            LightOn = false;
            LightX = 400;
            LightY = 300;
        }
    }

   

}
