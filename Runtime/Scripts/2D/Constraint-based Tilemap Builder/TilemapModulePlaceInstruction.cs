using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

using Rng = System.Random;

namespace Zlitz.Extra2D.WorldGen
{
    [RequireComponent(typeof(TilemapModulePlacer))]
    public abstract class TilemapModulePlaceInstruction : MonoBehaviour
    {
        [SerializeField]
        private BuildMode m_buildMode;

        [SerializeField]
        private bool m_asyncMode = true;

        [SerializeField]
        private TilemapModuleSet m_moduleSet;

        [SerializeField]
        private UnityEvent m_onLayoutCompleted;

        [SerializeField]
        private UnityEvent m_onLayoutFailed;

        [SerializeField]
        private UnityEvent m_onPlaceCompleted;

        [SerializeField]
        private bool m_exportLayoutStackTrace;

        [SerializeField, HideInInspector]
        private bool m_building;

        [SerializeField, HideInInspector]
        private bool m_built;

        [SerializeField, HideInInspector]
        private PlacementResult[] m_placementResults;

        public TilemapModuleSet moduleSet
        {
            get => m_moduleSet;
            set => m_moduleSet = value;
        }

        public event UnityAction onLayoutCompleted
        {
            add => m_onLayoutCompleted.AddListener(value);
            remove => m_onLayoutCompleted.RemoveListener(value);
        }

        public event UnityAction onLayoutFailed
        {
            add => m_onLayoutFailed.AddListener(value);
            remove => m_onLayoutFailed.RemoveListener(value);
        }

        public event UnityAction onPlaceCompleted
        {
            add => m_onPlaceCompleted.AddListener(value);
            remove => m_onPlaceCompleted.RemoveListener(value);
        }

        private CancellationTokenSource m_cts = new CancellationTokenSource();

        [ContextMenu("Build")]
        private void ContextMenuBuild()
        {
            if (m_asyncMode)
            {
                Task _ = BuildAsync();
            }
            else
            {
                Build();
            }
        }

        [ContextMenu("Cancel current build")]
        private void CancelCurrentBuild()
        {
            m_cts.Cancel();
            m_building = false;
        }

        [ContextMenu("Clear")]
        private void Clear()
        {
            if (TryGetComponent(out TilemapModulePlacer placer))
            {
                placer.Clear();
            }
            m_placementResults = null;
            m_built = false;
        }

        public void Build()
        {
            if (m_built)
            {
                Debug.LogWarning("TilemapModulePlaceInstruction had already execute a build. Clear first to build again.");
                return;
            }

            Debug.Assert(m_moduleSet != null, "ModuleSet must not be null");

            TilemapModulePlacer placer = GetComponent<TilemapModulePlacer>();
            Debug.Assert(placer != null, "Requires a TilemapModulePlacer to execute instructions");

            if (!placer.Begin(m_moduleSet))
            {
                return;
            }

            Rng rng = new Rng();
            LayoutStateStack states = new LayoutStateStack();

            Instruction[] instructions = CreateInstructions(rng).Select(i => Instruction.Copy(i)).Where(i => i != null).ToArray();

            bool buildResult = ExecuteRecursive(instructions, 0, states, rng);
            if (m_exportLayoutStackTrace)
            {
                string file = $"Zlitz_Extra2D_WorldGen_{GetType().Name}_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.txt";
                string path = Path.Combine(Application.persistentDataPath, file);

                using (StreamWriter sw = new StreamWriter(path, false))
                {
                    sw.WriteLine($"Build succeeded: {buildResult}");
                    sw.Write(states.stackTrace);
                }

                Debug.Log($"Layout Stack Trace exported to: {path}.");
            }

            if (!buildResult)
            {
                m_onLayoutFailed?.Invoke();
                return;
            }

            m_placementResults = states.currentState.placements.Select(p => p.ToPlacementResult()).ToArray();
            m_onLayoutCompleted?.Invoke();
            
            foreach (Placement placement in states.currentState.placements)
            {
                placer.Place(placement.content, placement.position);
            }

            m_onPlaceCompleted?.Invoke();
            m_built = true;
        }

