// Copyright 2023 The Open Brush Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Platforms;

#if UNITY_EDITOR
using System.Reflection;
#endif

namespace TiltBrush
{
    public enum ScriptCoordSpace
    {
        Default,
        Pointer,
        Canvas,
    }

    public static class LuaNames
    {

        // Special Tables

        public static string Parameters => "Parameters";
        public static string Settings => "Settings";
        public static string Colors => "Colors";
        public static string Brushes => "Brushes";

        // Special Methods
        public static string Main => "Main";
        public static string Start => "Start";
        public static string End => "End";
        public static string OnTriggerPressed => "OnTriggerPressed";
        public static string OnTriggerReleased => "OnTriggerReleased";
        public static string WhileTriggerPressed => "WhileTriggerPressed";
        public static string ScriptNameString => "_ScriptName";
        public static string IsExampleScriptBool => "_IsExampleScript";

    }

    public class LuaManager : MonoBehaviour
    {
        private FileWatcher m_FileWatcher;
        private static LuaManager m_Instance;
        private ApiManager apiManager;
        private static readonly string LuaFileSearchPattern = "*.lua";

#if UNITY_EDITOR
        // Used when called via MenuItem("Open Brush/API/Generate Lua Autocomplete File")
        public static List<string> AutoCompleteEntries;
#endif

        public List<ApiCategory> ApiCategories => Enum.GetValues(typeof(ApiCategory)).Cast<ApiCategory>().ToList();

        public enum ApiCategory
        {
            PointerScript = 0, // Modifies the pointer position on every frame
            ToolScript = 1, // A scriptable tool that can create strokes based on click/drag/release
            SymmetryScript = 2, // Generates copies of each new stroke with different transforms
            BackgroundScript = 3 // A general script that is called every frame
            // Scripts that modify brush settings for each new stroke (JitterScript?) Maybe combine with Pointerscript
            // Scripts that modify existing strokes (RepaintScript?)
            // Scriptable Brush mesh generation (BrushScript?)
            // Same as above but applies to the current selection with maybe some logic based on index within selection
        }

        [NonSerialized] public Dictionary<ApiCategory, SortedDictionary<string, Script>> Scripts;
        [NonSerialized] public Dictionary<ApiCategory, int> ActiveScripts;
        [NonSerialized] public bool PointerScriptsEnabled;
        [NonSerialized] public bool BackgroundScriptsEnabled;

        private List<string> m_ScriptPathsToUpdate;
        private Dictionary<string, Script> m_ActiveBackgroundScripts;

        private TransformBuffers m_TransformBuffers;
        private bool m_TriggerWasPressed;
        private static Dictionary<(Script OwnerScript, int ReferenceID), LuaTimer> m_Timers;

        public static LuaManager Instance => m_Instance;


        public struct ScriptTrTransform
        {
            public TrTransform Transform;
            public ScriptCoordSpace Space;

            public ScriptTrTransform(TrTransform transform, ScriptCoordSpace space)
            {
                Transform = transform;
                Space = space;
            }
        }

        public struct ScriptTrTransforms
        {
            public List<TrTransform> Transforms;
            public ScriptCoordSpace Space;

            public ScriptTrTransforms(List<TrTransform> transforms, ScriptCoordSpace space)
            {
                Transforms = transforms;
                Space = space;
            }
        }

        void Awake()
        {
            m_Instance = this;
            if (Directory.Exists(ApiManager.Instance.UserScriptsPath()))
            {
                m_FileWatcher = new FileWatcher(ApiManager.Instance.UserScriptsPath(), "*.lua");
                m_FileWatcher.NotifyFilter = NotifyFilters.LastWrite;
                m_FileWatcher.FileChanged += OnScriptsDirectoryChanged;
                m_FileWatcher.FileCreated += OnScriptsDirectoryChanged;
                // m_FileWatcher.FileDeleted += OnScriptsDirectoryChanged; TODO
                m_FileWatcher.EnableRaisingEvents = true;
            }
        }

        private void OnScriptsDirectoryChanged(object sender, FileSystemEventArgs e)
        {
            m_ScriptPathsToUpdate.Add(e.FullPath);
        }

