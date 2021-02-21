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
using UnityEngine;

namespace TiltBrush
{

    public class PolyhydraPanel : BasePanel
    {
        
        [NonSerialized] public VrUiPoly PolyhydraModel;
        
        override public void InitPanel()
        {
            base.InitPanel();
            PolyhydraModel = gameObject.GetComponentInChildren<VrUiPoly>(true);
        }

        void Update()
        {
            BaseUpdate();
            PolyhydraModel.transform.parent.Rotate(1, 1, 1);
        }

        override public void ForceUpdatePanelVisuals()
        {
            base.ForceUpdatePanelVisuals();
        }

        public void ButtonPressed()
        {
            // TODO
            var command = SketchControlsScript.GlobalCommands.About;
            CreatePopUp(command, -1, -1, $"ButtonPressed", MakeOnPopUpClose());
            m_ActivePopUp.transform.localPosition += new Vector3(0, .2f, 0);
            m_EatInput = true;
        }

        Action MakeOnPopUpClose()
        {
            return delegate { };
        }
        
    }
} // namespace TiltBrush