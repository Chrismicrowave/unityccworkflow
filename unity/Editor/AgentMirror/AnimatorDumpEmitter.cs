using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Emits AnimatorDump.json — FSM states, transitions, parameters for every AnimatorController.
/// Addresses pain #3 (state opacity): Animator goes from binary black-box to greppable JSON map.
/// </summary>
[InitializeOnLoad]
public static class AnimatorDumpEmitter
{
    static AnimatorDumpEmitter()
    {
        EditorApplication.delayCall += EmitNow;
    }

    /// <summary>Full emit — public so StableIdBootstrap can trigger on demand.</summary>
    public static void EmitNow()
    {
        try
        {
            AgentMirrorConfig.EnsureOutputDir();

            var dump = new Dictionary<string, ControllerDump>();

            string[] guids = AssetDatabase.FindAssets("t:AnimatorController");
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath)) continue;

                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
                if (controller == null) continue;

                var layerList = new List<LayerDump>();

                foreach (var layer in controller.layers)
                {
                    var stateMachine = layer.stateMachine;
                    if (stateMachine == null) continue;

                    string defaultStateName = stateMachine.defaultState != null
                        ? stateMachine.defaultState.name : "";

                    var stateDumps = new List<StateDump>();
                    foreach (var childState in stateMachine.states)
                    {
                        var state = childState.state;
                        var transitions = new List<TransitionDump>();

                        foreach (var t in state.transitions)
                        {
                            var conditions = new List<ConditionDump>();
                            foreach (var cond in t.conditions)
                            {
                                conditions.Add(new ConditionDump
                                {
                                    parameter = cond.parameter,
                                    mode      = cond.mode.ToString(),
                                    threshold = cond.threshold
                                });
                            }
                            transitions.Add(new TransitionDump
                            {
                                destination = t.destinationState != null ? t.destinationState.name : "(exit)",
                                hasExitTime = t.hasExitTime,
                                exitTime    = t.exitTime,
                                conditions  = conditions
                            });
                        }

                        string motionName = state.motion != null ? state.motion.name : "";
                        stateDumps.Add(new StateDump
                        {
                            name        = state.name,
                            speed       = state.speed,
                            isDefault   = state.name == defaultStateName,
                            motionName  = motionName,
                            transitions = transitions
                        });
                    }

                    // Parameters are per-controller, not per-layer — emit on first layer only
                    var paramList = new List<ParamDump>();
                    if (layerList.Count == 0)
                    {
                        foreach (var param in controller.parameters)
                        {
                            paramList.Add(new ParamDump
                            {
                                name         = param.name,
                                type         = param.type.ToString(),
                                defaultFloat = param.defaultFloat,
                                defaultInt   = param.defaultInt,
                                defaultBool  = param.defaultBool
                            });
                        }
                    }

                    layerList.Add(new LayerDump
                    {
                        name         = layer.name,
                        defaultState = defaultStateName,
                        states       = stateDumps,
                        parameters   = paramList
                    });
                }

                dump[assetPath] = new ControllerDump { layers = layerList };
            }

            AgentMirrorConfig.SafeWriteAllText(AgentMirrorConfig.AnimatorDumpPath, SerializeDump(dump));
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AgentMirror] AnimatorDumpEmitter failed: {ex.Message}");
        }
    }

    // ── Serialization ──────────────────────────────────────────────────────

    private static string SerializeDump(Dictionary<string, ControllerDump> dump)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        int ci = 0;
        foreach (var kv in dump)
        {
            sb.Append($"  \"{EscJ(kv.Key)}\": {{\"layers\":[");
            int li = 0;
            foreach (var layer in kv.Value.layers)
            {
                sb.Append("{");
                sb.Append($"\"name\":\"{EscJ(layer.name)}\",");
                sb.Append($"\"defaultState\":\"{EscJ(layer.defaultState)}\",");
                sb.Append("\"states\":[");
                int si = 0;
                foreach (var state in layer.states)
                {
                    sb.Append("{");
                    sb.Append($"\"name\":\"{EscJ(state.name)}\",");
                    sb.Append($"\"speed\":{state.speed},");
                    sb.Append($"\"isDefault\":{(state.isDefault ? "true" : "false")},");
                    sb.Append($"\"motionName\":\"{EscJ(state.motionName)}\",");
                    sb.Append("\"transitions\":[");
                    int ti = 0;
                    foreach (var tr in state.transitions)
                    {
                        sb.Append("{");
                        sb.Append($"\"destination\":\"{EscJ(tr.destination)}\",");
                        sb.Append($"\"hasExitTime\":{(tr.hasExitTime ? "true" : "false")},");
                        sb.Append($"\"exitTime\":{tr.exitTime},");
                        sb.Append("\"conditions\":[");
                        int cdi = 0;
                        foreach (var cond in tr.conditions)
                        {
                            sb.Append("{");
                            sb.Append($"\"parameter\":\"{EscJ(cond.parameter)}\",");
                            sb.Append($"\"mode\":\"{EscJ(cond.mode)}\",");
                            sb.Append($"\"threshold\":{cond.threshold}");
                            sb.Append("}");
                            if (++cdi < tr.conditions.Count) sb.Append(",");
                        }
                        sb.Append("]}");
                        if (++ti < state.transitions.Count) sb.Append(",");
                    }
                    sb.Append("]}");
                    if (++si < layer.states.Count) sb.Append(",");
                }
                sb.Append("],\"parameters\":[");
                int pi = 0;
                foreach (var p in layer.parameters)
                {
                    sb.Append("{");
                    sb.Append($"\"name\":\"{EscJ(p.name)}\",");
                    sb.Append($"\"type\":\"{EscJ(p.type)}\",");
                    sb.Append($"\"defaultFloat\":{p.defaultFloat},");
                    sb.Append($"\"defaultInt\":{p.defaultInt},");
                    sb.Append($"\"defaultBool\":{(p.defaultBool ? "true" : "false")}");
                    sb.Append("}");
                    if (++pi < layer.parameters.Count) sb.Append(",");
                }
                sb.Append("]}");
                if (++li < kv.Value.layers.Count) sb.Append(",");
            }
            sb.Append("]}");
            if (++ci < dump.Count) sb.Append(",");
            sb.AppendLine();
        }
        sb.Append("}");
        return sb.ToString();
    }

    private static string EscJ(string s) =>
        s == null ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    // ── Data types ──────────────────────────────────────────────────────────
    private class ControllerDump { public List<LayerDump> layers; }

    private class LayerDump
    {
        public string           name;
        public string           defaultState;
        public List<StateDump>  states;
        public List<ParamDump>  parameters;
    }

    private class StateDump
    {
        public string               name;
        public float                speed;
        public bool                 isDefault;
        public string               motionName;
        public List<TransitionDump> transitions;
    }

    private class TransitionDump
    {
        public string               destination;
        public bool                 hasExitTime;
        public float                exitTime;
        public List<ConditionDump>  conditions;
    }

    private class ConditionDump
    {
        public string parameter;
        public string mode;
        public float  threshold;
    }

    private class ParamDump
    {
        public string name;
        public string type;
        public float  defaultFloat;
        public int    defaultInt;
        public bool   defaultBool;
    }
}

/// <summary>Triggers AnimatorDumpEmitter when .controller files are imported.</summary>
public class AnimatorAssetPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (var path in importedAssets)
        {
            if (path.EndsWith(".controller", StringComparison.OrdinalIgnoreCase))
            {
                AnimatorDumpEmitter.EmitNow();
                return;
            }
        }
    }
}
