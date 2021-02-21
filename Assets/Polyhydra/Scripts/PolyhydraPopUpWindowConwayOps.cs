// Copyright 2020 The Tilt Brush Authors
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
using System.Linq;
using Conway;
using UnityEngine;


namespace TiltBrush {

public class PolyhydraPopUpWindowConwayOps : PolyhydraPopUpWindowBase {
  
    [NonSerialized] protected int OpIndex;


    protected override string[] GetButtonList()
    {
      return Enum.GetNames(typeof(Ops)).ToArray();
    }

    protected override string GetButtonTexturePath(int i)
    {
      return $"IconButtons/{(Ops) i}";
    }

    public override void HandleButtonPress(int buttonIndex)
    {
      var op = ParentPanel.PolyhydraModel.ConwayOperators[OpIndex];
      op.opType = (Ops)buttonIndex;
      ParentPanel.PolyhydraModel.ConwayOperators[OpIndex] = op;
    }



}
}  // namespace TiltBrush
