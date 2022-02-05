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

using System;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TiltBrush
{

    public class StrokeWarpTool : StrokeModificationTool
    {
        public float m_SpinSpeedAcceleration;
        public float m_MaxSpinSpeed;
        public float m_SpinSpeedDecay;
        private float m_SpinSpeedVel;
        private float m_SpinSpeed;
        private float m_SpinAmount;
        
        public enum WarpTypes
        {
            Jitter,
            Smooth,
            Push,
            Pull,
            Quantize,
            Noise,
            Twist,
            Script,
        }

        [NonSerialized] public WarpTypes CurrentWarpType;

        override protected void Awake()
        {
            base.Awake();

            PointerManager.m_Instance.OnPointerColorChange += UpdateMaterialColors;
            // Modify an instance of the materials, not the original materials
            m_ToolColdMaterial = Instantiate(m_ToolColdMaterial);
            m_ToolHotMaterial = Instantiate(m_ToolHotMaterial);
            m_ToolTransform.GetComponent<Renderer>().material = m_ToolColdMaterial;
            UpdateMaterialColors();
        }

        void UpdateMaterialColors()
        {
            m_ToolColdMaterial.color = PointerManager.m_Instance.PointerColor;
            m_ToolHotMaterial.SetColor("_BorderColor", PointerManager.m_Instance.PointerColor);
            m_ToolHotMaterial.SetColor("_BorderColor2", PointerManager.m_Instance.PointerColor);
        }

        override public void HideTool(bool bHide)
        {
            base.HideTool(bHide);
            if (m_SketchSurface.IsInFreePaintMode())
            {
                m_ToolTransform.GetComponent<Renderer>().enabled = !bHide;
            }
        }

        override public void IntersectionHappenedThisFrame()
        {
            InputManager.m_Instance.TriggerHaptics(InputManager.ControllerName.Brush, 0.05f);
        }

        override protected bool HandleIntersectionWithWidget(GrabWidget widget)
        {
            return false;
        }

        override protected void UpdateDetection()
        {
            // If we just went cold, reset our detection lists.
            // This is done before base.UpdateDetection() because our m_ToolWasHot flag is
            // updated in that call.
            if (m_ToolWasHot && !IsHot)
            {
                ClearGpuFutureLists();
            }

            base.UpdateDetection();
        }

        override protected void UpdateAudioVisuals()
        {
            bool bToolHot = IsHot;
            if (bToolHot != m_ToolWasHot)
            {
                m_ToolTransform.GetComponent<Renderer>().material =
                    bToolHot ? m_ToolHotMaterial : m_ToolColdMaterial;
            }

            RequestPlayAudio(bToolHot);

            if (IsHot)
            {
                m_SpinSpeedVel -= m_SpinSpeedAcceleration * Time.deltaTime;
                m_SpinSpeed = m_SpinSpeed + m_SpinSpeedVel * Time.deltaTime;
                if (m_SpinSpeed < -m_MaxSpinSpeed || m_SpinSpeed > m_MaxSpinSpeed)
                {
                    m_SpinSpeed = Mathf.Clamp(m_SpinSpeed, -m_MaxSpinSpeed, m_MaxSpinSpeed);
                    m_SpinSpeedVel = 0.0f;
                }
            }
            else
            {
                float speedDelta = m_SpinSpeedDecay * Time.deltaTime;
                m_SpinSpeed = Mathf.Sign(m_SpinSpeed) * Mathf.Max(Mathf.Abs(m_SpinSpeed) - speedDelta, 0.0f);
                m_SpinSpeedVel = 0.0f;
                m_BatchFilter = null;
            }
            m_SpinAmount += m_SpinSpeed * Time.deltaTime;
        }

        override protected void SnapIntersectionObjectToController()
        {
            if (m_LockToController)
            {
                Vector3 toolPos = InputManager.Brush.Geometry.ToolAttachPoint.position +
                    InputManager.Brush.Geometry.ToolAttachPoint.forward * m_PointerForwardOffset;
                m_ToolTransform.position = toolPos;

                Quaternion qTool = InputManager.Brush.Geometry.ToolAttachPoint.rotation *
                    Quaternion.AngleAxis(m_SpinAmount, Vector3.forward);
                m_ToolTransform.rotation = qTool;
            }
            else
            {
                transform.position = SketchSurfacePanel.m_Instance.transform.position;
                transform.rotation = SketchSurfacePanel.m_Instance.transform.rotation;
            }
        }

        override protected bool HandleIntersectionWithBatchedStroke(BatchSubset rGroup)
        {
            if (altSelect)
            {
                if (m_BatchFilter == null && rGroup.m_ParentBatch != null)
                    m_BatchFilter = rGroup.m_ParentBatch;

                if (!ReferenceEquals(m_BatchFilter, rGroup.m_ParentBatch))
                    return true;
            }
            else
                m_BatchFilter = null;

            
            int numPoints = rGroup.m_Stroke.m_ControlPoints.Length;
            for (var i = 0; i < numPoints; i++)
            {
                
                var cp = rGroup.m_Stroke.m_ControlPoints[i];
                TrTransform toolTrTransform_CS = Coords.AsCanvas[m_ToolTransform];
                var dist = Vector3.Distance(cp.m_Pos, toolTrTransform_CS.translation);
                
                if (dist < m_CurrentSize)
                {
                    float strength = Mathf.InverseLerp(0, m_CurrentSize, dist);
                    float toolEffectStrength;
                    switch (CurrentWarpType)
                    {
                        case WarpTypes.Jitter:
                            toolEffectStrength = 10f;
                            toolTrTransform_CS.translation += Random.insideUnitSphere * strength * toolEffectStrength;
                            break;
                        case WarpTypes.Noise:
                            toolEffectStrength = 10f;
                            toolTrTransform_CS.translation.y += .1f;
                            // toolTrTransform_CS.translation.y += Mathf.PerlinNoise(
                            //     toolTrTransform_CS.translation.x,
                            //     toolTrTransform_CS.translation.z) * strength * toolEffectStrength;
                            break;
                        case WarpTypes.Pull:
                            toolEffectStrength = 10f;
                            toolTrTransform_CS.translation = Vector3.Lerp(cp.m_Pos, toolTrTransform_CS.translation, strength * toolEffectStrength);
                            break;
                        case WarpTypes.Push:
                            toolEffectStrength = 10f;
                            toolTrTransform_CS.translation += Vector3.Lerp(toolTrTransform_CS.translation, cp.m_Pos, strength * toolEffectStrength);
                            break;
                        case WarpTypes.Quantize:
                            toolEffectStrength = 1f;
                            cp.m_Pos = Vector3.Lerp(cp.m_Pos, FreePaintTool.SnapToGrid(cp.m_Pos), strength * toolEffectStrength);
                            break;
                        case WarpTypes.Smooth:
                            toolEffectStrength = 1f;
                            var prev = rGroup.m_Stroke.m_ControlPoints[Mathf.Max(0, i - 1)];
                            var next = rGroup.m_Stroke.m_ControlPoints[Mathf.Min(i + 1, numPoints - 1)];
                            var prevThisDistance = Vector3.Distance(prev.m_Pos, cp.m_Pos);
                            var thisNextDistance = Vector3.Distance(cp.m_Pos, next.m_Pos);
                            var prevNextDistance = prevThisDistance + thisNextDistance;
                            var bias = Mathf.InverseLerp(prevThisDistance, thisNextDistance, prevNextDistance);
                            var midpoint = Vector3.Lerp(
                                prev.m_Pos,
                                next.m_Pos,
                                bias
                            );
                            cp.m_Pos = Vector3.Lerp(cp.m_Pos, midpoint, strength * toolEffectStrength);
                            break;
                        case WarpTypes.Twist:
                            cp.m_Orient = Quaternion.Slerp(cp.m_Orient, toolTrTransform_CS.rotation, strength);
                            break;
                        case WarpTypes.Script:
                            break;
                    }
                    rGroup.m_Stroke.m_ControlPoints[i] = cp;
                }
            }
            
            var didRepaint = SketchMemoryScript.m_Instance.MemorizeStrokeRepaint(
                rGroup.m_Stroke, false, false, false, force: true);
            
            didRepaint = true;
            
            if (didRepaint) { PlayModifyStrokeSound(); }
            return didRepaint;
        }

        override protected bool HandleIntersectionWithSolitaryObject(GameObject rGameObject)
        {
            var didRepaint = SketchMemoryScript.m_Instance.MemorizeStrokeRepaint(
                rGameObject, false, false, false, force: false);
            PlayModifyStrokeSound();
            if (didRepaint) { PlayModifyStrokeSound(); }
            return didRepaint;
        }

        override public void AssignControllerMaterials(InputManager.ControllerName controller)
        {
            if (controller == InputManager.ControllerName.Brush)
            {
                InputManager.Brush.Geometry.ShowBrushSizer();
            }
        }
        
    }
} // namespace TiltBrush