        private void Start()
        {
            Init();
        }

        public void Init()
        {
            m_TransformBuffers = new TransformBuffers(512);
            m_ScriptPathsToUpdate = new List<string>();
            m_ActiveBackgroundScripts = new Dictionary<string, Script>();
            m_Timers = new Dictionary<(Script OwnerScript, int ReferenceID), LuaTimer>();
            UserData.RegisterAssembly();
            Script.GlobalOptions.Platform = new StandardPlatformAccessor();
            LuaCustomConverters.RegisterAll();
            InitScriptDataStructures();
            LoadExampleScripts();
            LoadUserScripts();
            var panel = (ScriptsPanel)PanelManager.m_Instance.GetPanelByType(BasePanel.PanelType.Scripts);
            panel.InitScriptUiNav();
            ConfigureScriptButton(ApiCategory.PointerScript);
            ConfigureScriptButton(ApiCategory.SymmetryScript);
            ConfigureScriptButton(ApiCategory.ToolScript);
        }

        private void Update()
        {
            // Consume the queue of scripts that the FileListener reports have changed
            foreach (var path in m_ScriptPathsToUpdate)
            {
                var scriptFilename = Path.GetFileName(path);
                var scriptName = Path.GetFileNameWithoutExtension(scriptFilename);
                var catMatch = TryGetCategoryFromScriptName(scriptName);
                if (catMatch.HasValue)
                {
                    var category = catMatch.Value;
                    LoadScriptFromPath(path);
                    if (catMatch != ApiCategory.BackgroundScript)
                    {
                        var activeScriptName = GetScriptNames(category)[ActiveScripts[category]];
                        ActiveScripts[category] = GetScriptNames(category).IndexOf(activeScriptName);
                        if (activeScriptName == scriptName.Substring(category.ToString().Length + 1))
                        {
                            InitScript(GetActiveScript(category));
                        }
                    }
                }
            }
            m_ScriptPathsToUpdate.Clear();

            var toRemove = new List<(Script OwnerScript, int ReferenceID)>();
            foreach (var kv in m_Timers)
            {
                var timer = kv.Value;
                if ((Time.time - timer.m_TimeLastRun >= timer.m_Interval) &&
                    (Time.time - timer.m_TimeAdded >= timer.m_Delay))
                {
                    timer.m_TimeLastRun = Time.time;
                    timer.m_Fn.Call();
                    timer.m_CallCount++;
                    if (timer.m_Repeats != -1 && timer.m_CallCount >= timer.m_Repeats)
                    {
                        toRemove.Add(kv.Key);
                    }
                }
            }
            foreach (var item in toRemove)
            {
                m_Timers.Remove(item);
            }
            if (BackgroundScriptsEnabled) CallActiveBackgroundScripts(LuaNames.Main);
        }

        public void InitScriptDataStructures()
        {
            Scripts = new Dictionary<ApiCategory, SortedDictionary<string, Script>>();
            ActiveScripts = new Dictionary<ApiCategory, int>();
            foreach (var category in ApiCategories)
            {
                Scripts[category] = new SortedDictionary<string, Script>();
                ActiveScripts[category] = 0;
            }
        }

        public void LoadUserScripts()
        {
            string[] files = Directory.GetFiles(ApiManager.Instance.UserScriptsPath(), LuaFileSearchPattern, SearchOption.AllDirectories);
            foreach (string scriptPath in files)
            {
                LoadScriptFromPath(scriptPath);
            }
        }

        private void LoadExampleScripts()
        {
            var exampleScripts = Resources.LoadAll("LuaScriptExamples", typeof(TextAsset));
            foreach (var asset in exampleScripts)
            {
                var luaFile = (TextAsset)asset;
                LoadScriptFromString(luaFile.name, luaFile.text, isExampleScript: true);
            }
        }

        private ApiCategory? TryGetCategoryFromScriptName(string scriptFilename)
        {
            foreach (ApiCategory category in ApiCategories)
            {
                var categoryName = category.ToString();
                if (scriptFilename.StartsWith(categoryName)) return category;
            }
            return null;
        }

