using Microsoft.AspNetCore.Components.Web;

namespace DCLibrary.Death
{
    public class DeathInputController
    {
        private readonly DeathCubeRenderer cubeRenderer;
        private readonly DeathCubeState cubeState;

        public DeathInputController(DeathCubeRenderer renderer, DeathCubeState state)
        {
            cubeRenderer = renderer;
            cubeState = state;
        }

        private bool isRightMouseDown = false;
        private bool isLeftMouseDown = false;

        private int lastMouseX;
        private int lastMouseY;

        // Which face is currently grabbed for rotation
        private int grabbedFaceIndex;
        private int selectedFaceIndex = 0; // default front

        public void SelectFrontFace()
        {
            selectedFaceIndex = 0; // front (blue)
        }

        public void SelectRightFace()
        {
            selectedFaceIndex = 2; // right (red)
        }
        public void SelectBackFace()
        {
            selectedFaceIndex = 1; // back (green)
        }
        public void SelectLeftFace()
        {
            selectedFaceIndex = 3; // left (orange)
        }
        public void SelectTopFace()
        {
            selectedFaceIndex = 4; // top (white)
        }
        public void SelectBottomFace()
        {
            selectedFaceIndex = 5; // bottom (yellow)
        }

        public int GetSelectedFace()
        {
            return selectedFaceIndex;
        }

        public void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == 0)
            {
                grabbedFaceIndex = selectedFaceIndex;
                isLeftMouseDown = true;
                lastMouseX = (int)e.ClientX;
                lastMouseY = (int)e.ClientY;
            }
            else if (e.Button == 2)
            {
                isRightMouseDown = true;
                lastMouseX = (int)e.ClientX;
                lastMouseY = (int)e.ClientY;
                cubeRenderer.StopSpin();
            }
        }

        private DateTime lastRotateTime = DateTime.MinValue;

        public void OnMouseMove(MouseEventArgs e)
        {
            if (isLeftMouseDown)
            {
                int dx = (int)e.ClientX - lastMouseX;
                int dy = (int)e.ClientY - lastMouseY;

                lastMouseX = (int)e.ClientX;
                lastMouseY = (int)e.ClientY;

                if (Math.Abs(dx) > 10)
                {
                    // throttle: only rotate if enough time has passed
                    if ((DateTime.Now - lastRotateTime).TotalMilliseconds > 150)
                    {
                        bool clockwise = dx > 0;
                        cubeState.RotateFace(grabbedFaceIndex, clockwise);
                        lastRotateTime = DateTime.Now;
                    }
                }
            }
            else if (isRightMouseDown)
            {
                double dx = e.ClientX - lastMouseX;
                double dy = e.ClientY - lastMouseY;
                lastMouseX = (int)e.ClientX;
                lastMouseY = (int)e.ClientY;

                cubeRenderer.cubeAngle = (cubeRenderer.cubeAngle + dx * 0.5 - dy * 0.5) % 360;
            }
        }

        public void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == 0) isLeftMouseDown = false;
            if (e.Button == 2) isRightMouseDown = false;
        }
    }
}
