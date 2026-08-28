using SpectralXGLX.SpectralGL.Math;

namespace SpectralXGLX.SpectralXComponent.SpectralXRender
{
    /// <summary>
    /// Interface for all mesh types in SpectralX engine
    /// </summary>
    public interface IMesh
    {

        string Name { get; set; }
        bool TransformDirty { get; set; }
        Vector3 Position { get; set; }
        Vector4 Color { get; set; }
        Vector3 Size { get; set; }
        Vector3 Rotation { get; set; }
        CustomMat4 WorldMatrix { get; }


        // Geometry
        List<Vector3> Vertices { get; }
        List<Vector2> UVs { get; }
        List<Vector3> Normals { get; }
        List<Vector3> PolygonNormals { get; }
        List<Face> Faces { get; }

        // Geometry counts
        int VertexCount { get; }
        int UVCount { get; }
        int NormalCount { get; }
        int FaceCount { get; }
        int PrimitiveCount { get; }

        // Edges
        List<Edge> Edges { get; }
        int EdgeCount { get; }

        // Face type counts
        bool HasRenderableFaces { get; }
        int TriangleFaceCount { get; }
        int QuadFaceCount { get; }

        // Shaders
        float GlowRadius { get; set; }
        float GlowStrength { get; set; }

        // Glow color — separate from text color, controls mirror glow tint
        Vector4 GlowColor { get; set; }

        // Shadow blur — CSS style soft feathered bloom behind text
        float ShadowBlur { get; set; }
        Vector4 ShadowColor { get; set; }

        // Glow pulse — animates glow radius over time like CSS animation
        float GlowPulseSpeed { get; set; }
        float GlowPulseMin { get; set; }
        float GlowPulseMax { get; set; }

        bool IsEmissive { get; set; }
        float EmissiveIntensity { get; set; }


        // ── SDF Text Properties ──────────────────────────────────────────────────
        bool IsSDFText { get; set; } 
        string Text { get; set; }
        float FontSize { get; set; }
        string FontKey { get; set; } 
        float LetterSpacing { get; set; } 
        float LineHeight { get; set; } 
        TextAlignment TextAlign { get; set; } 
        Vector4 OutlineColor { get; set; } 
        float OutlineWidth { get; set; } 
        bool TextDirty { get; set; } 
    
        // Materials
        List<string> MaterialTextures { get; set; }
        List<Vector4> MaterialColors { get; set; }
        string? OverlayTextureDataUrl { get; set; }
        float OverlayAlpha { get; set; }
        bool OverlayDirty { get; set; }

        // Texture support
        string? TexturePath { get; set; }
        bool HasTexture => !string.IsNullOrEmpty(TexturePath);
        Vector3[,]? TextureData { get; set; }
        string? TextureDataUrl { get; set; }
        int TextureWidth { get; set; }
        int TextureHeight { get; set; }
        bool TextureIsRawRGBA { get; set; }
        bool TextureDirty { get; set; } 

        // Sprite sheet animation
        bool IsAnimated { get; set; } 
        int FrameCount { get; set; } 
        float FrameRate { get; set; } 
        int CurrentFrame { get; set; } 
        float FrameTimer { get; set; } 
        float SheetWidth { get; set; } 
        float SheetHeight { get; set; } 
        float FramePixelWidth { get; set; }

        float FramePixelHeight { get; set; }

        float UVOffsetX { get; set; }
        float UVOffsetY { get; set; }

        float UVScaleX { get; set; }

        float UVScaleY { get; set; }
        bool FacingRight { get; set; } 
        bool CastsShadow { get; set; }
        bool ReceivesShadow { get; set; } 
        string? JSSourceMesh { get; set; }

        bool IsValid();
        IMesh Clone();
    }
}
