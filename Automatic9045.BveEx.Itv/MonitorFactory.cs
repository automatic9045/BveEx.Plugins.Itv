using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SlimDX.Direct3D9;

using BveTypes.ClassWrappers;

namespace Automatic9045.BveEx.Itv
{
    internal class MonitorFactory
    {
        private readonly Device Device;
        private readonly Renderer Renderer;

        public MonitorFactory(Device device, Renderer renderer)
        {
            Device = device;
            Renderer = renderer;
        }

        public Monitor FromModel(Model model, string textureFileName, Size textureSize, double location)
        {
            List<MaterialInfo> targetMaterials = new List<MaterialInfo>();
            ExtendedMaterial[] materials = model.Mesh.GetMaterials();
            if (materials is null) throw new InvalidOperationException("モデルに材質情報が定義されていません。");

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i].TextureFileName.ToLowerInvariant() == textureFileName)
                {
                    targetMaterials.Add(model.Materials[i]);
                }
            }

            return new Monitor(Device, Renderer, targetMaterials, textureSize, location);
        }
    }
}
