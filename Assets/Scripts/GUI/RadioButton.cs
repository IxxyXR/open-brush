// Copyright 2022 The Open Brush Authors
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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TiltBrush
{
    // TODO Refactor RadioButton, ToggleButton and OptionButton so that 
    // ToggleButton carries less baggage that it doesn't need from OptionButton.
    public class RadioButton : OptionButton
    {
        public bool m_IsToggledOn;
        public UnityEvent m_OnToggle;

        private RadioButton[] m_SiblingRadioButtons;

        protected override void Awake()
        {
            base.Awake();
            m_SiblingRadioButtons = transform.parent.GetComponentsInChildren<RadioButton>();
        }

        protected override bool IsButtonActive()
        {
            return m_IsToggledOn;
        }

        override protected void OnButtonPressed()
        {
            foreach (var sibling in m_SiblingRadioButtons)
            {
                sibling.m_IsToggledOn = false;
                sibling.UpdateVisuals();
            }
            m_IsToggledOn = true;
            m_OnToggle.Invoke();
        }
    }
} // namespace TiltBrush
