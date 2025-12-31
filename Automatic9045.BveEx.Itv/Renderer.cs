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
    internal class Renderer
    {
        private int CurrentBlockOriginLocation = 0;

        public Scenario Scenario { get; set; } = null;
        public Matrix CameraToBlock { get; private set; }

        public Renderer()
        {
        }

        public void Tick()
        {
            Matrix cameraToBlock = Scenario.Vehicle.CameraLocation.TransformFromBlock;
            cameraToBlock.Invert();
            CameraToBlock = cameraToBlock;

            CurrentBlockOriginLocation = Scenario.VehicleLocation.BlockIndex * 25;
        }

        public Vector3 GetPosisionFromCurrentBlockOrigin(double location)
            => Scenario.Map.MyTrack.GetPosition(CurrentBlockOriginLocation, location);

        public float GetDirectionFromCurrentBlockOrigin(double location)
        {
            MyTrack myTrack = Scenario.Map.MyTrack;

            double originDirection = myTrack.GetDirectionAt(CurrentBlockOriginLocation);
            double targetDirection = myTrack.GetDirectionAt((int)location);
            double relativeDirection = targetDirection - originDirection;

            return (float)relativeDirection;
        }

        public void Render(Size renderSize, double zoom)
        {
            float aspect = (float)renderSize.Width / renderSize.Height;
            Vector2 planeVertex = new Vector2(1, -1 / aspect) / (float)zoom;
            Scenario.Vehicle.CameraLocation.Plane = new RectangleF(-planeVertex.X / 2, -planeVertex.Y / 2, planeVertex.X, planeVertex.Y);
            Scenario.ObjectDrawer.Draw();
        }
    }
}
