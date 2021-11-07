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

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TiltBrush
{
    public class DuplicateSelectionWithMirrorCommand : BaseCommand
    {
        // This command stores a copy of the selection and a copy of the duplicate.
        private List<Stroke> m_SelectedStrokes;
        private List<GrabWidget> m_SelectedWidgets;

        private List<Stroke> m_DuplicatedStrokes;
        private List<GrabWidget> m_DuplicatedWidgets;

        private TrTransform m_OriginTransform;
        private IEnumerable<TrTransform> m_DuplicateTransforms;

        private bool m_DupeInPlace;

        public DuplicateSelectionWithMirrorCommand(IEnumerable<TrTransform> xfList, BaseCommand parent = null) : base(parent)
        {
            // Save selected and duplicated strokes.
            m_SelectedStrokes = SelectionManager.m_Instance.SelectedStrokes.ToList();
            m_DuplicatedStrokes = new List<Stroke>();
            m_DuplicateTransforms = xfList.ToList();
            
            // Save selected widgets.
            m_SelectedWidgets = SelectionManager.m_Instance.SelectedWidgets.ToList();

            // Save duplicated widgets and move them to their own group.
            m_DuplicatedWidgets = new List<GrabWidget>();

            int foo = 1;
            foreach (var duplicateXf in m_DuplicateTransforms)
            {
                // Assume any negative scale is a simple mirror and special-case it
                // This works as long as the only symmetry mode with a negative scale
                // is the standard single mirror
                // if (duplicateXf.scale < 0)
                // {
                //     // First duplicate with no transform
                //     DupAndTransform(new TrTransform());
                //     // Then flip using the xf as a mirror
                //     FlipSelectionCommand.FlipSelection(
                //         App.Scene.MainCanvas,
                //         m_DuplicatedStrokes,
                //         m_DuplicatedWidgets,
                //         new Plane(
                //             duplicateXf.rotation * Vector3.forward,
                //             duplicateXf.translation.magnitude)
                //     );
                //     continue;
                // }

                // Handle symmetry ops that are just rotations and translations
                Debug.Log($"[{foo}]  T: {duplicateXf.translation} R: {duplicateXf.rotation.eulerAngles} S: {duplicateXf.scale}");
                foo++;
                DupAndTransform(duplicateXf);
            }
            
            m_OriginTransform = SelectionManager.m_Instance.SelectionTransform;
            m_DuplicateTransforms = xfList;
            m_DupeInPlace = m_DuplicateTransforms.Count() > 0 && m_DuplicateTransforms.ElementAt(0) == m_OriginTransform;
        }
        private void DupAndTransform(TrTransform xf)
        {
            // Move duplicates of grouped strokes to their own groups.
            var oldGroupToNewGroup = new Dictionary<SketchGroupTag, SketchGroupTag>();

            foreach (var stroke in m_SelectedStrokes)
            {
                var newStroke = SketchMemoryScript.m_Instance.DuplicateStroke(stroke, App.Scene.MainCanvas, xf);
                if (newStroke.Group != SketchGroupTag.None)
                {
                    if (!oldGroupToNewGroup.ContainsKey(newStroke.Group))
                    {
                        oldGroupToNewGroup.Add(newStroke.Group, App.GroupManager.NewUnusedGroup());
                    }

                    newStroke.Group = oldGroupToNewGroup[newStroke.Group];
                }

                m_DuplicatedStrokes.Add(newStroke);
            }

            foreach (var widget in m_SelectedWidgets)
            {
                var duplicatedWidget = widget.Clone();
                if (widget.Group != SketchGroupTag.None)
                {
                    if (!oldGroupToNewGroup.ContainsKey(widget.Group))
                    {
                        oldGroupToNewGroup.Add(widget.Group, App.GroupManager.NewUnusedGroup());
                    }

                    duplicatedWidget.Group = oldGroupToNewGroup[widget.Group];
                }

                duplicatedWidget.LocalTransform = xf;
                m_DuplicatedWidgets.Add(duplicatedWidget);
            }
        }

        public override bool NeedsSave { get { return true; } }

        protected override void OnRedo()
        {
            // Deselect selected strokes.
            if (m_SelectedStrokes != null) SelectionManager.m_Instance.DeselectStrokes(m_SelectedStrokes);
            // Deselect selected widgets.
            if (m_SelectedWidgets != null) SelectionManager.m_Instance.DeselectWidgets(m_SelectedWidgets);

            // Place duplicated strokes.
            foreach (var stroke in m_DuplicatedStrokes)
            {
                switch (stroke.m_Type)
                {
                    case Stroke.Type.BrushStroke:
                        BaseBrushScript brushScript = stroke.m_Object.GetComponent<BaseBrushScript>();
                        if (brushScript) brushScript.HideBrush(false);
                        break;
                    case Stroke.Type.BatchedBrushStroke:
                        stroke.m_BatchSubset.m_ParentBatch.EnableSubset(stroke.m_BatchSubset);
                        break;
                    default:
                        Debug.LogError("Unexpected: redo NotCreated duplicate stroke");
                        break;
                }
                TiltMeterScript.m_Instance.AdjustMeter(stroke, up: true);
            }
            SelectionManager.m_Instance.RegisterStrokesInSelectionCanvas(m_DuplicatedStrokes);

            // Place duplicated widgets.
            for (int i = 0; i < m_DuplicatedWidgets.Count; ++i)
            {
                m_DuplicatedWidgets[i].RestoreFromToss();
            }
            SelectionManager.m_Instance.RegisterWidgetsInSelectionCanvas(m_DuplicatedWidgets);
            
        }

        protected override void OnUndo()
        {
            // Remove duplicated strokes.
            foreach (var stroke in m_DuplicatedStrokes)
            {
                switch (stroke.m_Type)
                {
                    case Stroke.Type.BrushStroke:
                        BaseBrushScript brushScript = stroke.m_Object.GetComponent<BaseBrushScript>();
                        if (brushScript) brushScript.HideBrush(true);
                        break;
                    case Stroke.Type.BatchedBrushStroke:
                        stroke.m_BatchSubset.m_ParentBatch.DisableSubset(stroke.m_BatchSubset);
                        break;
                    default:
                        Debug.LogError("Unexpected: undo NotCreated duplicate stroke");
                        break;
                }
                TiltMeterScript.m_Instance.AdjustMeter(stroke, up: false);
            }
            SelectionManager.m_Instance.DeregisterStrokesInSelectionCanvas(m_DuplicatedStrokes);

            // Remove duplicated widgets.
            foreach (var t in m_DuplicatedWidgets) t.Hide();
            SelectionManager.m_Instance.DeregisterWidgetsInSelectionCanvas(m_DuplicatedWidgets);

            // Reset the selection transform before we select strokes.
            SelectionManager.m_Instance.SelectionTransform = m_OriginTransform;

            // Select strokes.
            if (m_SelectedStrokes != null) SelectionManager.m_Instance.SelectStrokes(m_SelectedStrokes);
            if (m_SelectedWidgets != null) SelectionManager.m_Instance.SelectWidgets(m_SelectedWidgets);
            SelectionManager.m_Instance.UpdateSelectionWidget();
        }

        public override bool Merge(BaseCommand other)
        {
            if (!m_DupeInPlace)
            {
                return false;
            }

            // If we duplicated a selection in place (the stamp feature), subsequent movements of
            // the selection should get bundled up with this command as a child.
            MoveWidgetCommand move = other as MoveWidgetCommand;
            if (move != null)
            {
                if (m_Children.Count == 0)
                {
                    m_Children.Add(other);
                }
                else
                {
                    MoveWidgetCommand childMove = m_Children[0] as MoveWidgetCommand;
                    Debug.Assert(childMove != null);
                    return childMove.Merge(other);
                }
                return true;
            }
            return false;
        }
    }
} // namespace TiltBrush
