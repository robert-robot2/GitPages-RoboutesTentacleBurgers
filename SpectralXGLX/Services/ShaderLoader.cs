namespace SpectralXGLX.Services;

public static class ShaderLoader
{
    private static readonly System.Reflection.Assembly _asm =
        typeof(ShaderLoader).Assembly;

    private static readonly Dictionary<string, string> _cache = new();

    public static string Load(string shaderFileName)
    {
        if (_cache.TryGetValue(shaderFileName, out var cached))
            return cached;

        // Must match: AssemblyName.FolderName.FileName
        // Assembly name for SpectralXGLX project = "SpectralXGLX"
        var resourceName = $"SpectralXGLX.Shaders.{shaderFileName}";

        using var stream = _asm.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException(
                $"[ShaderLoader] Not found: {resourceName}\n" +
                $"Available resources:\n" +
                string.Join("\n", _asm.GetManifestResourceNames()));

        using var reader = new StreamReader(stream);
        var source = reader.ReadToEnd();
        _cache[shaderFileName] = source;
        return source;
    }
}