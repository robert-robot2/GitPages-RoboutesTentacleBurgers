namespace SpectralXGLX.SpectralXComponent
{
    public class SpectralXViewport
    {
        public int ViewportWidth { get; private set; } = 1024;
        public int ViewportHeight { get; private set; } = 768;
        public string BackgroundColor { get; set; } = "black";
        public bool DynamicSize { get; set; } = false; // Home page sets this true

        public string Style =>
            DynamicSize
                ? $"position:relative;background-color:{BackgroundColor};overflow:hidden;transition:all 0.4s ease-in-out;"
                : $"position:relative;width:{ViewportWidth}px;height:{ViewportHeight}px;background-color:{BackgroundColor};overflow:hidden;transition:all 0.4s ease-in-out;";

        public void SetSize(int width, int height)
        {
            if (width <= 0 || height <= 0) return;
            ViewportWidth = width;
            ViewportHeight = height;
        }
    }
}
