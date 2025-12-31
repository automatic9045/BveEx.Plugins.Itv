using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BveTypes.ClassWrappers;

namespace Automatic9045.BveEx.Itv
{
    internal class Camera
    {
        public static readonly Camera Default = new Camera(0, new SixDof(0, 0, 0, 0, 0, 0), 1);


        public double Location { get; }
        public SixDof Position { get; }
        public double Zoom { get; }

        public Camera(double location, SixDof position, double zoom)
        {
            Location = location;
            Position = position;
            Zoom = zoom;
        }
    }
}
