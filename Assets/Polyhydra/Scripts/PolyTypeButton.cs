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
using Wythoff;

namespace TiltBrush
{
    public class PolyTypeButton : BaseButton
    {
        public VrUi.ShapeCategories ShapeCategory;
        private VrUiPoly _Poly;
        [NonSerialized] public PopUpWindow parentPopup;

        override protected void Awake()
        {
            base.Awake();
            // TODO set this up manually or at least only do it once per panel
            _Poly = SketchControlsScript.m_Instance.gameObject.GetComponentInChildren<VrUiPoly>(true);
        }
        
        protected override void OnButtonPressed()
        {
            base.OnButtonPressed();
            Debug.Log($"OnButtonPressed: {ShapeCategory}");
            switch (ShapeCategory)
            {
                case VrUi.ShapeCategories.Platonic:
                    _Poly.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
                    _Poly.UniformPolyType = (PolyTypes) Uniform.Platonic[0].Index - 1;
                    break;
                case VrUi.ShapeCategories.Prisms:
                    _Poly.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
                    _Poly.UniformPolyType = (PolyTypes) Uniform.Prismatic[0].Index - 1;
                    break;
                case VrUi.ShapeCategories.Archimedean:
                    _Poly.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
                    _Poly.UniformPolyType = (PolyTypes) Uniform.Archimedean[0].Index - 1;
                    break;
                case VrUi.ShapeCategories.UniformConvex:
                    _Poly.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
                    _Poly.UniformPolyType = (PolyTypes) Uniform.Convex[0].Index - 1;
                    break;
                case VrUi.ShapeCategories.KeplerPoinsot:
                    _Poly.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
                    _Poly.UniformPolyType = (PolyTypes) Uniform.KeplerPoinsot[0].Index - 1;
                    break;
                case VrUi.ShapeCategories.UniformStar:
                    _Poly.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
                    _Poly.UniformPolyType = (PolyTypes) Uniform.Star[0].Index - 1;
                    break;
                case VrUi.ShapeCategories.Johnson:
                    _Poly.ShapeType = PolyHydraEnums.ShapeTypes.Johnson;
                    break;
                case VrUi.ShapeCategories.Waterman:
                    _Poly.ShapeType = PolyHydraEnums.ShapeTypes.Waterman;
                    break;
                case VrUi.ShapeCategories.Grids:
                    _Poly.ShapeType = PolyHydraEnums.ShapeTypes.Grid;
                    break;
                case VrUi.ShapeCategories.Other:
                    _Poly.ShapeType = PolyHydraEnums.ShapeTypes.Other;
                    break;
            }

            parentPopup.RequestClose();
            
            Debug.Log($"{_Poly.ShapeType}");
        }
    }
} // namespace TiltBrush