        public async Task BuildAsync()
        {
            if (m_built)
            {
                Debug.LogWarning("TilemapModulePlaceInstruction had already execute a build. Clear first to build again.");
                return;
            }

            if (m_building)
            {
                Debug.LogWarning("This TilemapModulePlaceInstruction is building.");
                return;
            }

            Debug.Assert(m_moduleSet != null, "ModuleSet must not be null");

            TilemapModulePlacer placer = GetComponent<TilemapModulePlacer>();
            Debug.Assert(placer != null, "Requires a TilemapModulePlacer to execute instructions");

            if (!placer.Begin(m_moduleSet))
            {
                return;
            }

            m_cts = new CancellationTokenSource();

            Rng rng = new Rng();
            LayoutStateStack states = new LayoutStateStack();

            Instruction[] instructions = CreateInstructions(rng).Select(i => Instruction.Copy(i)).Where(i => i != null).ToArray();

            m_building = true;

            bool buildResult = await ExecuteAsyncRecursive(instructions, 0, states, rng, m_cts.Token);

            if (m_exportLayoutStackTrace)
            {
                string file = $"Zlitz_Extra2D_WorldGen_{GetType().Name}_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.txt";
                string path = Path.Combine(Application.persistentDataPath, file);

                using (StreamWriter sw = new StreamWriter(path, false))
                {
                    sw.WriteLine($"Build succeeded: {buildResult}");
                    sw.Write(states.stackTrace);
                }

                Debug.Log($"Layout Stack Trace exported to: {path}.");
            }

            if (!buildResult)
            {
                m_onLayoutFailed?.Invoke();
                return;
            }

            m_placementResults = states.currentState.placements.Select(p => p.ToPlacementResult()).ToArray();
            m_onLayoutCompleted?.Invoke();

            List<Task> placeTasks = new List<Task>();
            foreach (Placement placement in states.currentState.placements)
            {
                placeTasks.Add(placer.PlaceAsync(placement.content, placement.position, m_cts.Token));
                if (placeTasks.Count >= 16)
                {
                    await Task.WhenAll(placeTasks);
                    placeTasks.Clear();
                    await Task.Yield();
                }
            }

            await Task.WhenAll(placeTasks);

            m_building = false;

            m_onPlaceCompleted?.Invoke();
            m_built = true;
        }

        protected abstract IEnumerable<Instruction> CreateInstructions(Rng rng);

        private void Awake()
        {
            if (m_buildMode == BuildMode.BuildOnAwake || m_buildMode == BuildMode.ForceBuildOnAwake)
            {
                if (m_buildMode == BuildMode.ForceBuildOnAwake)
                {
                    Clear();
                }
                if (m_asyncMode)
                {
                    Task _ = BuildAsync();
                }
                else
                {
                    Build();
                }
            }
        }

