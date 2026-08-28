namespace DCLibrary.Death
{
    public class DeathCubeState
    {



        public int HighlightedFaceIndex { get; private set; } = -1;
        public string HighlightColor { get; private set; } = "blue";

        public void SetHighlightedFace(int faceIndex, string color)
        {
            HighlightedFaceIndex = faceIndex;
            HighlightColor = color;
        }


        // 6 faces × 9 stickers each
        // 0: Front, 1: Back, 2: Right, 3: Left, 4: Top, 5: Bottom
        public string[,] faceColors = new string[6, 9]
        {
            { "blue","blue","blue","blue","blue","blue","blue","blue","blue" },     // front
            { "green","green","green","green","green","green","green","green","green" }, // back
            { "red","red","red","red","red","red","red","red","red" },              // right
            { "orange","orange","orange","orange","orange","orange","orange","orange","orange" }, // left
            { "white","white","white","white","white","white","white","white","white" }, // top
            { "yellow","yellow","yellow","yellow","yellow","yellow","yellow","yellow","yellow" }  // bottom
        };

        // Rotates the 3x3 grid of the given face and updates adjacent edge strips
        public int MoveCount { get; set; } = 0;
        public List<string> MoveLog { get; private set; } = new List<string>();

        public void RotateFace(int faceIndex, bool clockwise)
        {
            RotateFaceSelf(faceIndex, clockwise);

            // Track move
            MoveCount++;
            string direction = clockwise ? "CW" : "CCW";
            MoveLog.Add($"Face {faceIndex} rotated {direction}");

            if (faceIndex == 0)
            {
                if (clockwise) RotateFrontEdgesClockwise();
                else RotateFrontEdgesCounterClockwise();
            }
            else if (faceIndex == 2)
            {
                if (clockwise) RotateRightEdgesClockwise();
                else RotateRightEdgesCounterClockwise();
            }
            else if (faceIndex == 1)
            {
                if (clockwise) RotateBackEdgesClockwise();
                else RotateBackEdgesCounterClockwise();
            }
            else if (faceIndex == 3)
            {
                if (clockwise) RotateLeftEdgesClockwise();
                else RotateLeftEdgesCounterClockwise();
            }
            else if (faceIndex == 4)
            {
                if (clockwise) RotateTopEdgesClockwise();
                else RotateTopEdgesCounterClockwise();
            }
            else if (faceIndex == 5)
            {
                if (clockwise) RotateBottomEdgesClockwise();
                else RotateBottomEdgesCounterClockwise();
            }
        }



        private void RotateFaceSelf(int faceIndex, bool clockwise)
        {
            string[] t = new string[9];
            for (int i = 0; i < 9; i++) t[i] = faceColors[faceIndex, i];

            if (clockwise)
            {
                faceColors[faceIndex, 0] = t[6];
                faceColors[faceIndex, 1] = t[3];
                faceColors[faceIndex, 2] = t[0];
                faceColors[faceIndex, 3] = t[7];
                faceColors[faceIndex, 4] = t[4];
                faceColors[faceIndex, 5] = t[1];
                faceColors[faceIndex, 6] = t[8];
                faceColors[faceIndex, 7] = t[5];
                faceColors[faceIndex, 8] = t[2];
            }
            else
            {
                faceColors[faceIndex, 0] = t[2];
                faceColors[faceIndex, 1] = t[5];
                faceColors[faceIndex, 2] = t[8];
                faceColors[faceIndex, 3] = t[1];
                faceColors[faceIndex, 4] = t[4];
                faceColors[faceIndex, 5] = t[7];
                faceColors[faceIndex, 6] = t[0];
                faceColors[faceIndex, 7] = t[3];
                faceColors[faceIndex, 8] = t[6];
            }
        }

        // --- Front face edge rotations ---
        private void RotateFrontEdgesClockwise()
        {
            var U = new[] { faceColors[4, 6], faceColors[4, 7], faceColors[4, 8] };
            var R = new[] { faceColors[2, 0], faceColors[2, 3], faceColors[2, 6] };
            var D = new[] { faceColors[5, 0], faceColors[5, 1], faceColors[5, 2] };
            var L = new[] { faceColors[3, 2], faceColors[3, 5], faceColors[3, 8] };

            faceColors[2, 0] = U[2]; faceColors[2, 3] = U[1]; faceColors[2, 6] = U[0];
            faceColors[5, 0] = R[0]; faceColors[5, 1] = R[1]; faceColors[5, 2] = R[2];
            faceColors[3, 2] = D[2]; faceColors[3, 5] = D[1]; faceColors[3, 8] = D[0];
            faceColors[4, 6] = L[0]; faceColors[4, 7] = L[1]; faceColors[4, 8] = L[2];
        }

        private void RotateFrontEdgesCounterClockwise()
        {
            var U = new[] { faceColors[4, 6], faceColors[4, 7], faceColors[4, 8] };
            var R = new[] { faceColors[2, 0], faceColors[2, 3], faceColors[2, 6] };
            var D = new[] { faceColors[5, 0], faceColors[5, 1], faceColors[5, 2] };
            var L = new[] { faceColors[3, 2], faceColors[3, 5], faceColors[3, 8] };

            faceColors[4, 6] = R[0]; faceColors[4, 7] = R[1]; faceColors[4, 8] = R[2];
            faceColors[2, 0] = D[2]; faceColors[2, 3] = D[1]; faceColors[2, 6] = D[0];
            faceColors[5, 0] = L[0]; faceColors[5, 1] = L[1]; faceColors[5, 2] = L[2];
            faceColors[3, 2] = U[2]; faceColors[3, 5] = U[1]; faceColors[3, 8] = U[0];
        }

        // --- Right face edge rotations ---
        private void RotateRightEdgesClockwise()
        {
            var U = new[] { faceColors[4, 2], faceColors[4, 5], faceColors[4, 8] };
            var F = new[] { faceColors[0, 2], faceColors[0, 5], faceColors[0, 8] };
            var D = new[] { faceColors[5, 2], faceColors[5, 5], faceColors[5, 8] };
            var B = new[] { faceColors[1, 0], faceColors[1, 3], faceColors[1, 6] };

            faceColors[0, 2] = U[0]; faceColors[0, 5] = U[1]; faceColors[0, 8] = U[2];
            faceColors[5, 2] = F[0]; faceColors[5, 5] = F[1]; faceColors[5, 8] = F[2];
            faceColors[1, 0] = D[2]; faceColors[1, 3] = D[1]; faceColors[1, 6] = D[0];
            faceColors[4, 2] = B[2]; faceColors[4, 5] = B[1]; faceColors[4, 8] = B[0];
        }

        private void RotateRightEdgesCounterClockwise()
        {
            var U = new[] { faceColors[4, 2], faceColors[4, 5], faceColors[4, 8] };
            var F = new[] { faceColors[0, 2], faceColors[0, 5], faceColors[0, 8] };
            var D = new[] { faceColors[5, 2], faceColors[5, 5], faceColors[5, 8] };
            var B = new[] { faceColors[1, 0], faceColors[1, 3], faceColors[1, 6] };

            faceColors[4, 2] = F[0]; faceColors[4, 5] = F[1]; faceColors[4, 8] = F[2];
            faceColors[0, 2] = D[0]; faceColors[0, 5] = D[1]; faceColors[0, 8] = D[2];
            faceColors[5, 2] = B[2]; faceColors[5, 5] = B[1]; faceColors[5, 8] = B[0];
            faceColors[1, 0] = U[2]; faceColors[1, 3] = U[1]; faceColors[1, 6] = U[0];
        }

        private void RotateBackEdgesClockwise()
        {
            // Capture strips
            var U = new[] { faceColors[4, 0], faceColors[4, 1], faceColors[4, 2] }; // Up top row
            var L = new[] { faceColors[3, 0], faceColors[3, 3], faceColors[3, 6] }; // Left col
            var D = new[] { faceColors[5, 6], faceColors[5, 7], faceColors[5, 8] }; // Down bottom row
            var R = new[] { faceColors[2, 2], faceColors[2, 5], faceColors[2, 8] }; // Right col

            // U -> R
            faceColors[2, 2] = U[0]; faceColors[2, 5] = U[1]; faceColors[2, 8] = U[2];
            // R -> D
            faceColors[5, 6] = R[0]; faceColors[5, 7] = R[1]; faceColors[5, 8] = R[2];
            // D -> L (reversed)
            faceColors[3, 0] = D[2]; faceColors[3, 3] = D[1]; faceColors[3, 6] = D[0];
            // L -> U (reversed)
            faceColors[4, 0] = L[2]; faceColors[4, 1] = L[1]; faceColors[4, 2] = L[0];
        }

        private void RotateBackEdgesCounterClockwise()
        {
            var U = new[] { faceColors[4, 0], faceColors[4, 1], faceColors[4, 2] };
            var L = new[] { faceColors[3, 0], faceColors[3, 3], faceColors[3, 6] };
            var D = new[] { faceColors[5, 6], faceColors[5, 7], faceColors[5, 8] };
            var R = new[] { faceColors[2, 2], faceColors[2, 5], faceColors[2, 8] };

            // U <- R
            faceColors[4, 0] = R[0]; faceColors[4, 1] = R[1]; faceColors[4, 2] = R[2];
            // R <- D
            faceColors[2, 2] = D[0]; faceColors[2, 5] = D[1]; faceColors[2, 8] = D[2];
            // D <- L (reversed)
            faceColors[5, 6] = L[2]; faceColors[5, 7] = L[1]; faceColors[5, 8] = L[0];
            // L <- U (reversed)
            faceColors[3, 0] = U[2]; faceColors[3, 3] = U[1]; faceColors[3, 6] = U[0];
        }

        private void RotateLeftEdgesClockwise()
        {
            var U = new[] { faceColors[4, 0], faceColors[4, 3], faceColors[4, 6] }; // Up left col
            var F = new[] { faceColors[0, 0], faceColors[0, 3], faceColors[0, 6] }; // Front left col
            var D = new[] { faceColors[5, 0], faceColors[5, 3], faceColors[5, 6] }; // Down left col
            var B = new[] { faceColors[1, 2], faceColors[1, 5], faceColors[1, 8] }; // Back right col

            // U -> F
            faceColors[0, 0] = U[0]; faceColors[0, 3] = U[1]; faceColors[0, 6] = U[2];
            // F -> D
            faceColors[5, 0] = F[0]; faceColors[5, 3] = F[1]; faceColors[5, 6] = F[2];
            // D -> B (reversed)
            faceColors[1, 2] = D[2]; faceColors[1, 5] = D[1]; faceColors[1, 8] = D[0];
            // B -> U (reversed)
            faceColors[4, 0] = B[2]; faceColors[4, 3] = B[1]; faceColors[4, 6] = B[0];
        }

        private void RotateLeftEdgesCounterClockwise()
        {
            var U = new[] { faceColors[4, 0], faceColors[4, 3], faceColors[4, 6] };
            var F = new[] { faceColors[0, 0], faceColors[0, 3], faceColors[0, 6] };
            var D = new[] { faceColors[5, 0], faceColors[5, 3], faceColors[5, 6] };
            var B = new[] { faceColors[1, 2], faceColors[1, 5], faceColors[1, 8] };

            // U <- F
            faceColors[4, 0] = F[0]; faceColors[4, 3] = F[1]; faceColors[4, 6] = F[2];
            // F <- D
            faceColors[0, 0] = D[0]; faceColors[0, 3] = D[1]; faceColors[0, 6] = D[2];
            // D <- B (reversed)
            faceColors[5, 0] = B[2]; faceColors[5, 3] = B[1]; faceColors[5, 6] = B[0];
            // B <- U (reversed)
            faceColors[1, 2] = U[2]; faceColors[1, 5] = U[1]; faceColors[1, 8] = U[0];
        }

        private void RotateTopEdgesClockwise()
        {
            var B = new[] { faceColors[1, 0], faceColors[1, 1], faceColors[1, 2] }; // Back top row
            var R = new[] { faceColors[2, 0], faceColors[2, 1], faceColors[2, 2] }; // Right top row
            var F = new[] { faceColors[0, 0], faceColors[0, 1], faceColors[0, 2] }; // Front top row
            var L = new[] { faceColors[3, 0], faceColors[3, 1], faceColors[3, 2] }; // Left top row

            // B -> R
            faceColors[2, 0] = B[0]; faceColors[2, 1] = B[1]; faceColors[2, 2] = B[2];
            // R -> F
            faceColors[0, 0] = R[0]; faceColors[0, 1] = R[1]; faceColors[0, 2] = R[2];
            // F -> L
            faceColors[3, 0] = F[0]; faceColors[3, 1] = F[1]; faceColors[3, 2] = F[2];
            // L -> B
            faceColors[1, 0] = L[0]; faceColors[1, 1] = L[1]; faceColors[1, 2] = L[2];
        }

        private void RotateTopEdgesCounterClockwise()
        {
            var B = new[] { faceColors[1, 0], faceColors[1, 1], faceColors[1, 2] };
            var R = new[] { faceColors[2, 0], faceColors[2, 1], faceColors[2, 2] };
            var F = new[] { faceColors[0, 0], faceColors[0, 1], faceColors[0, 2] };
            var L = new[] { faceColors[3, 0], faceColors[3, 1], faceColors[3, 2] };

            // B <- L
            faceColors[1, 0] = L[0]; faceColors[1, 1] = L[1]; faceColors[1, 2] = L[2];
            // L <- F
            faceColors[3, 0] = F[0]; faceColors[3, 1] = F[1]; faceColors[3, 2] = F[2];
            // F <- R
            faceColors[0, 0] = R[0]; faceColors[0, 1] = R[1]; faceColors[0, 2] = R[2];
            // R <- B
            faceColors[2, 0] = B[0]; faceColors[2, 1] = B[1]; faceColors[2, 2] = B[2];
        }


        private void RotateBottomEdgesClockwise()
        {
            var F = new[] { faceColors[0, 6], faceColors[0, 7], faceColors[0, 8] }; // Front bottom row
            var R = new[] { faceColors[2, 6], faceColors[2, 7], faceColors[2, 8] }; // Right bottom row
            var B = new[] { faceColors[1, 6], faceColors[1, 7], faceColors[1, 8] }; // Back bottom row
            var L = new[] { faceColors[3, 6], faceColors[3, 7], faceColors[3, 8] }; // Left bottom row

            // F -> R
            faceColors[2, 6] = F[0]; faceColors[2, 7] = F[1]; faceColors[2, 8] = F[2];
            // R -> B
            faceColors[1, 6] = R[0]; faceColors[1, 7] = R[1]; faceColors[1, 8] = R[2];
            // B -> L
            faceColors[3, 6] = B[0]; faceColors[3, 7] = B[1]; faceColors[3, 8] = B[2];
            // L -> F
            faceColors[0, 6] = L[0]; faceColors[0, 7] = L[1]; faceColors[0, 8] = L[2];
        }

        private void RotateBottomEdgesCounterClockwise()
        {
            var F = new[] { faceColors[0, 6], faceColors[0, 7], faceColors[0, 8] };
            var R = new[] { faceColors[2, 6], faceColors[2, 7], faceColors[2, 8] };
            var B = new[] { faceColors[1, 6], faceColors[1, 7], faceColors[1, 8] };
            var L = new[] { faceColors[3, 6], faceColors[3, 7], faceColors[3, 8] };

            // F <- L
            faceColors[0, 6] = L[0]; faceColors[0, 7] = L[1]; faceColors[0, 8] = L[2];
            // L <- B
            faceColors[3, 6] = B[0]; faceColors[3, 7] = B[1]; faceColors[3, 8] = B[2];
            // B <- R
            faceColors[1, 6] = R[0]; faceColors[1, 7] = R[1]; faceColors[1, 8] = R[2];
            // R <- F
            faceColors[2, 6] = F[0]; faceColors[2, 7] = F[1]; faceColors[2, 8] = F[2];
        }



        public void Scramble()
        {
            var rand = new Random();
            for (int i = 0; i < 20; i++) // apply 20 random moves
            {
                int face = rand.Next(0, 6);
                bool clockwise = rand.Next(0, 2) == 0;
                RotateFace(face, clockwise);
            }
        }

        public void ResetToInitial()
        {
            faceColors = new string[6, 9]
            {
        { "blue","blue","blue","blue","blue","blue","blue","blue","blue" },     // front
        { "green","green","green","green","green","green","green","green","green" }, // back
        { "red","red","red","red","red","red","red","red","red" },              // right
        { "orange","orange","orange","orange","orange","orange","orange","orange","orange" }, // left
        { "white","white","white","white","white","white","white","white","white" }, // top
        { "yellow","yellow","yellow","yellow","yellow","yellow","yellow","yellow","yellow" }  // bottom
            };
        }


        public bool IsSolved()
        {
            for (int face = 0; face < 6; face++)
            {
                string center = faceColors[face, 4]; // middle sticker
                for (int i = 0; i < 9; i++)
                {
                    if (faceColors[face, i] != center)
                        return false;
                }
            }
            return true;
        }






    }
}
