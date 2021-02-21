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
using Wythoff;


namespace TiltBrush {

public class PolyhydraPopUpWindowShapeTypes : PolyhydraPopUpWindowBase
{

    protected override string[] GetButtonList()
    {
        return Enum.GetNames(typeof(VrUi.ShapeCategories)).ToArray();
    }

  protected override string GetButtonTexturePath(int i)
  {
      return $"ShapeTypeButtons/{(VrUi.ShapeCategories) i}";
  }

  public override void HandleButtonPress(int buttonIndex)
  {
      
    var shapeCategory = (VrUi.ShapeCategories) buttonIndex;
    ParentPanel.CurrentShapeCategory = shapeCategory;
    switch (shapeCategory)
    {
        case VrUi.ShapeCategories.Platonic:
            ParentPanel.PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
            ParentPanel.PolyhydraModel.UniformPolyType = (PolyTypes) Uniform.Platonic[0].Index - 1;
            break;
        case VrUi.ShapeCategories.Prisms:
            ParentPanel.PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
            ParentPanel.PolyhydraModel.UniformPolyType = (PolyTypes) Uniform.Prismatic[0].Index - 1;
            break;
        case VrUi.ShapeCategories.Archimedean:
            ParentPanel.PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
            ParentPanel.PolyhydraModel.UniformPolyType = (PolyTypes) Uniform.Archimedean[0].Index - 1;
            break;
        // case VrUi.ShapeCategories.UniformConvex:
        //     ParentPanel.PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
        //     ParentPanel.PolyhydraModel.UniformPolyType = (PolyTypes) Uniform.Convex[0].Index - 1;
        //     break;
        case VrUi.ShapeCategories.KeplerPoinsot:
            ParentPanel.PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
            ParentPanel.PolyhydraModel.UniformPolyType = (PolyTypes) Uniform.KeplerPoinsot[0].Index - 1;
            break;
        // case VrUi.ShapeCategories.UniformStar:
        //     ParentPanel.PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
        //     ParentPanel.PolyhydraModel.UniformPolyType = (PolyTypes) Uniform.Star[0].Index - 1;
        //     break;
        case VrUi.ShapeCategories.Johnson:
            ParentPanel.PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Johnson;
            break;
        case VrUi.ShapeCategories.Waterman:
            ParentPanel.PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Waterman;
            break;
        case VrUi.ShapeCategories.Grids:
            ParentPanel.PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Grid;
            break;
        case VrUi.ShapeCategories.Other:
            ParentPanel.PolyhydraModel.ShapeType = PolyHydraEnums.ShapeTypes.Other;
            break;
    }

    ParentPanel.SetPanelButtonVisibility(shapeCategory);

  }

}
}  // namespace TiltBrush
