using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using SlimDX;
using SlimDX.Direct3D9;

using BveTypes.ClassWrappers;
using FastMember;
using ObjectiveHarmonyPatch;
using TypeWrapping;

using BveEx.Extensions.MapStatements;
using BveEx.PluginHost.Plugins;

namespace Automatic9045.BveEx.Itv
{
    [Plugin(PluginType.MapPlugin)]
    internal class PluginMain : AssemblyPluginBase
    {
        private readonly HarmonyPatch DrawPatch;
        private readonly HarmonyPatch OnDeviceLostPatch;

        private readonly Renderer Renderer = new Renderer();

        private IReadOnlyList<Monitor> Monitors;
        private Surface OriginalRenderTarget = null;

        public PluginMain(PluginBuilder builder) : base(builder)
        {
            IStatementSet statements = Extensions.GetExtension<IStatementSet>();
            ItvFactory itvFactory = new ItvFactory(statements, Renderer, BveHacker.LoadingProgressForm);
            itvFactory.Loaded += OnItvLoaded;

            ClassMemberSet mainFormMembers = BveHacker.BveTypes.GetClassInfoOf<Scenario>();
            FastMethod drawMethod = mainFormMembers.GetSourceMethodOf(nameof(Scenario.Draw));
            DrawPatch = HarmonyPatch.Patch(null, drawMethod.Source, PatchType.Prefix);

            ClassMemberSet assistantDrawerMembers = BveHacker.BveTypes.GetClassInfoOf<AssistantSet>();
            FastMethod onDeviceLostMethod = assistantDrawerMembers.GetSourceMethodOf(nameof(AssistantSet.OnDeviceLost));
            OnDeviceLostPatch = HarmonyPatch.Patch(null, onDeviceLostMethod.Source, PatchType.Prefix);

            int frameCount = 0;
            DrawPatch.Invoked += (sender, e) =>
            {
                if (!BveHacker.IsScenarioCreated) return PatchInvokationResult.DoNothing(e);
                
                if (frameCount <= 5)
                {
                    frameCount++;
                    return PatchInvokationResult.DoNothing(e);
                }
                frameCount = 0;

                Renderer.Scenario = Renderer.Scenario ?? BveHacker.Scenario;
                Device device = Direct3DProvider.Instance.Device;

                if (OriginalRenderTarget is null || OriginalRenderTarget.Disposed)
                {
                    OriginalRenderTarget = device.GetRenderTarget(0);
                }

                RectangleF originalPlane = BveHacker.Scenario.Vehicle.CameraLocation.Plane;
                Renderer.Tick();

                double location = BveHacker.Scenario.VehicleLocation.Location;

                ObjectDrawer objectDrawer = BveHacker.Scenario.ObjectDrawer;
                double backDrawDistance = objectDrawer.DrawDistanceManager.BackDrawDistance;
                double frontDrawDistance = objectDrawer.DrawDistanceManager.FrontDrawDistance;

                foreach (Monitor monitor in Monitors)
                {
                    double monitorDistance = monitor.Location - location;
                    if (-backDrawDistance < monitorDistance && monitorDistance < frontDrawDistance)
                    {
                        double cameraDistance = monitor.Camera.Location - location;
                        objectDrawer.DrawDistanceManager.BackDrawDistance = Math.Max(-cameraDistance + 100, backDrawDistance);
                        objectDrawer.DrawDistanceManager.FrontDrawDistance = Math.Max(cameraDistance + 100, frontDrawDistance);
                        objectDrawer.StructureDrawer.OnDrawLocationRangeUpdated(objectDrawer.StructureDrawer.Src, EventArgs.Empty);

                        monitor.Render();
                    }
                }

                objectDrawer.DrawDistanceManager.BackDrawDistance = backDrawDistance;
                objectDrawer.DrawDistanceManager.FrontDrawDistance = frontDrawDistance;
                objectDrawer.StructureDrawer.OnDrawLocationRangeUpdated(objectDrawer.StructureDrawer.Src, EventArgs.Empty);

                device.SetRenderTarget(0, OriginalRenderTarget);
                device.SetTransform(TransformState.View, Matrix.Identity);
                BveHacker.Scenario.Vehicle.CameraLocation.Plane = originalPlane;

                return PatchInvokationResult.DoNothing(e);
            };

            OnDeviceLostPatch.Invoked += (sender, e) =>
            {
                FreeResources();
                return PatchInvokationResult.DoNothing(e);
            };


            void OnItvLoaded(object sender, EventArgs e)
            {
                Monitors = itvFactory.Monitors;
                itvFactory.Dispose();
            }
        }

        public override void Dispose()
        {
            DrawPatch.Dispose();
            OnDeviceLostPatch.Dispose();

            FreeResources();
        }

        private void FreeResources()
        {
            OriginalRenderTarget?.Dispose();
            foreach (Monitor monitor in Monitors)
            {
                monitor.Dispose();
            }
        }

        public override void Tick(TimeSpan elapsed)
        {
        }
    }
}
