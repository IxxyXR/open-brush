using System;
using System.Collections.Generic;
using System.Linq;
using Conway;
using Johnson;
using TiltBrush;
using UnityEngine;
using UnityEngine.Serialization;
using Valve.Newtonsoft.Json.Utilities;
using Wythoff;


public class VrUiPoly : MonoBehaviour
{
    public bool GenerateSubmeshes = false;

    public PolyHydraEnums.ShapeTypes ShapeType;
    public PolyTypes UniformPolyType;
    public PolyHydraEnums.PolyTypeCategories UniformPolyTypeCategory;
    public PolyHydraEnums.JohnsonPolyTypes JohnsonPolyType;
    public PolyHydraEnums.OtherPolyTypes OtherPolyType;
    public PolyHydraEnums.GridTypes GridType;
    public PolyHydraEnums.GridShapes GridShape;
    public int PrismP;
    public int PrismQ;

    public Gradient colors;
    public float ColorRange;
    public float ColorOffset;

    public ConwayPoly _conwayPoly;
    public bool Rescale;
    private MeshFilter meshFilter;
    private Color[] previewColors;
    public Color MainColor;
    public float ColorBlend = 0.5f;
    public PolyHydraEnums.ColorMethods PreviewColorMethod;

    public Material SymmetryWidgetMaterial;

    public bool vertexGizmos;
    public bool faceGizmos;
    public bool edgeGizmos;
    public bool faceCenterGizmos;

    void Start()
    {
        Init();
    }

    void Init()
    {
        ColorSetup();
        meshFilter = gameObject.GetComponent<MeshFilter>();
        MakePolyhedron();
    }

    private void ColorSetup()
    {
        int numColors = 12; // What is the maximum colour index we want to support? 
        var colorButtons = FindObjectsOfType<CustomColorButton>();
        // Use custom colors if they are present
        if (colorButtons.Length > 1)
        {
            var colorSet = new List<Color>();
            while (colorSet.Count < numColors)
            {
                for (var i = 0; i < colorButtons.Length; i++)
                {
                    var color = colorButtons[i].CustomColor;
                    colorSet.Add(color);
                }
            }
            previewColors = colorSet.ToArray();
            // Custom color buttons are right to left so reverse them
            previewColors.Reverse();
            // Intuitively the first face role isn't often seen so move it to the end
            var firstColor = previewColors[0];
            previewColors = previewColors.Skip(1).ToArray();
            previewColors.Append(firstColor);
        }
        // Otherwise use the default palette
        else
        {
            previewColors = Enumerable.Range(0, numColors).Select(x => colors.Evaluate(((x / 8f) * ColorRange + ColorOffset) % 1)).ToArray();
        }
        previewColors = previewColors.Select(col => Color.Lerp(MainColor, col, ColorBlend)).ToArray();
    }

    public void UpdateColorBlend(float blend)
    {
        MainColor = PointerManager.m_Instance.LastChosenColor;
        ColorBlend = blend;
        RebuildPoly();
    }

    [Serializable]
    public struct ConwayOperator
    {
        public Ops opType;
        public FaceSelections faceSelections;
        public bool randomize;
        public float amount;
        public float amount2;
        public bool disabled;

        public ConwayOperator ClampAmount(PolyHydraEnums.OpConfig config, bool safe = false)
        {
            float min = safe ? config.amountSafeMin : config.amountMin;
            float max = safe ? config.amountSafeMax : config.amountMax;
            amount = Mathf.Clamp(amount, min, max);
            return this;
        }

        public ConwayOperator ClampAmount2(PolyHydraEnums.OpConfig config, bool safe = false)
        {
            float min = safe ? config.amount2SafeMin : config.amount2Min;
            float max = safe ? config.amount2SafeMax : config.amount2Max;
            amount2 = Mathf.Clamp(amount2, min, max);
            return this;
        }

        public ConwayOperator ChangeAmount(float val)
        {
            amount += val;
            return this;
        }
        public ConwayOperator ChangeAmount2(float val)
        {
            amount2 += val;
            return this;
        }
        public ConwayOperator ChangeOpType(int val)
        {
            opType += val;
            opType = (Ops)Mathf.Clamp(
                (int)opType, 1, Enum.GetNames(typeof(Ops)).Length - 1
            );
            return this;
        }
        public ConwayOperator ChangeFaceSelection(int val)
        {
            faceSelections += val;
            faceSelections = (FaceSelections)Mathf.Clamp(
                (int)faceSelections, 0, Enum.GetNames(typeof(FaceSelections)).Length - 1
            );
            return this;
        }

        public ConwayOperator SetDefaultValues(PolyHydraEnums.OpConfig config)
        {
            amount = config.amountDefault;
            amount2 = config.amount2Default;
            return this;
        }
    }
    public List<ConwayOperator> ConwayOperators;

