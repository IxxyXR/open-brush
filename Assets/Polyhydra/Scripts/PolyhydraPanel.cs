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
    [NonSerialized] public VrUi.ShapeCategories CurrentShapeCategory;

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


    public void SetPanelButtonVisibility(VrUi.ShapeCategories shapeCategory)
    {
        var buttons = gameObject.GetComponentsInChildren<PolyhydraOptionButton>(true);

        switch (shapeCategory)
        {
            // All the shapeCategories that use the Uniform popup
            case VrUi.ShapeCategories.Archimedean:
            case VrUi.ShapeCategories.Platonic:
            case VrUi.ShapeCategories.Prisms:
            case VrUi.ShapeCategories.KeplerPoinsot:
                foreach (var button in buttons)
                {
                    switch (button.m_Command)
                    {
                        case SketchControlsScript.GlobalCommands.PolyhydraOpenShapeTypesPopup:
                        // case SketchControlsScript.GlobalCommands.PolyhydraConwayOpTypesPopup:
                        case SketchControlsScript.GlobalCommands.PolyhydraOpenUniformsPopup:
                            button.gameObject.SetActive(true);
                            break;

                        case SketchControlsScript.GlobalCommands.PolyhydraGridShapesPopup:
                        case SketchControlsScript.GlobalCommands.PolyhydraGridTypesPopup:
                        case SketchControlsScript.GlobalCommands.PolyhydraJohnsonTypesPopup:
                        case SketchControlsScript.GlobalCommands.PolyhydraOtherTypesPopup:
                            button.gameObject.SetActive(false);
                            break;
                    }
                }
                break;

            case VrUi.ShapeCategories.Grids:
                foreach (var button in buttons)
                {
                    switch (button.m_Command)
                    {
                        case SketchControlsScript.GlobalCommands.PolyhydraOpenShapeTypesPopup:
                        // case SketchControlsScript.GlobalCommands.PolyhydraConwayOpTypesPopup:
                        case SketchControlsScript.GlobalCommands.PolyhydraGridShapesPopup:
                        case SketchControlsScript.GlobalCommands.PolyhydraGridTypesPopup:
                            button.gameObject.SetActive(true);
                            break;

                        case SketchControlsScript.GlobalCommands.PolyhydraOpenUniformsPopup:
                        case SketchControlsScript.GlobalCommands.PolyhydraJohnsonTypesPopup:
                        case SketchControlsScript.GlobalCommands.PolyhydraOtherTypesPopup:
                            button.gameObject.SetActive(false);
                            break;
                    }
                }
                break;
            
            case VrUi.ShapeCategories.Other:
                foreach (var button in buttons)
                {
                    switch (button.m_Command)
                    {
                        case SketchControlsScript.GlobalCommands.PolyhydraOpenShapeTypesPopup:
                        // case SketchControlsScript.GlobalCommands.PolyhydraConwayOpTypesPopup:
                        case SketchControlsScript.GlobalCommands.PolyhydraOtherTypesPopup:
                            button.gameObject.SetActive(true);
                            break;

                        case SketchControlsScript.GlobalCommands.PolyhydraOpenUniformsPopup:
                        case SketchControlsScript.GlobalCommands.PolyhydraJohnsonTypesPopup:
                        case SketchControlsScript.GlobalCommands.PolyhydraGridShapesPopup:
                        case SketchControlsScript.GlobalCommands.PolyhydraGridTypesPopup:
                            button.gameObject.SetActive(false);
                            break;
                    }
                }
                break;
            
            case VrUi.ShapeCategories.Johnson:
                foreach (var button in buttons)
                {
                    switch (button.m_Command)
                    {
                        case SketchControlsScript.GlobalCommands.PolyhydraOpenShapeTypesPopup:
                        // case SketchControlsScript.GlobalCommands.PolyhydraConwayOpTypesPopup:
                        case SketchControlsScript.GlobalCommands.PolyhydraJohnsonTypesPopup:
                            button.gameObject.SetActive(true);
                            break;

                        case SketchControlsScript.GlobalCommands.PolyhydraOtherTypesPopup:
                        case SketchControlsScript.GlobalCommands.PolyhydraOpenUniformsPopup:
                        case SketchControlsScript.GlobalCommands.PolyhydraGridShapesPopup:
                        case SketchControlsScript.GlobalCommands.PolyhydraGridTypesPopup:
                            button.gameObject.SetActive(false);
                            break;
                    }
                }
                break;
            
            case VrUi.ShapeCategories.Waterman:
                foreach (var button in buttons)
                {
                    switch (button.m_Command)
                    {
                        case SketchControlsScript.GlobalCommands.PolyhydraOpenShapeTypesPopup:
                        // case SketchControlsScript.GlobalCommands.PolyhydraConwayOpTypesPopup:
                            button.gameObject.SetActive(true);
                            break;

                        case SketchControlsScript.GlobalCommands.PolyhydraJohnsonTypesPopup:
                        case SketchControlsScript.GlobalCommands.PolyhydraOtherTypesPopup:
                        case SketchControlsScript.GlobalCommands.PolyhydraOpenUniformsPopup:
                        case SketchControlsScript.GlobalCommands.PolyhydraGridShapesPopup:
                        case SketchControlsScript.GlobalCommands.PolyhydraGridTypesPopup:
                            button.gameObject.SetActive(false);
                            break;
                    }
                }
                break;
        
    }
    }
}
} // namespace TiltBrush