        private void Start()
        {
            if (m_buildMode == BuildMode.BuildOnStart || m_buildMode == BuildMode.ForceBuildOnStart)
            {
                if (m_buildMode == BuildMode.ForceBuildOnStart)
                {
                    Clear();
                }
                if (m_asyncMode)
                {
                    Task _ = BuildAsync();
                }
                else
                {
                    Build();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (m_placementResults != null)
            {
                Color gizmosColor = Gizmos.color;
                
                Matrix4x4 gizmosMatrix = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;

                foreach (PlacementResult placement in m_placementResults)
                {
                    Vector3 bottomLeft  = new Vector3(placement.position.x, placement.position.y, 0);
                    Vector3 bottomRight = bottomLeft + new Vector3(placement.regionSize.x, 0, 0);
                    Vector3 topRight    = bottomRight + new Vector3(0, placement.regionSize.y, 0);
                    Vector3 topLeft     = bottomLeft + new Vector3(0, placement.regionSize.y, 0);

                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(bottomLeft, bottomRight);
                    Gizmos.DrawLine(bottomRight, topRight);
                    Gizmos.DrawLine(topRight, topLeft);
                    Gizmos.DrawLine(topLeft, bottomLeft);

                    Gizmos.color = Color.red;
                    foreach (PlacementResult.Connection connection in placement.connections)
                    {
                        Vector3 from = bottomLeft + new Vector3(0.5f + connection.position.x, 0.5f + connection.position.y, 0.0f);
                        Vector3 to   = from + 0.4f * new Vector3(connection.direction.x, connection.direction.y, 0.0f);
                        Gizmos.DrawLine(from, to);
                    }
                }

                Gizmos.matrix = gizmosMatrix;
                Gizmos.color = gizmosColor;
            }   
        }

        private bool ExecuteRecursive(Instruction[] instructions, int index, LayoutStateStack states, Rng rng)
        {
            if (index >= instructions.Length)
            {
                return true;
            }
            
            Instruction instruction = instructions[index];
            instruction.Reset(m_moduleSet, states, rng);

            while (true)
            {
                states.Push($"Instruction {index}: {instruction.debugName}");

                bool execute = instruction.Execute(moduleSet, states, rng);
                if (execute && ExecuteRecursive(instructions, index + 1, states, rng))
                {
                    return true;
                }

                states.Pop();
                if (instruction.Next(m_moduleSet, rng))
                {
                    continue;
                }

                return false;
            }
        }

        private async Task<bool> ExecuteAsyncRecursive(Instruction[] instructions, int index, LayoutStateStack states, Rng rng, CancellationToken ct)
        {
            if (index >= instructions.Length)
            {
                return true;
            }

            Instruction instruction = instructions[index];
            instruction.Reset(m_moduleSet, states, rng);

            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    return false;
                }

                states.Push($"Instruction {index}: {instruction.debugName}");

                bool execute = instruction.Execute(moduleSet, states, rng);
                if (execute && await ExecuteAsyncRecursive(instructions, index + 1, states, rng, ct))
                {
                    await Task.Yield();
                    return true;
                }

                await Task.Yield();

                states.Pop();
                if (instruction.Next(m_moduleSet, rng))
                {
                    continue;
                }

                return false;
            }
        }

        public abstract class Instruction
        {
            public int repeatIndex { get; internal set; } = 0;

            public float weight { get; private set; }

            public abstract string debugName { get; }

            public abstract void Reset(TilemapModuleSet moduleSet, LayoutStateStack states, Rng rng);

            public abstract bool Execute(TilemapModuleSet moduleSet, LayoutStateStack states, Rng rng);

            public abstract bool Next(TilemapModuleSet moduleSet, Rng rng);

            protected abstract Instruction Copy();

            public Instruction Weight(float weight)
            {
                this.weight = Mathf.Max(0.0f, weight);
                return this;
            }

            public static Instruction Copy(Instruction source)
            {
                Instruction result = source.Copy();
                return result?.Weight(source.weight);
            }
        }

        public class Connection
        {
            public Vector2Int position { get; private set; }

            public Vector2Int direction { get; private set; }
        
            public string tag;

            public bool MatchTag(string tag)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    return true;
                }
                return tag == this.tag;
            }

            public Connection(Vector2Int position, Vector2Int direction)
            {
                this.position  = position;
                this.direction = direction;
            }