        public void LogLuaError(Script script, string fnName, InterpreterException e)
        {
            string msg = e.DecoratedMessage;
            // chunk_1:(12,4-38): cannot access field count of userdata<TiltBrush.LayerApiWrapper>
            // Make the message more user friendly
            msg = msg.Replace("chunk_1:", "on line: ");
            // Replace the (line, char range) with just the line number itself
            msg = Regex.Replace(msg, @"(.+)\((\d+),.+\)(.+)", @"$1$2$3");
            if (string.IsNullOrEmpty(msg)) msg = e.Message;
            string errorMsg = $"Error in {script.Globals.Get(LuaNames.ScriptNameString).String}.{fnName} {msg}";
            ControllerConsoleScript.m_Instance.AddNewLine(errorMsg, true, true);
            Debug.LogError($"{errorMsg}\n\n{e.StackTrace}\n\n");
        }

        public void LogLuaMessage(string s)
        {
            ControllerConsoleScript.m_Instance.AddNewLine(s, false, true);
            Debug.Log(s);
        }

        private string LoadScriptFromPath(string path)
        {

            Stream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            string contents;
            using(var sr = new StreamReader(fileStream)) contents = sr.ReadToEnd();
            fileStream.Close();
            string filename = Path.GetFileName(path);
            return LoadScriptFromString(Path.GetFileNameWithoutExtension(filename), contents);
        }

        private string LoadScriptFromString(string scriptFilename, string contents, bool isExampleScript=false)
        {
            if (scriptFilename.StartsWith("__")) return null;
            Script script = new Script();
            string scriptName = null;
            try
            {
                script.DoString(contents);
            }
            catch (SyntaxErrorException e)
            {
                LogLuaError(script, "(Loading)", e);
                return null;
            }
            var catMatch = TryGetCategoryFromScriptName(scriptFilename);
            if (catMatch.HasValue)
            {
                var category = catMatch.Value;
                scriptName = scriptFilename.Substring(category.ToString().Length + 1);

                script.Globals[LuaNames.ScriptNameString] = scriptName;
                script.Globals[LuaNames.IsExampleScriptBool] = isExampleScript;

                Scripts[category][scriptName] = script;
                InitScriptOnce(script);
            }
            return scriptName;
        }

        public Vector3 GetPastBrushPos(int back)
        {
            return m_TransformBuffers.PastBrushTr(back).translation;
        }

        public Quaternion GetPastBrushRot(int back)
        {
            return m_TransformBuffers.PastBrushTr(back).rotation;
        }

        public Vector3 GetPastWandPos(int back)
        {
            return m_TransformBuffers.PastWandTr(back).translation;
        }

        public Quaternion GetPastWandRot(int back)
        {
            return m_TransformBuffers.PastWandTr(back).rotation;
        }

        public Vector3 GetPastHeadPos(int back)
        {
            return m_TransformBuffers.PastHeadTr(back).translation;
        }

        public Quaternion GetPastHeadRot(int back)
        {
            return m_TransformBuffers.PastHeadTr(back).rotation;
        }

        // Skips most error checking. Use for repeated redefinition.
        public void SetApiProperty(Script script, string cmd, object action)
        {
            var parts = cmd.Split(".");
            var tbl = script.Globals.Get(parts[0]);
            if (Equals(tbl, DynValue.Nil))
            {
                script.Globals.Set(parts[0], DynValue.NewTable(new Table(script)));
                tbl = script.Globals.Get(parts[0]);
            }
            tbl.Table[parts[1]] = action;
        }

        public void RegisterApiProperty(Script script, string cmd, object action)
        {
            _RegisterToApi(script, cmd, action);
#if UNITY_EDITOR
            if (Application.isEditor && AutoCompleteEntries!=null)
            {
                AutoCompleteEntries.Add($"{cmd} = nil");
            }
#endif
        }

