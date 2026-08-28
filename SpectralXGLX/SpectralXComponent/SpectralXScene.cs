using SpectralXGLX.SpectralGL;
using SpectralXGLX.SpectralXComponent.SpectralXLighting;

namespace SpectralXGLX.SpectralXComponent
{
    /// <summary>
    /// Represents a collection of meshes and lights in the world.
    /// Pure data container. No rendering logic.
    /// </summary>
    public class SpectralXScene
    {
        private readonly List<IMesh> _meshes = new();
        private readonly List<SpectralXLight> _lights = new();

        public IReadOnlyList<IMesh> Meshes => _meshes;
        public IReadOnlyList<SpectralXLight> Lights => _lights;


        public void AddMesh(IMesh mesh)
        {
            if (mesh == null)
                return;
            _meshes.Add(mesh);
        }

        public void RemoveMesh(IMesh mesh)
        {
            _meshes.Remove(mesh);
        }

        public void AddLight(SpectralXLight light)
        {
            if (light == null)
                return;
            _lights.Add(light);
        }

        public void RemoveLight(SpectralXLight light)
        {
            _lights.Remove(light);
        }

        public void Clear()
        {
            _meshes.Clear();
            _lights.Clear();
        }
    }
}
