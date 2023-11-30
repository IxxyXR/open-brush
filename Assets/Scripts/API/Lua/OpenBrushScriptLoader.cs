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

using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using System.IO;
using UnityEngine;

namespace TiltBrush
{
    public class OpenBrushScriptLoader : ScriptLoaderBase
    {
        public override bool ScriptFileExists(string name)
        {
            return true;
            LuaManager.LogLuaMessage($"ScriptFileExists: {name}? {File.Exists(name)}");
            return File.Exists(name);
        }

        public override object LoadFile(string file, Table globalContext)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(@"local symmetryHueShift =  {}
if not Parameters then Parameters = {} end

Parameters[""hueShiftFrequency""] = {label=""Hue Shift Frequency"", type=""float"", min=0.1, max=6, default=1}
Parameters[""hueShiftAmount""] = {label=""Hue Shift Amount"", type=""float"", min=0, max=1, default=0.3}

function symmetryHueShift.generate(copies, initialHsv)
	Symmetry.ClearColors()
	if hueShiftAmount > 0 then
		for i = 0, copies - 1 do
			t = i / copies
			newHue = Waveform:Triangle(t, hueShiftFrequency) * hueShiftAmount
			newColor = Color.HsvToRgb(initialHsv.x + newHue, initialHsv.y, initialHsv.z)
			Symmetry.AddColor(newColor)
		end
		Brush.ForceNewStroke()
	end
end
return symmetryHueShift
");
            writer.Flush();
            stream.Position = 0;
            return stream;

            FileStream result = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            LuaManager.LogLuaMessage($"LoadFile: {file} CanRead? {result.CanRead}");
            return result;
        }
    }
}