        public void RegisterApiClass(Script script, string prefix, Type t)
        {
            script.Globals[prefix] = t;
#if UNITY_EDITOR
            if (Application.isEditor && AutoCompleteEntries!=null)
            {
                foreach (var prop in t.GetProperties()
                    .Where(x => x.GetGetMethod(true).IsStatic))
                {
                    AutoCompleteEntries.Add($"{prefix}.{prop.Name} = nil");
                }
                foreach (var prop in t.GetMethods().Where(m => !m.IsSpecialName)
                    .Where(x =>
                        x.Name.ToString() != "Equals" &&
                        x.Name.ToString() != "GetHashCode" &&
                        x.Name.ToString() != "GetType" &&
                        x.Name.ToString() != "ToString"))
                {
                    string paramNames = "";
                    var paramNameList = prop.GetParameters().Select(p => p.Name);
                    paramNames = string.Join(", ", paramNameList);
                    AutoCompleteEntries.Add($"function {prefix}.{prop.Name}({paramNames}) end");
                }
            }
#endif
        }

        public void RegisterApiCommand(Script script, string cmd, object action)
        {
            _RegisterToApi(script, cmd, action);
#if UNITY_EDITOR
            if (Application.isEditor && AutoCompleteEntries!=null)
            {
                string paramNames = "";
                Delegate d = action as Delegate;
                var paramNameList = d.Method.GetParameters().Select(p => p.Name);
                paramNames = string.Join(", ", paramNameList);
                AutoCompleteEntries.Add($"function {cmd}({paramNames}) end");
            }
#endif
        }

        public void _RegisterToApi(Script script, string cmd, object action, bool allowRedefine=false)
        {
            var parts = cmd.Split(".");
            var tbl = script.Globals.Get(parts[0]);
            if (Equals(tbl, DynValue.Nil))
            {
                script.Globals.Set(parts[0], DynValue.NewTable(new Table(script)));
                tbl = script.Globals.Get(parts[0]);
            }
            else if (tbl.Type!=DataType.Table)
            {
                Debug.LogError($"Probably a namespace clash with {cmd}. {parts[0]} is type {script.Globals.Get(parts[0]).Type}");
                return;
            }
            var entry = tbl.Table.Get(parts[1]);
            if (Equals(entry, DynValue.Nil) || allowRedefine)
            {
                tbl.Table[parts[1]] = action;
            }
            else
            {
                Debug.LogError($"Redefined {cmd}. {parts[1]} is already defined as {entry.Type}");
                return;
            }
        }

        public Script GetActiveScript(ApiCategory category)
        {
            string scriptName = GetScriptNames(category)[ActiveScripts[category]];
            return Scripts[category][scriptName];
        }

        private DynValue _CallScript(Script script, string fnName)
        {
            Closure activeFunction = script.Globals.Get(fnName).Function;
            DynValue result = DynValue.Nil;
            if (activeFunction != null)
            {
                try
                {
                    result = activeFunction.Call();
                }
                catch (InterpreterException  e)
                {
                    LogLuaError(script, fnName, e);
                }
            }

            // A bit hacky but always reset ForcePainting modes when calling "End".
            // (even if there is no "End" function defined)
            // It was proving fairly complex to reliably enforce this in the scripts themselves
            // It was hard to debug when it went wrong and it breaks the app in a confusing way.
            // So just do it here to be certain...
            if (fnName == LuaNames.End)
            {
                ApiMethods.ForcePaintingOn(false);
                ApiMethods.ForcePaintingOff(false);
                UnsetAllTimers(script);
            }

            return result;
        }

        private void CallActiveBackgroundScripts(string fnName)
        {
            foreach (var script in m_ActiveBackgroundScripts.Values)
            {
                _CallScript(script, fnName);
            }
        }

        private ScriptTrTransform CallActivePointerScript(string fnName)
        {
            var script = GetActiveScript(ApiCategory.PointerScript);
            DynValue result = _CallScript(script, fnName);
            var space = _GetSpaceForActiveScript(ApiCategory.PointerScript);
            var tr = TrTransform.identity;
            if (!Equals(result, DynValue.Nil)) tr = result.ToObject<TrTransform>();
            return new ScriptTrTransform(tr, space);
        }

        public ScriptTrTransforms CallActiveToolScript(string fnName)
        {
            var script = GetActiveScript(ApiCategory.ToolScript);
            var space = _GetSpaceForActiveScript(ApiCategory.ToolScript);
            var trs = _TrTransformsFromScript(fnName, script);
            return new ScriptTrTransforms(trs, space);
        }

