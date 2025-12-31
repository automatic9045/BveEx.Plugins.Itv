using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BveTypes.ClassWrappers;

using BveEx.Extensions.MapStatements;

namespace Automatic9045.BveEx.Itv
{
    internal class ItvFactory : IDisposable
    {
        private static readonly string UserName = "Automatic9045";
        private static readonly ClauseFilter RootFilter = ClauseFilter.Element("Itv", 0);

        private static readonly ClauseFilter[] PutCameraFilters = new ClauseFilter[]
        {
            RootFilter, ClauseFilter.Element("Camera", 1), ClauseFilter.Function("Put", 7),
        };

        private static readonly ClauseFilter[] PutMonitorFilters = new ClauseFilter[]
        {
            RootFilter, ClauseFilter.Element("Monitor", 1), ClauseFilter.Function("Put", 13),
        };


        private readonly IStatementSet Statements;
        private readonly MonitorFactory MonitorFactory;
        private readonly LoadingProgressForm LoadingProgressForm;

        private readonly Dictionary<string, Camera> Cameras = new Dictionary<string, Camera>();
        private readonly List<MonitorConfig> MonitorConfigs = new List<MonitorConfig>();

        public IReadOnlyList<Monitor> Monitors { get; private set; } = null;

        public event EventHandler Loaded;

        public ItvFactory(IStatementSet statements, Renderer renderer, LoadingProgressForm loadingProgressForm)
        {
            Statements = statements;
            Statements.StatementLoaded += OnStatementLoaded;
            Statements.LoadingCompleted += OnStatementLoadingCompleted;

            MonitorFactory = new MonitorFactory(Direct3DProvider.Instance.Device, renderer);
            LoadingProgressForm = loadingProgressForm;
        }

        public void Dispose()
        {
            Statements.StatementLoaded -= OnStatementLoaded;
            Statements.LoadingCompleted -= OnStatementLoadingCompleted;
        }

        private void OnStatementLoaded(object sender, StatementLoadedEventArgs e)
        {
            MapStatement statement = e.Statement.Source;

            try
            {
                if (e.Statement.IsUserStatement(UserName, PutCameraFilters))
                {
                    MapStatementClause keyClause = statement.Clauses[4];
                    MapStatementClause functionClause = statement.Clauses[5];

                    string key = Convert.ToString(keyClause.Keys[0]).ToLowerInvariant();
                    SixDof position = new SixDof(
                        Convert.ToDouble(functionClause.Args[0]), Convert.ToDouble(functionClause.Args[1]), Convert.ToDouble(functionClause.Args[2]),
                        Convert.ToDouble(functionClause.Args[3]) / 180 * Math.PI, Convert.ToDouble(functionClause.Args[4]) / 180 * Math.PI, Convert.ToDouble(functionClause.Args[5]) / 180 * Math.PI);
                    double zoom = Convert.ToDouble(functionClause.Args[6]);

                    Camera camera = new Camera(statement.Location, position, zoom);
                    Cameras.Add(key, camera);
                }
                else if (e.Statement.IsUserStatement(UserName, PutMonitorFilters))
                {
                    MapStatementClause keyClause = statement.Clauses[4];
                    MapStatementClause functionClause = statement.Clauses[5];

                    string structureKey = Convert.ToString(keyClause.Keys[0]).ToLowerInvariant();
                    Model model = e.MapLoader.Map.StructureModels[structureKey];

                    string trackKey = Convert.ToString(functionClause.Args[0]).ToLowerInvariant();
                    SixDof position = new SixDof(
                        Convert.ToDouble(functionClause.Args[1]), Convert.ToDouble(functionClause.Args[2]), Convert.ToDouble(functionClause.Args[3]),
                        Convert.ToDouble(functionClause.Args[4]) / 180 * Math.PI, Convert.ToDouble(functionClause.Args[5]) / 180 * Math.PI, Convert.ToDouble(functionClause.Args[6]) / 180 * Math.PI);
                    TiltOptions tiltOptions = (TiltOptions)Convert.ToInt32(functionClause.Args[7]);
                    double span = Convert.ToDouble(functionClause.Args[8]);

                    Size textureSize = new Size(Convert.ToInt32(functionClause.Args[9]), Convert.ToInt32(functionClause.Args[10]));
                    string texturePath = Convert.ToString(functionClause.Args[11]).ToLowerInvariant();
                    string cameraKey = Convert.ToString(functionClause.Args[12]).ToLowerInvariant();

                    Structure structure = new Structure(statement.Location, trackKey,
                        position.X, position.Y, position.Z, position.RotationX, position.RotationY, position.RotationZ, tiltOptions, span, model);
                    e.MapLoader.Map.Structures.Put.Add(structure);

                    Monitor monitor = MonitorFactory.FromModel(model, texturePath, textureSize, statement.Location);
                    MonitorConfig monitorConfig = new MonitorConfig(monitor, cameraKey, statement);
                    MonitorConfigs.Add(monitorConfig);
                }
            }
            catch (Exception ex)
            {
                LoadingProgressForm.ThrowError(ex.Message, statement.FileName, statement.Clauses[0].LineIndex, statement.Clauses[0].CharIndex);
            }
        }

        private void OnStatementLoadingCompleted(object sender, EventArgs e)
        {
            List<Monitor> monitors = new List<Monitor>(MonitorConfigs.Count);

            foreach (MonitorConfig monitorConfig in MonitorConfigs)
            {
                try
                {
                    Monitor monitor = monitorConfig.Monitor;
                    Camera camera = Cameras[monitorConfig.CameraKey];
                    monitor.Camera = camera;
                    monitors.Add(monitor);
                }
                catch (Exception ex)
                {
                    MapStatement source = monitorConfig.Source;
                    LoadingProgressForm.ThrowError(ex.Message, source.FileName, source.Clauses[0].LineIndex, source.Clauses[0].CharIndex);
                }
            }

            Monitors = monitors;
            Loaded?.Invoke(this, EventArgs.Empty);
        }


        private class MonitorConfig
        {
            public Monitor Monitor { get; }
            public string CameraKey { get; }
            public MapStatement Source { get; }

            public MonitorConfig(Monitor monitor, string cameraKey, MapStatement source)
            {
                Monitor = monitor;
                CameraKey = cameraKey;
                Source = source;
            }
        }
    }
}
