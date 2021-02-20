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
using System.Linq;
using TiltBrush;
using UnityEngine;


namespace TiltBrush {

public class PolyhydraPopUpWindow : PolyhydraPopUpWindowBase {
  
  override public void Init(GameObject rParent, string sText) {
    
    m_ColorBackground = m_Background.GetComponent<MeshRenderer>().material;
    base.Init(rParent, sText);
    
    VrUi.ShapeCategories[] cats = Enum.GetValues(typeof(VrUi.ShapeCategories)).Cast<VrUi.ShapeCategories>().ToArray();

    float buttonSpacing = m_ButtonWidth * 0.25f;
    float baseButtonLeftBuffer = m_ButtonWidth + buttonSpacing;
    float totalWindowWidth = transform.localScale.x;

    Vector3 vTransformedBase = transform.TransformPoint(m_BaseButtonOffset);
    
    for(int i = 0; i < cats.Length; i++)
    {
      
      GameObject rButton = (GameObject)Instantiate(ButtonPrefab);

      Vector3 vOffset = transform.right;
      vOffset *= (totalWindowWidth * -0.5f) + baseButtonLeftBuffer + ((m_ButtonWidth + buttonSpacing) * (i % 3f));
      vOffset.y += -0.15f * Mathf.FloorToInt(i / 3f);

      Vector3 vButtonPos = vTransformedBase + vOffset;
      rButton.transform.position = vButtonPos;

      Vector3 vButtonScale = rButton.transform.localScale;
      vButtonScale.Set(m_ButtonWidth, m_ButtonWidth, m_ButtonWidth);
      rButton.transform.localScale = vButtonScale;

      rButton.transform.rotation = transform.rotation;
      rButton.transform.parent = transform;

      Renderer rButtonRenderer = rButton.GetComponent<Renderer>();
      rButtonRenderer.material.mainTexture = Resources.Load<Texture2D>($"ShapeButtons/{(VrUi.ShapeCategories)i}");

      PolyTypeButton rButtonScript = rButton.GetComponent<PolyTypeButton>();
      rButtonScript.ShapeCategory = cats[i];
      rButtonScript.parentPopup = this;
      rButtonScript.SetDescriptionText(cats[i].ToString());
      rButtonScript.RegisterComponent();
    }
  }
}
}  // namespace TiltBrush