        public ScriptTrTransforms CallActiveSymmetryScript(string fnName)
        {
            var script = GetActiveScript(ApiCategory.SymmetryScript);
            var space = _GetSpaceForActiveScript(ApiCategory.SymmetryScript);
            var trs = _TrTransformsFromScript(fnName, script);
            return new ScriptTrTransforms(trs, space);
        }

        private List<TrTransform> _TrTransformsFromScript(string fnName, Script script)
        {
            DynValue result = _CallScript(script, fnName);
            var trs = new List<TrTransform>();
            try
            {
                if (!Equals(result, DynValue.Nil)) trs = result.ToObject<List<TrTransform>>();
            }
            catch (InterpreterException  e)
            {
                LogLuaError(script, fnName, e);
            }
            return trs;
        }

        public DynValue GetSettingForActiveScript(ApiCategory category, string key)
        {
            var script = GetActiveScript(category);
            var settings = script.Globals.Get(LuaNames.Settings);
            return settings?.Table?.Get(key);
        }

        private ScriptCoordSpace _GetSpaceForActiveScript(ApiCategory category)
        {
            ScriptCoordSpace space = ScriptCoordSpace.Default;
            var spaceVal = GetSettingForActiveScript(category, "space");
            if (spaceVal != null)
            {
                Enum.TryParse(spaceVal.String, true, out space);
            }
            return space;
        }

        public void SetActiveScriptByName(ApiCategory category, string scriptName)
        {
            int index = GetScriptNames(category).IndexOf(scriptName);
            if (index != -1)
            {
                _SetActiveScript(category, index);
            }
        }

        public void ChangeCurrentScript(ApiCategory category, int increment)
        {
            if (Scripts[category].Count == 0) return;
            int index = (int)Mathf.Repeat(ActiveScripts[category] + increment, Scripts[category].Count);
            _SetActiveScript(category, index);
        }

        private void _SetActiveScript(ApiCategory category, int index)
        {
            var previousScript = GetActiveScript(category);
            // TODO Only call this if previousScript has been initialized and hasn't already been ended
            // Checking a method for null does this but only really as a side-effect
            if (previousScript.Globals.Get("draw.path").Function != null) _CallScript(previousScript, LuaNames.End);
            ActiveScripts[category] = index;
            var script = GetActiveScript(category);
            InitScript(script);
            ConfigureScriptButton(category);
        }

        public void ConfigureScriptButton(ApiCategory category)
        {
            var script = GetActiveScript(category);
            var scriptName = script.Globals.Get(LuaNames.ScriptNameString).String;
            var panel = (ScriptsPanel)PanelManager.m_Instance.GetPanelByType(BasePanel.PanelType.Scripts);
            string description = script.Globals.Get(LuaNames.Settings)?.Table?.Get("description")?.String;
            panel.ConfigureScriptButton(category, scriptName, description);
        }

        public void InitScriptOnce(Script script)
        {
            script.Options.DebugPrint = LogLuaMessage;
            RegisterApiClasses(script);
        }

        public void InitScript(Script script)
        {
            var configs = GetWidgetConfigs(script);
            foreach (var config in configs.Pairs)
            {
                if (config.Key.Type != DataType.String) continue;
                // Ensure the value is set
                GetOrSetWidgetCurrentValue(script, config);
            }
            _CallScript(script, LuaNames.Start);
        }

