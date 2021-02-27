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
using TMPro;
using UnityEngine;
using UnityEngine.Events;


namespace TiltBrush {
  
  [Serializable] public class myFloatEvent : UnityEvent<float>{}
    public class PolyhydraSlider : BaseSlider
    {

        public float min;
        public float max;
        public TextMeshPro minText;
        public TextMeshPro maxText;
        public TextMeshPro valueText;
        public Type SliderType;
        
        [SerializeField] public myFloatEvent onUpdateValue;

        float remap(float s, float a1, float a2, float b1, float b2)
        {
          return b1 + (s-a1)*(b2-b1)/(a2-a1);
        }
        
        override protected void Awake() {
            base.Awake();
            m_CurrentValue = 0.5f;
            SetSliderPositionToReflectValue();
            minText.text = FormatValue(min);
            maxText.text = FormatValue(max);
            valueText.text = FormatValue(m_CurrentValue);
        }

        private string FormatValue(float val)
        {
          if (SliderType == typeof(int))
          {
            return Mathf.FloorToInt(val).ToString();
          }
          else
          {
            return (Mathf.Round(val*10)/10).ToString();
          }
        }

        override public void UpdateValue(float fValue) {
            //PointerManager.m_Instance.FreePaintPointerAngle = fValue * 90.0f;
            var val = remap(fValue, 0, 1, min, max);
            valueText.text = FormatValue(val);
            onUpdateValue.Invoke(val);
        }

        public override void ResetState() {
            base.ResetState();
            //SetAvailable(!App.VrSdk.VrControls.LogitechPenIsPresent());
        }
    }
}  // namespace TiltBrush