            public Connection(Connection other)
            {
                position  = other.position;
                direction = other.direction;
                tag       = other.tag;
            }
        }

        public struct Placement
        {
            public ITilemapModule module { get; private set; }

            public GeneratedTilemapModule content {get; private set;}

            public Vector2Int position {get; private set;}

            public Placement(ITilemapModule module, GeneratedTilemapModule content, Vector2Int position)
            {
                this.module   = module;
                this.content  = content;
                this.position = position;
            }

            internal PlacementResult ToPlacementResult()
            {
                return new PlacementResult(module.id, position, content.regionSize, content.connections);
            }
        }

        public sealed class LayoutState
        {
            public string debugName;

            private GridMask m_gridMask;

            private List<Connection> m_connections;

            private List<Placement> m_placements;

            public IEnumerable<Connection> connections => m_connections;

            public IEnumerable<Placement> placements => m_placements;

            public bool TryPlace(GeneratedTilemapModule generatedModule, Vector2Int position)
            {
                for (int dx = 0; dx < generatedModule.regionSize.x; dx++)
                {
                    for (int dy = 0; dy < generatedModule.regionSize.y; dy++)
                    {
                        Vector2Int offset = new Vector2Int(dx, dy);
                        if (generatedModule.GetPlaceInfo(offset) != null && m_gridMask.Contains(position + offset))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            public void AddPlacement(ITilemapModule module, GeneratedTilemapModule content, Vector2Int position, Action<IEnumerable<Connection>, Rng> newConnectionCallback, Rng rng, Vector2Int? connectedAt = null)
            {
                m_placements.Add(new Placement(module, content, position));
                
                for (int dx = 0; dx < content.regionSize.x; dx++)
                {
                    for (int dy = 0; dy < content.regionSize.y; dy++)
                    {
                        Vector2Int offset = new Vector2Int(dx, dy);
                        m_gridMask.Add(position + offset);
                    }
                }

                List<Connection> newConnections = new List<Connection>();
                foreach (KeyValuePair<Vector2Int, Vector2Int> generatedConnection in content.connections)
                {
                    Vector2Int pos = position + generatedConnection.Key;

                    if (connectedAt.HasValue && connectedAt.Value == pos)
                    {
                        for (int i = 0; i < m_connections.Count; i++)
                        {
                            if (m_connections[i].direction + generatedConnection.Value == Vector2Int.zero && m_connections[i].position == pos + generatedConnection.Value)
                            {
                                m_connections.RemoveAt(i);
                                break;
                            }
                        }
                        continue;
                    }

                    Connection newConnection = new Connection(pos, generatedConnection.Value);

                    newConnections.Add(newConnection);
                    m_connections.Add(newConnection);
                }

                newConnectionCallback?.Invoke(newConnections, rng);
            }

            public Connection[] GetConnections(string[] connectionTag)
            {
                return m_connections.Where(c => connectionTag == null || connectionTag.Length <= 0 || connectionTag.Any(t => c.MatchTag(t))).ToArray();
            }

            public LayoutState()
            {
                m_gridMask    = new GridMask();
                m_connections = new List<Connection>();
                m_placements  = new List<Placement>();
            }

            public LayoutState(LayoutState other)
            {
                m_gridMask    = new GridMask(other.m_gridMask);
                m_connections = other.m_connections.Select(c => new Connection(c)).ToList();
                m_placements  = other.m_placements.ToList();
            }
        }

        public sealed class LayoutStateStack
        {
            private Stack<LayoutState> m_states;

            private StringBuilder m_stackTrace = new StringBuilder();

            public LayoutState currentState => m_states.Peek();

            public int count => m_states.Count;

            public string stackTrace => m_stackTrace.ToString();

            public void Push(string debugName)
            {
                if (m_states.TryPeek(out LayoutState currentState))
                {
                    m_states.Push(new LayoutState(currentState));
                }
                else
                {
                    m_states.Push(new LayoutState());
                }
                m_states.Peek().debugName = debugName;
                
                m_stackTrace.AppendLine($"Push {debugName}");
                
                m_stackTrace.AppendLine($"  Placements:");
                foreach (Placement placement in m_states.Peek().placements)
                {
                    m_stackTrace.AppendLine($"  + {placement.module.id} - {placement.position}");
                }

                m_stackTrace.AppendLine($"  Connections:");
                foreach (Connection connection in m_states.Peek().connections)
                {
                    m_stackTrace.AppendLine($"  + {connection.position} - {connection.direction} ({connection.tag})");
                }
            }

            public bool Pop()
            {
                if (m_states.TryPop(out LayoutState currentState))
                {
                    m_stackTrace.AppendLine($"Pop {currentState.debugName}");

                    m_stackTrace.AppendLine($"  Placements:");
                    foreach (Placement placement in currentState.placements)
                    {
                        m_stackTrace.AppendLine($"  + {placement.module.id} - {placement.position}");
                    }

                    m_stackTrace.AppendLine($"  Connections:");
                    foreach (Connection connection in currentState.connections)
                    {
                        m_stackTrace.AppendLine($"  + {connection.position} - {connection.direction} ({connection.tag})");
                    }

                    return true;
                }
                return false;
            }

            public bool PopUnder()
            {
                if (m_states.TryPop(out LayoutState currentState))
                {
                    bool result = m_states.TryPop(out LayoutState previousState);
                    m_states.Push(currentState);

                    if (result)
                    {
                        m_stackTrace.AppendLine($"PopUnder {currentState.debugName}");

                        m_stackTrace.AppendLine($"  Placements:");
                        foreach (Placement placement in currentState.placements)
                        {
                            m_stackTrace.AppendLine($"  + {placement.module.id} - {placement.position}");
                        }

                        m_stackTrace.AppendLine($"  Connections:");
                        foreach (Connection connection in currentState.connections)
                        {
                            m_stackTrace.AppendLine($"  + {connection.position} - {connection.direction} ({connection.tag})");
                        }
                    }
                    return result;
                }
                return false;
            }

            public LayoutStateStack()
            {
                m_states = new Stack<LayoutState>();
            }
        }

        [Serializable]
        internal struct PlacementResult
        {
            [SerializeField]
            private string m_moduleId;

            [SerializeField]
            private Vector2Int m_position;

            [SerializeField]
            private Vector2Int m_regionSize;

            [SerializeField]
            private Connection[] m_connections;

            public string moduleId => m_moduleId;

            public Vector2Int position => m_position;

            public Vector2Int regionSize => m_regionSize;

            public IEnumerable<Connection> connections => m_connections ?? Enumerable.Empty<Connection>();

            public PlacementResult(string moduleId, Vector2Int position, Vector2Int regionSize, IEnumerable<KeyValuePair<Vector2Int, Vector2Int>> connections)
            {
                m_moduleId = moduleId;
                m_position = position;
                m_regionSize = regionSize;
                m_connections = connections.Select(c => new Connection(c.Key, c.Value)).ToArray();
            }

            [SerializeField]
            public struct Connection
            {
                [SerializeField]
                private Vector2Int m_position;

                [SerializeField]
                private Vector2Int m_direction;

                public Vector2Int position => m_position;

                public Vector2Int direction => m_direction;

                public Connection(Vector2Int position, Vector2Int direction)
                {
                    m_position = position;
                    m_direction = direction;
                }
            }
        }

        public enum BuildMode
        {
            Manually,
            BuildOnAwake,
            ForceBuildOnAwake,
            BuildOnStart,
            ForceBuildOnStart
        }

        public static class Instructions
        {
            public static Instruction PlaceAtPosition(TilemapModulePool modulePool, Vector2Int position, Action<IEnumerable<Connection>, int, Rng> newConnectionCallback)
            {
                return new PlacePositionInstruction(modulePool, position, newConnectionCallback);
            }

            public static Instruction PlaceConnectTo(TilemapModulePool modulePool, Func<int, string[]> connectionTag, Action<IEnumerable<Connection>, int, Rng> newConnectionCallback)
            {
                return new PlaceConnectionInstruction(modulePool, connectionTag, newConnectionCallback);
            }

            public static Instruction Random(params Instruction[] instructions)
            {
                return new RandomInstruction(instructions);
            }
        }

        public static class ConnectionTags
        {
            public static Func<int, string[]> Any => null;

            public static Func<int, string[]> Of(params string[] connectionTags)
            {
                return (index) => connectionTags;
            }

            public static Func<int, string[]> ByIndex(Func<int, string> connectionTag)
            {
                return (index) => new string[] { connectionTag?.Invoke(index) ?? "" };
            }

            public static Func<int, string[]> ByIndex(Func<int, string[]> connectionTags)
            {
                return (index) => connectionTags?.Invoke(index) ?? null;
            }
        }

        public static class ConnectionCallbacks
        {
            public static Action<IEnumerable<Connection>, int, Rng> None => null;

            public static Action<IEnumerable<Connection>, int, Rng> AssignTagAll(string connectionTag)
            {
                return (connections, index, rng) => AssignTagAllImpl(connections, connectionTag);
            }

            public static Action<IEnumerable<Connection>, int, Rng> AssignTagAll(Func<int, string> connectionTag)
            {
                return (connections, index, rng) => AssignTagAllImpl(connections, connectionTag?.Invoke(index) ?? "");
            }

            public static Action<IEnumerable<Connection>, int, Rng> AssignTagAll(Func<int, Rng, string> connectionTag)
            {
                return (connections, index, rng) => AssignTagAllImpl(connections, connectionTag?.Invoke(index, rng) ?? "");
            }

            private static void AssignTagAllImpl(IEnumerable<Connection> connections, string connectionTag)
            {
                foreach (Connection connection in connections)
                {
                    connection.tag = connectionTag;
                }
            }
        }
    }
}
