using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LastBell.Editor
{
    // Reflection keeps the optional MCP package out of LastBell.Editor's assembly references.
    internal static class LastBellMcpConnection
    {
        private const string ProjectRoot = "C:/Github/Test2/Client";
        private const string Endpoint = "http://127.0.0.1:8093";
        private static bool connecting;
        private static double nextAttempt;
        private static bool reportedFailure;

        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            if (!IsProjectEnabled()) return;
            // Own this Editor session's connection lifecycle; do not let the package
            // launch a server at another project's globally remembered endpoint.
            SessionState.SetBool("HttpAutoStartHandler.SessionInitialized", true);
            SessionState.EraseBool("HttpAutoStartHandler.ConnectPending");
            nextAttempt = EditorApplication.timeSinceStartup + 5;
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
        }

        private static bool IsProjectEnabled()
        {
            if (Application.isBatchMode) return false;
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/');
            if (!string.Equals(root, ProjectRoot, StringComparison.OrdinalIgnoreCase)) return false;
            string config = Path.Combine(root, "..", ".codex", "config.toml");
            return File.Exists(config) && File.ReadAllText(config).Contains(Endpoint + "/mcp");
        }

        private static void Update()
        {
            if (connecting || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.timeSinceStartup < nextAttempt) return;
            nextAttempt = EditorApplication.timeSinceStartup + 15;
            Connect();
        }

        [MenuItem("Last Bell/Tools/Connect Project MCP (8093)")]
        private static async void Connect()
        {
            if (connecting || !IsProjectEnabled()) return;
            connecting = true;
            try
            {
                Assembly assembly = Array.Find(AppDomain.CurrentDomain.GetAssemblies(),
                    value => value.GetName().Name == "MCPForUnity.Editor");
                if (assembly == null) return; // Package import may still be pending.
                Type configType = assembly.GetType("MCPForUnity.Editor.Services.EditorConfigurationCache", true);
                Type locatorType = assembly.GetType("MCPForUnity.Editor.Services.MCPServiceLocator", true);
                Type modeType = assembly.GetType("MCPForUnity.Editor.Services.Transport.TransportMode", true);
                object config = configType.GetProperty("Instance").GetValue(null);
                object transport = locatorType.GetProperty("TransportManager").GetValue(null);
                Type transportType = transport.GetType();
                object http = Enum.Parse(modeType, "Http");
                string currentUrl = (string)configType.GetProperty("HttpBaseUrl").GetValue(config);
                if (currentUrl == Endpoint && (bool)transportType.GetMethod("IsRunning").Invoke(transport, new[] { http }))
                    return;
                // This stops only this Editor's transport, never an external MCP server or Editor.
                if (currentUrl != Endpoint)
                    await (Task)transportType.GetMethod("StopAsync").Invoke(transport, new object[] { null });
                configType.GetMethod("SetUseHttpTransport").Invoke(config, new object[] { true });
                configType.GetMethod("SetHttpTransportScope").Invoke(config, new object[] { "local" });
                configType.GetMethod("SetHttpBaseUrl").Invoke(config, new object[] { Endpoint });
                bool connected = await (Task<bool>)transportType.GetMethod("StartAsync").Invoke(transport, new[] { http });
                if (connected)
                {
                    reportedFailure = false;
                    Debug.Log("[Last Bell MCP] Connected to " + Endpoint + " for " + ProjectRoot);
                }
                else if (!reportedFailure)
                {
                    reportedFailure = true;
                    Debug.LogWarning("[Last Bell MCP] Start Tools/MCP/start-lastbell-mcp.ps1. Retrying every 15 seconds.");
                }
            }
            catch (Exception error)
            {
                if (!reportedFailure) Debug.LogWarning("[Last Bell MCP] " + error.GetBaseException().Message);
                reportedFailure = true;
            }
            finally { connecting = false; }
        }
    }
}
