namespace SpectralXGLX.SpectralGL.Math

{
    public struct CustomVec3
    {
        public float X;
        public float Y;
        public float Z;

        public CustomVec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static CustomVec3 operator +(CustomVec3 a, CustomVec3 b)
            => new CustomVec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static CustomVec3 operator -(CustomVec3 a, CustomVec3 b)
            => new CustomVec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static CustomVec3 operator *(CustomVec3 a, float s)
            => new CustomVec3(a.X * s, a.Y * s, a.Z * s);

        public static CustomVec3 operator /(CustomVec3 a, float s)
            => new CustomVec3(a.X / s, a.Y / s, a.Z / s);

        public float Dot(CustomVec3 b)
            => X * b.X + Y * b.Y + Z * b.Z;

        public CustomVec3 Cross(CustomVec3 b)
            => new CustomVec3(
                Y * b.Z - Z * b.Y,
                Z * b.X - X * b.Z,
                X * b.Y - Y * b.X
            );

        public float Length()
            => MathF.Sqrt(X * X + Y * Y + Z * Z);

        // added to display camera realtime updates xyz
        public override string ToString()
        {
            return $"({X:F2}, {Y:F2}, {Z:F2})";
        }


        public CustomVec3 Normalized()
        {
            float len = Length();
            return len > 0 ? this / len : new CustomVec3(0, 0, 0);
        }
    }
}