        public void RegisterApiClasses(Script script)
        {
            RegisterApiClass(script, "unityColor", typeof(ColorApiWrapper));
            RegisterApiClass(script, "unityMathf", typeof(MathfApiWrapper));
            RegisterApiClass(script, "unityQuaternion", typeof(QuaternionApiWrapper));
            // RegisterApiClass(script, "unityVector2", typeof(Vector2ApiWrapper));
            RegisterApiClass(script, "unityVector3", typeof(Vector3ApiWrapper));

            RegisterApiClass(script, "app", typeof(AppApiWrapper));
            RegisterApiClass(script, "brush", typeof(BrushApiWrapper));
            RegisterApiClass(script, "camerapath", typeof(CamerapathApiWrapper));
            RegisterApiClass(script, "draw", typeof(DrawApiWrapper));
            RegisterApiClass(script, "guides", typeof(GuidesApiWrapper));
            RegisterApiClass(script, "headset", typeof(HeadsetApiWrapper));
            RegisterApiClass(script, "images", typeof(ImageApiWrapper));
            RegisterApiClass(script, "layers", typeof(LayerApiWrapper));
            RegisterApiClass(script, "models", typeof(ModelApiWrapper));
            RegisterApiClass(script, "path", typeof(PathApiWrapper));
            RegisterApiClass(script, "selection", typeof(SelectionApiWrapper));
            RegisterApiClass(script, "sketch", typeof(SketchApiWrapper));
            RegisterApiClass(script, "spectator", typeof(SpectatorApiWrapper));
            RegisterApiClass(script, "strokes", typeof(StrokesApiWrapper));
            RegisterApiClass(script, "symmetry", typeof(SymmetryApiWrapper));
            RegisterApiClass(script, "timer", typeof(TimerApiWrapper));
            RegisterApiClass(script, "turtle", typeof(TurtleApiWrapper));
            RegisterApiClass(script, "user", typeof(UserApiWrapper));
            RegisterApiClass(script, "wand", typeof(WandApiWrapper));
            RegisterApiClass(script, "waveform", typeof(WaveformApiWrapper));
        }

        public void EnablePointerScript(bool enable)
        {
            PointerScriptsEnabled = enable;
            if (enable)
            {
                InitScript(GetActiveScript(ApiCategory.PointerScript));
            }
            else
            {
                CallActivePointerScript(LuaNames.End);
            }
        }

        public void EnableBackgroundScripts(bool enable)
        {
            BackgroundScriptsEnabled = enable;
            if (enable)
            {
                foreach (var script in m_ActiveBackgroundScripts.Values)
                {
                    InitScript(script);
                }
            }
            else
            {
                CallActiveBackgroundScripts(LuaNames.Main);
            }
        }

        public List<string> GetScriptNames(ApiCategory category)
        {
            if (Scripts != null && Scripts.Count > 0)
            {
                return Scripts[category].Keys.ToList();
            }
            else
            {
                return new List<string>();
            }
        }

        public Table GetWidgetConfigs(Script script)
        {
            var configs = script.Globals.Get(LuaNames.Parameters);
            return configs.IsNil() ? new Table(script) : configs.Table;
        }

        public void SetScriptParameterForActiveScript(ApiCategory category, string paramName, float paramValue)
        {
            var script = GetActiveScript(category);
            script.Globals.Set(paramName, DynValue.NewNumber(paramValue));
        }

        public float GetOrSetWidgetCurrentValue(Script script, TablePair config)
        {
            // Try and get the value from the script
            var val = script.Globals.Get(config.Key);
            // If it isn't set...
            if (val.Equals(DynValue.Nil))
            {
                // Get the default from the config entry
                val = config.Value.Table.Get("default");
                // Otherwise default to 0
                val = val.Equals(DynValue.Nil) ? DynValue.NewNumber(0) : val;
                // Set the value in the script
                script.Globals.Set(config.Key, val);
            }
            return (float)val.Number;
        }

        public void RecordPointerPositions(
            Vector3 brushPos_GS, Quaternion brushRot_GS,
            Vector3 wandPos_GS, Quaternion wandRot_GS,
            Vector3 headPos_GS, Quaternion headRot_GS)
        {
            m_TransformBuffers.AddBrushTr(App.Scene.ActiveCanvas.Pose.inverse * TrTransform.TR(brushPos_GS, brushRot_GS));
            m_TransformBuffers.AddWandTr(App.Scene.ActiveCanvas.Pose.inverse * TrTransform.TR(wandPos_GS, wandRot_GS));
            m_TransformBuffers.AddHeadTr(App.Scene.ActiveCanvas.Pose.inverse * TrTransform.TR(headPos_GS, headRot_GS));
        }

