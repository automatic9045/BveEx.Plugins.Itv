using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SlimDX;
using SlimDX.Direct3D9;

using BveTypes.ClassWrappers;

namespace Automatic9045.BveEx.Itv
{
    internal class Monitor : IDisposable
    {
        private readonly Device Device;
        private readonly Renderer Renderer;

        private readonly IEnumerable<MaterialInfo> TargetMaterials;
        private readonly Size TextureSize;

        private Matrix Transform;
        private Texture Texture = null;
        private Surface Stencil = null;
        private Surface TextureSurface = null;

        public double Location { get; }

        private Camera _Camera = Camera.Default;
        public Camera Camera
        {
            get => _Camera;
            set
            {
                _Camera = value;

                SixDof position = value.Position;
                Transform = Matrix.Translation(-(float)position.X, -(float)position.Y, -(float)position.Z)
                    * Matrix.RotationX((float)position.RotationX) * Matrix.RotationY((float)position.RotationY) * Matrix.RotationZ((float)position.RotationZ);
            }
        }

        public Monitor(Device device, Renderer renderer, IEnumerable<MaterialInfo> targetMaterials, Size textureSize, double location)
        {
            Device = device;
            Renderer = renderer;

            TargetMaterials = targetMaterials;
            TextureSize = textureSize;

            Location = location;
        }

        public void Dispose()
        {
            FreeResources();
        }

        public void Render()
        {
            if (Texture is null || Texture.Disposed)
            {
                Texture = new Texture(Device, TextureSize.Width, TextureSize.Height, 0, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default);
                foreach (MaterialInfo material in TargetMaterials)
                {
                    material.Texture.Dispose();
                    material.Texture = Texture;
                }
            }

            if (Stencil is null || Stencil.Disposed)
            {
                SurfaceDescription surfaceDescription = Device.DepthStencilSurface.Description;
                Stencil = Surface.CreateDepthStencil(Device, Math.Max(surfaceDescription.Width, TextureSize.Width), Math.Max(surfaceDescription.Height, TextureSize.Height),
                    surfaceDescription.Format, surfaceDescription.MultisampleType, surfaceDescription.MultisampleQuality, true);

                Surface oldStencil = Device.DepthStencilSurface;
                Device.DepthStencilSurface = Stencil;
                oldStencil.Dispose();
            }

            if (TextureSurface is null || TextureSurface.Disposed)
            {
                TextureSurface = Texture.GetSurfaceLevel(0);
            }

            Device.SetRenderTarget(0, TextureSurface);
            Device.Clear(ClearFlags.Target, Color.Black, 1, 0);

            Vector3 position = Renderer.GetPosisionFromCurrentBlockOrigin(Camera.Location);
            float direction = Renderer.GetDirectionFromCurrentBlockOrigin(Camera.Location);
            Device.SetTransform(TransformState.View, Renderer.CameraToBlock * Matrix.RotationY(-direction) * Matrix.Translation(position) * Transform);

            Renderer.Render(TextureSize, Camera.Zoom);
        }

        public void FreeResources()
        {
            Texture?.Dispose();
            Stencil?.Dispose();
            TextureSurface?.Dispose();
        }
    }
}
