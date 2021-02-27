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

public class PolyhydraPanel : BasePanel
{
    
    [NonSerialized] public VrUiPoly PolyhydraModel;
    [NonSerialized] public VrUi.ShapeCategories CurrentShapeCategory;

    public PolyhydraSlider SliderP;
    public PolyhydraSlider SliderQ;


    override public void InitPanel()
    {
        base.InitPanel();
        PolyhydraModel = gameObject.GetComponentInChildren<VrUiPoly>(true);
        SetSliderConfiguration();
        SetPanelButtonVisibility();
    }

    public void HandleSliderP(float value)
    {
      PolyhydraModel.PrismP = Mathf.FloorToInt(value);
      RebuildPoly();
    }

    public void HandleSliderQ(float value)
    {
      PolyhydraModel.PrismQ = Mathf.FloorToInt(value);
      RebuildPoly();
    }

    public void RebuildPoly()
    {
      PolyhydraModel.Validate();
      PolyhydraModel.PreviewColorMethod = (PolyhydraModel.ShapeType == PolyHydraEnums.ShapeTypes.Waterman)
        ? PolyHydraEnums.ColorMethods.ByFaceDirection
        : PolyHydraEnums.ColorMethods.ByRole;
      PolyhydraModel.MakePolyhedron();
    }
    
    void Update()
    {
        BaseUpdate();
        PolyhydraModel.transform.parent.Rotate(1, 1, 1);
    }


