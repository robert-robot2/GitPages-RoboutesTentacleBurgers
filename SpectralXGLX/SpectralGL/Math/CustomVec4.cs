


namespace SpectralXGLX.SpectralGL.Math
{
    public struct CustomVec4
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public CustomVec4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        // Optional: helper for debugging
        public override string ToString()
        {
            return $"Vec4({X}, {Y}, {Z}, {W})";
        }

        // Conversion from Vec3 + w
        public CustomVec4(CustomVec3 v, float w) : this(v.X, v.Y, v.Z, w) { }
    }
}
