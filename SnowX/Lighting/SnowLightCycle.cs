namespace SnowX.Lighting
{
    public class SnowLightCycle
    {
        // Internal state
        private DateTime _lastUpdateTime = DateTime.Now;
        private double _accumulatedSeconds = 0;
        private double _pausedHour = 0;

        // Public properties
        public bool IsPaused { get; set; } = false;
        public int CycleDurationMinutes { get; set; } = 2; // 2-1440 minutes (2 min to 24 hours)
        public double CurrentHour { get; private set; } = 12; // Start at noon

        // Pause/Resume methods
        public void Pause() => IsPaused = true;
        public void Resume() => IsPaused = false;
        public void TogglePause() => IsPaused = !IsPaused;

        // Web-friendly color as CSS rgba string
        public string OverlayColor { get; private set; } = "rgba(0, 0, 0, 0)";
        public double Intensity { get; private set; } = 0.0; // 0.0 = day, 1.0 = night

        // Deterministic update - call this in your GameTick
        public void Update()
        {
            double cycleSeconds = CycleDurationMinutes * 60.0;
            var now = DateTime.Now;
            var delta = (now - _lastUpdateTime).TotalSeconds;
            _lastUpdateTime = now;

            if (!IsPaused)
                _accumulatedSeconds = (_accumulatedSeconds + delta) % cycleSeconds;

            double hour = IsPaused
                ? _pausedHour
                : (_accumulatedSeconds / cycleSeconds) * 24.0;

            if (!IsPaused)
                _pausedHour = hour;

            CurrentHour = hour;

            // Compute overlay color and intensity
            (OverlayColor, Intensity) = GetOverlayForHour(hour);
        }

        private (string, double) GetOverlayForHour(double hour)
        {
            // Night: 0-6, 20-24 (dark overlay)
            // Day: 8-18 (minimal overlay)
            // Dawn: 6-8 (transition)
            // Dusk: 18-20 (transition)

            if (hour >= 0 && hour < 6)
            {
                // Deep night (midnight to early morning)
                double intensity = 0.7; // Dark
                return ($"rgba(10, 15, 40, {intensity})", intensity);
            }
            else if (hour >= 6 && hour < 8)
            {
                // Dawn (6am to 8am) - transition from night to day
                double t = (hour - 6) / 2.0;
                double intensity = Lerp(0.7, 0.0, t);
                int r = (int)Lerp(10, 255, t);
                int g = (int)Lerp(15, 200, t);
                int b = (int)Lerp(40, 150, t);
                return ($"rgba({r}, {g}, {b}, {intensity:F2})", intensity);
            }
            else if (hour >= 8 && hour < 18)
            {
                // Full daylight
                return ("rgba(255, 255, 255, 0)", 0.0);
            }
            else if (hour >= 18 && hour < 20)
            {
                // Dusk (6pm to 8pm) - transition from day to night
                double t = (hour - 18) / 2.0;
                double intensity = Lerp(0.0, 0.7, t);
                int r = (int)Lerp(255, 30, t);
                int g = (int)Lerp(200, 20, t);
                int b = (int)Lerp(150, 60, t);
                return ($"rgba({r}, {g}, {b}, {intensity:F2})", intensity);
            }
            else // 20-24
            {
                // Night
                double intensity = 0.7;
                return ($"rgba(10, 15, 40, {intensity})", intensity);
            }
        }

        private double Lerp(double start, double end, double t)
        {
            return start + (end - start) * Math.Clamp(t, 0, 1);
        }

        // Helper to get time of day as string for debug
        public string GetTimeOfDayString()
        {
            int hours = (int)CurrentHour;
            int minutes = (int)((CurrentHour - hours) * 60);
            string period = hours >= 12 ? "PM" : "AM";
            int displayHours = hours > 12 ? hours - 12 : (hours == 0 ? 12 : hours);
            return $"{displayHours:D2}:{minutes:D2} {period}";
        }

        // Helper to get phase name
        public string GetPhase()
        {
            if (CurrentHour >= 0 && CurrentHour < 6) return "Night";
            if (CurrentHour >= 6 && CurrentHour < 8) return "Dawn";
            if (CurrentHour >= 8 && CurrentHour < 18) return "Day";
            if (CurrentHour >= 18 && CurrentHour < 20) return "Dusk";
            return "Night";
        }
    }
}