    public void RebuildPoly()
    {
        Validate();
        PreviewColorMethod = (ShapeType == PolyHydraEnums.ShapeTypes.Waterman)
            ? PolyHydraEnums.ColorMethods.ByFaceDirection
            : PolyHydraEnums.ColorMethods.ByRole;
        MakePolyhedron();

        Mesh polyMesh;
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }
        if (Application.isPlaying)
        {
            polyMesh = GetComponent<MeshFilter>().mesh;
            meshFilter.mesh = polyMesh;
        }
        else
        {
            polyMesh = GetComponent<MeshFilter>().sharedMesh;
            meshFilter.sharedMesh = polyMesh;
        }
        PointerManager.m_Instance.SetSymmetryMode(PointerManager.m_Instance.CurrentSymmetryMode);
    }

    public void Validate()
    {

        ColorSetup();

        if (ShapeType == PolyHydraEnums.ShapeTypes.Uniform)
        {
            if (PrismP < 3) { PrismP = 3; }
            if (PrismP > 16) PrismP = 16;
            if (PrismQ > PrismP - 2) PrismQ = PrismP - 2;
            if (PrismQ < 2) PrismQ = 2;
        }

        // Control the amount variables to some degree
        for (var i = 0; i < ConwayOperators.Count; i++)
        {
            if (PolyHydraEnums.OpConfigs == null) continue;
            var op = ConwayOperators[i];
            if (PolyHydraEnums.OpConfigs[op.opType].usesAmount)
            {
                op.amount = Mathf.Round(op.amount * 1000) / 1000f;
                op.amount2 = Mathf.Round(op.amount2 * 1000) / 1000f;

                float opMin, opMax;
                if (SafeLimits)
                {
                    opMin = PolyHydraEnums.OpConfigs[op.opType].amountSafeMin;
                    opMax = PolyHydraEnums.OpConfigs[op.opType].amountSafeMax;
                }
                else
                {
                    opMin = PolyHydraEnums.OpConfigs[op.opType].amountMin;
                    opMax = PolyHydraEnums.OpConfigs[op.opType].amountMax;
                }
                if (op.amount < opMin) op.amount = opMin;
                if (op.amount > opMax) op.amount = opMax;
            }
            else
            {
                op.amount = 0;
            }
            ConwayOperators[i] = op;
        }
    }

    public bool SafeLimits;
    private ConwayPoly stashed;

    public Color GetFaceColor(int faceIndex)
    {
        return PolyMeshBuilder.CalcFaceColor(
            _conwayPoly,
            previewColors,
            PreviewColorMethod,
            faceIndex
        );
    }

    public void MakePolyhedron()
    {
        switch (ShapeType)
        {
            case PolyHydraEnums.ShapeTypes.Uniform:
                var wythoff = new WythoffPoly(UniformPolyType, PrismP, PrismQ);
                wythoff.BuildFaces();
                _conwayPoly = new ConwayPoly(wythoff);
                _conwayPoly = _conwayPoly.SitLevel();
                break;
            case PolyHydraEnums.ShapeTypes.Johnson:
                _conwayPoly = JohnsonPoly.Build(JohnsonPolyType, PrismP);
                break;
            case PolyHydraEnums.ShapeTypes.Grid:
                _conwayPoly = Grids.Grids.MakeGrid(GridType, GridShape, PrismP, PrismQ);
                break;
            case PolyHydraEnums.ShapeTypes.Waterman:
                _conwayPoly = WatermanPoly.Build(1f, PrismP, PrismQ);
                break;
            case PolyHydraEnums.ShapeTypes.Other:
                _conwayPoly = JohnsonPoly.BuildOther(OtherPolyType, PrismP, PrismQ);
                break;
        }
        _conwayPoly.basePolyhedraInfo = new ConwayPoly.BasePolyhedraInfo { P = PrismP, Q = PrismQ };

        foreach (var op in ConwayOperators.ToList())
        {
            if (op.disabled || op.opType == Ops.Identity) continue;
            _conwayPoly = ApplyOp(_conwayPoly, ref stashed, op);
            _conwayPoly.basePolyhedraInfo = new ConwayPoly.BasePolyhedraInfo { P = PrismP, Q = PrismQ };
        }

        var mesh = PolyMeshBuilder.BuildMeshFromConwayPoly(_conwayPoly, GenerateSubmeshes, previewColors, PreviewColorMethod);
        AssignFinishedMesh(mesh);
    }

    public void AssignFinishedMesh(Mesh mesh)
    {

        if (Rescale)
        {
            var size = mesh.bounds.size;
            var maxDimension = Mathf.Max(size.x, size.y, size.z);
            var scale = (1f / maxDimension) * 2f;
            if (scale > 0 && scale != Mathf.Infinity)
            {
                transform.localScale = new Vector3(scale, scale, scale);
            }
            else
            {
                Debug.LogError("Failed to rescale");
            }
        }

        if (meshFilter != null)
        {
            if (Application.isPlaying) { meshFilter.mesh = mesh; }
            else { meshFilter.sharedMesh = mesh; }
        }
    }

    public static ConwayPoly ApplyOp(ConwayPoly conway, ref ConwayPoly stash, ConwayOperator op)
    {

        float amount = op.amount;
        var opParams = new OpParams
        {
            valueA = amount,
            valueB = op.amount2,
            randomize = op.randomize,
            facesel = op.faceSelections
        };
        conway = conway.ApplyOp(op.opType, opParams);
        return conway;
    }
    void OnDrawGizmos()
    {

        if (_conwayPoly == null) return;
#if UNITY_EDITOR
        GizmoHelper.DrawGizmos(_conwayPoly, transform, vertexGizmos, faceGizmos, edgeGizmos, faceCenterGizmos, false, 0.3f);
#endif
    }
}