        public void ApplyPointerScript(Quaternion pointerRot, ref Vector3 pos_GS, ref Quaternion rot_GS)
        {
            ScriptTrTransform scriptTransformOutput = new ScriptTrTransform();
            bool scriptHasRun = false;

            if (InputManager.m_Instance.GetCommandDown(InputManager.SketchCommands.Activate))
            {
                ApiManager.Instance.StartUndo();
                scriptTransformOutput = CallActivePointerScript(LuaNames.OnTriggerPressed);
                m_TriggerWasPressed = true;
                scriptHasRun = true;
            }
            else if (InputManager.m_Instance.GetCommand(InputManager.SketchCommands.Activate))
            {
                scriptTransformOutput = CallActivePointerScript(LuaNames.WhileTriggerPressed);
                m_TriggerWasPressed = true;
                scriptHasRun = true;
            }
            else if (m_TriggerWasPressed)
            {
                scriptTransformOutput = CallActivePointerScript(LuaNames.OnTriggerPressed);
                m_TriggerWasPressed = false;
                scriptHasRun = true;
                ApiManager.Instance.EndUndo();
            }

            if (!scriptHasRun) return;

            switch (scriptTransformOutput.Space)
            {
                case ScriptCoordSpace.Default:
                case ScriptCoordSpace.Pointer:
                    var oldPos = pos_GS;
                    pos_GS = scriptTransformOutput.Transform.translation;
                    pos_GS = pointerRot * pos_GS;
                    pos_GS += oldPos;
                    rot_GS *= scriptTransformOutput.Transform.rotation;
                    break;
                case ScriptCoordSpace.Canvas:
                    var tr_CS = TrTransform.TR(
                        scriptTransformOutput.Transform.translation,
                        scriptTransformOutput.Transform.rotation
                    );
                    var tr_GS = App.Scene.Pose * tr_CS;
                    pos_GS = tr_GS.translation;
                    rot_GS = tr_GS.rotation;
                    break;
            }
        }

        public static void SetTimer(Closure fn, float interval, float delay, int repeats)
        {
            var key = (fn.OwnerScript, fn.ReferenceID);
            m_Timers[key] = new LuaTimer(fn, interval, delay, repeats);
        }

        public static void UnsetTimer(Closure fn)
        {
            var key = (fn.OwnerScript, fn.ReferenceID);
            m_Timers.Remove(key);
        }

        public static void UnsetAllTimers(Script script)
        {
            var toRemove = new List<(Script OwnerScript, int ReferenceID)>();
            foreach (var key in m_Timers.Keys)
            {
                if (key.OwnerScript == script)
                {
                    toRemove.Add(key);
                }
            }
            foreach (var item in toRemove)
            {
                m_Timers.Remove(item);
            }
        }

        public bool ToggleBackgroundScript(string scriptToToggle)
        {
            foreach (var scriptName in m_ActiveBackgroundScripts.Keys)
            {
                if (scriptToToggle == scriptName)
                {
                    m_ActiveBackgroundScripts.Remove(scriptToToggle);
                    return false;
                }
            }
            m_ActiveBackgroundScripts[scriptToToggle] = Scripts[ApiCategory.BackgroundScript][scriptToToggle];
            var script = Scripts[ApiCategory.BackgroundScript][scriptToToggle];
            InitScript(script);
            return true;
        }

        public bool CopyActiveScriptToUserScriptFolder(ApiCategory category)
        {
            var index = ActiveScripts[category];
            var scriptName = GetScriptNames(category)[index];
            return CopyScriptToUserScriptFolder(category, scriptName);
        }

        public bool CopyScriptToUserScriptFolder(ApiCategory category, string scriptName)
        {
            var originalFilename = $"{category}.{scriptName}";
            var newFilename = Path.Join(ApiManager.Instance.UserScriptsPath(), $"{originalFilename}.lua");
            if (!File.Exists(newFilename))
            {
                FileUtils.WriteTextFromResources($"LuaScriptExamples/{originalFilename}", newFilename);
                return true;
            }
            return false;
        }

        public bool IsBackgroundScriptActive(string scriptName)
        {
            return m_ActiveBackgroundScripts.ContainsKey(scriptName);
        }
    }
}