    public void SetPanelButtonVisibility()
    {
        var buttons = gameObject.GetComponentsInChildren<PolyhydraOptionButton>(true);

        switch (CurrentShapeCategory)
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

    public void ConfigureGeometry()
    {
        switch (CurrentShapeCategory)
        {
            case VrUi.ShapeCategories.Platonic:
                PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
                PolyhydraModel.UniformPolyType = (PolyTypes) Uniform.Platonic[0].Index - 1;
                break;
            case VrUi.ShapeCategories.Prisms:
                PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
                PolyhydraModel.UniformPolyType = (PolyTypes) Uniform.Prismatic[0].Index - 1;
                break;
            case VrUi.ShapeCategories.Archimedean:
                PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
                PolyhydraModel.UniformPolyType = (PolyTypes) Uniform.Archimedean[0].Index - 1;
              break;
            // case VrUi.ShapeCategories.UniformConvex:
            //     PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
            //     PolyhydraModel.UniformPolyType = (PolyTypes) Uniform.Convex[0].Index - 1;
            //     break;
            case VrUi.ShapeCategories.KeplerPoinsot:
                PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
                PolyhydraModel.UniformPolyType = (PolyTypes) Uniform.KeplerPoinsot[0].Index - 1;
                break;
            // case VrUi.ShapeCategories.UniformStar:
            //     PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
            //     PolyhydraModel.UniformPolyType = (PolyTypes) Uniform.Star[0].Index - 1;
            //     break;
            case VrUi.ShapeCategories.Johnson:
                PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Johnson;
                break;
            case VrUi.ShapeCategories.Waterman:
                PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Waterman;
                break;
            case VrUi.ShapeCategories.Grids:
                PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Grid;
                break;
            case VrUi.ShapeCategories.Other:
                PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Other;
                break;
        }
      
    }

    public void SetSliderConfiguration()
    {
        switch (CurrentShapeCategory)
        {
            case VrUi.ShapeCategories.Platonic:
              
                SliderP.gameObject.SetActive(false);
                SliderQ.gameObject.SetActive(false);
                break;
            
            case VrUi.ShapeCategories.Prisms:
              
                SliderP.Min = 3;
                SliderP.Max = 16;
                SliderQ.Min = 2;
                SliderQ.Max = 3;
                SliderP.SliderType = SliderTypes.Int;
                SliderQ.SliderType = SliderTypes.Int;

                switch (PolyhydraModel.UniformPolyType)
                {
                  case PolyTypes.Polygonal_Prism:
                  case PolyTypes.Polygonal_Antiprism:
                    SliderP.gameObject.SetActive(true);
                    SliderQ.gameObject.SetActive(false);
                    SliderP.SetDescriptionText("Sides");
                    break;
                  case PolyTypes.Polygrammic_Prism:
                  case PolyTypes.Polygrammic_Antiprism:
                  case PolyTypes.Polygrammic_Crossed_Antiprism:
                    SliderP.gameObject.SetActive(true);
                    SliderQ.gameObject.SetActive(true);
                    SliderP.SetDescriptionText("Sides");
                    SliderQ.SetDescriptionText("Q");
                    break;
                }

                break;

            case VrUi.ShapeCategories.Archimedean:
              
                SliderP.gameObject.SetActive(false);
                SliderQ.gameObject.SetActive(false);
                SliderP.SliderType = SliderTypes.Int;
                SliderQ.SliderType = SliderTypes.Int;
                break;
            
            // case VrUi.ShapeCategories.UniformConvex:
            //     PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
            //     PolyhydraModel.UniformPolyType = (PolyTypes) Uniform.Convex[0].Index - 1;
            //     break;
            
            case VrUi.ShapeCategories.KeplerPoinsot:
              
                SliderP.gameObject.SetActive(false);
                SliderQ.gameObject.SetActive(false);
                SliderP.SliderType = SliderTypes.Int;
                SliderQ.SliderType = SliderTypes.Int;
                break;

            // case VrUi.ShapeCategories.UniformStar:
            //     PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
            //     PolyhydraModel.UniformPolyType = (PolyTypes) Uniform.Star[0].Index - 1;
            //     break;
            
            case VrUi.ShapeCategories.Johnson:
              
                SliderP.gameObject.SetActive(true);
                SliderQ.gameObject.SetActive(false);
                SliderP.SetDescriptionText("Sides");
                SliderP.Min = 3;
                SliderP.Max = 16;
                SliderP.SliderType = SliderTypes.Int;
                SliderQ.SliderType = SliderTypes.Int;
                break;
            
            case VrUi.ShapeCategories.Waterman:
              
                SliderP.gameObject.SetActive(true);
                SliderQ.gameObject.SetActive(true);
                SliderP.SetDescriptionText("Root");
                SliderQ.SetDescriptionText("C");
                SliderP.Min = 1;
                SliderP.Max = 80;
                SliderQ.Min = 1;
                SliderQ.Max = 7;
                SliderP.SliderType = SliderTypes.Int;
                SliderQ.SliderType = SliderTypes.Int;
                break;
            
            case VrUi.ShapeCategories.Grids:
              
                SliderP.gameObject.SetActive(true);
                SliderQ.gameObject.SetActive(true);
                SliderP.SetDescriptionText("Width");
                SliderQ.SetDescriptionText("Depth");
                SliderP.Min = 1;
                SliderP.Max = 16;
                SliderQ.Min = 1;
                SliderQ.Max = 16;
                SliderP.SliderType = SliderTypes.Int;
                SliderQ.SliderType = SliderTypes.Int;
                break;
            
            case VrUi.ShapeCategories.Other:
              
              SliderP.SliderType = SliderTypes.Int;
              SliderQ.SliderType = SliderTypes.Int;
              
              switch (PolyhydraModel.OtherPolyType)
              {
                case PolyHydraEnums.OtherPolyTypes.Polygon:
                  SliderP.gameObject.SetActive(true);
                  SliderQ.gameObject.SetActive(false);
                  SliderP.Min = 3;
                  SliderP.Max = 16;
                  SliderP.SetDescriptionText("Sides");
                  break;
                case PolyHydraEnums.OtherPolyTypes.GriddedCube:
                  SliderP.gameObject.SetActive(true);
                  SliderQ.gameObject.SetActive(true);
                  SliderP.Min = 1;
                  SliderP.Max = 10;
                  SliderQ.Min = 1;
                  SliderQ.Max = 10;
                  SliderP.SetDescriptionText("X Resolution");
                  SliderP.SetDescriptionText("Y Resolution");
                  break;
                case PolyHydraEnums.OtherPolyTypes.UvSphere:
                case PolyHydraEnums.OtherPolyTypes.UvHemisphere:
                  SliderP.gameObject.SetActive(true);
                  SliderQ.gameObject.SetActive(true);
                  SliderP.Min = 1;
                  SliderP.Max = 16;
                  SliderQ.Min = 1;
                  SliderQ.Max = 16;
                  SliderP.SetDescriptionText("Sides");
                  SliderQ.SetDescriptionText("Slices");
                  break;
                default:
                  SliderP.gameObject.SetActive(false);
                  SliderQ.gameObject.SetActive(false);
                  break;
              }

              break;
        }
        SliderP.UpdateValue(SliderP.GetCurrentValue());
        SliderQ.UpdateValue(SliderQ.GetCurrentValue());
    }
}
} // namespace TiltBrush