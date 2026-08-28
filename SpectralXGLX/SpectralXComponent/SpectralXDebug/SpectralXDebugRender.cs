using Microsoft.AspNetCore.Components;
using System.Text;

namespace SpectralXGLX.SpectralXComponent.SpectralXDebug
{
    public class SpectralXDebugRender
    {
        private readonly SpectralXEngine engine;
        private readonly SpectralXViewport viewport;
        private readonly SpectralXInput input;
        private readonly SpectralXCamera camera;
        private readonly SpectralXIsoCamera isoCamera;
        private readonly SpectralXMeshLibrary meshLibrary;

        public bool Enabled { get; set; } = false;

        public SpectralXDebugRender(
            SpectralXEngine engine,
            SpectralXViewport viewport,
            SpectralXInput input,
            SpectralXCamera camera,
         //   SpectralXIsoCamera IsoCamera,
            SpectralXMeshLibrary meshLibrary)
        {
            this.engine = engine;
            this.viewport = viewport;
            this.input = input;
            this.camera = camera;
        //    this.isoCamera = IsoCamera;
            this.meshLibrary = meshLibrary;
        }

        public MarkupString Output
        {
            get
            {
                if (!Enabled)
                    return new MarkupString(string.Empty);

                var sb = new StringBuilder();

                // ?? Left panel — perf, meshes, camera ????????????????????????????????
                sb.AppendLine("<div style='position:absolute;top:1%;left:1%;width:14%;height:76%;z-index:9000;");
                sb.AppendLine("overflow:auto;padding:8px;background:rgba(0,0,0,0.85);");
                sb.AppendLine("color:lime;font-family:monospace;font-size:11px;border:1px solid rgba(0,255,0,0.2);border-radius:4px;'>");

                sb.AppendLine("<b>PERFORMANCE</b><br/>");
                sb.AppendLine($"FPS: {engine.Performance.CurrentFPS:F1}<br/>");
                sb.AppendLine($"Avg: {engine.Performance.AverageFPS:F1}<br/>");
                sb.AppendLine($"Min: {engine.Performance.MinFPS:F1}<br/>");
                sb.AppendLine($"Max: {engine.Performance.MaxFPS:F1}<br/>");
                sb.AppendLine($"Frame: {engine.Performance.FrameTimeMs:F2} ms<br/>");
                sb.AppendLine($"Viewport: {viewport.ViewportWidth} x {viewport.ViewportHeight}<br/>");

            

                sb.AppendLine("<hr style='border-color:rgba(0,255,0,0.3);'/>");
                sb.AppendLine("<b>METRICS</b><br/>");
                foreach (var metric in engine.Performance.GetMetrics().Values)
                {
                    sb.AppendLine(
                        $"{metric.Name}: {metric.AverageMs:F2}ms " +
                        $"({metric.PercentOfFrame:F1}%)<br/>");
                }

                sb.AppendLine("<hr style='border-color:rgba(0,255,0,0.3);'/>");
                sb.AppendLine("<b>CAMERA</b><br/>");
                sb.AppendLine($"Pos: {camera.Position}<br/>");
                sb.AppendLine($"Fwd Y+ {camera.Forward}<br/>");
                sb.AppendLine($"Rt X+: {camera.Right}<br/>");
                sb.AppendLine($"Up Z+: {camera.Up}<br/>");

                sb.AppendLine("<hr style='border-color:rgba(0,255,0,0.3);'/>");
                sb.AppendLine("<b>OrthoCAMERA</b><br/>");
                sb.AppendLine($"Pos: {engine.OrthoCamera.Position}<br/>");
           
                // Add after isoCAMERA block, before the </div>

                sb.AppendLine("<hr style='border-color:rgba(0,255,0,0.3);'/>");
                sb.AppendLine("<b>CUBE CITY</b><br/>");
                sb.AppendLine($"Phase: {engine.CityPhaseDebug}<br/>");
                sb.AppendLine($"Active: {engine.CityActiveCount}<br/>");
                sb.AppendLine($"Packed: {engine.CityPackedCount}<br/>");
                sb.AppendLine($"Drawn: {engine.CityDrawn}<br/>");
                sb.AppendLine("<span style='color:yellow;'>-- cull --</span><br/>");
                sb.AppendLine($"Frustum: {engine.CityCulledFrustum}<br/>");
                sb.AppendLine($"Occlusion: {engine.CityCulledOcclusion}<br/>");
                sb.AppendLine($"Distance: {engine.CityCulledDistance}<br/>");
                sb.AppendLine("</div>");          // ← ONE close here at the end

                // ?? Bottom-right panel — input, combos, debug messages ???????????
                sb.AppendLine("<div style='position:absolute;bottom:1%;right:6%;width:14%;height:22%;z-index:9000;");
                sb.AppendLine("overflow:auto;padding:8px;background:rgba(0,0,0,0.85);");
                sb.AppendLine("color:white;font-family:monospace;font-size:11px;border:1px solid rgba(255,165,0,0.3);border-radius:4px;'>");

                sb.AppendLine("<b style='color:orange;'>INPUT</b><br/>");
                foreach (var key in input.DebugRecentKeys)
                {
                    bool active = input.DebugActiveSequences().Any(seq => seq.Contains(key));
                    sb.AppendLine(
                        $"<span style='display:inline-block;margin:1px;padding:1px 4px;" +
                        $"border-radius:3px;background:{(active ? "lime" : "#444")};" +
                        $"color:white;font-size:10px;'>{key}</span>");
                }

                sb.AppendLine("<br/><b style='color:orange;'>SEQUENCES</b><br/>");
                foreach (var seq in input.DebugActiveSequences())
                {
                    sb.AppendLine(
                        $"<span style='display:inline-block;margin:1px;padding:1px 4px;" +
                        $"border-radius:3px;background:orange;font-size:10px;'>{seq}</span>");
                }

                sb.AppendLine("<br/><b style='color:orange;'>COMBO</b>");
                sb.AppendLine("<div style='width:100%;height:6px;background:#222;border-radius:3px;margin:3px 0;'>");
                sb.AppendLine(
                    $"<div style='height:100%;width:{input.GetComboProgressPercent()}%;" +
                    "background:lime;border-radius:3px;'></div>");
                sb.AppendLine("</div>");

                sb.AppendLine("<b style='color:orange;'>MESSAGES</b><br/>");
                foreach (var msg in input.DebugMessages.TakeLast(5))
                {
                    sb.AppendLine($"<div style='font-size:10px;'>{msg}</div>");
                }

                sb.AppendLine("</div>");

                return new MarkupString(sb.ToString());
            }
        }
    }
}