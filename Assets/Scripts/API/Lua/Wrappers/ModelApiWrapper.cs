﻿using MoonSharp.Interpreter;
using UnityEngine;
namespace TiltBrush
{
    [MoonSharpUserData]
    public class ModelApiWrapper
    {

        public ModelWidget _ModelWidget;

        public ModelApiWrapper(ModelWidget widget)
        {
            _ModelWidget = widget;
        }

        public int index => WidgetManager.m_Instance.GetActiveWidgetIndex(_ModelWidget);

        public override string ToString()
        {
            return $"Model({_ModelWidget})";
        }

        public TrTransform transform
        {
            get =>  App.Scene.MainCanvas.AsCanvas[_ModelWidget.transform];
            set
            {
                value = App.Scene.Pose * value;
                App.Scene.ActiveCanvas.AsCanvas[_ModelWidget.transform] = value;
            }
        }

        public Vector3 position
        {
            get => transform.translation;
            set
            {
                var tr_CS = transform;
                var newTransform = TrTransform.T(value);
                newTransform = App.Scene.Pose * newTransform;
                tr_CS.translation = newTransform.translation;
                transform = tr_CS;
            }
        }

        public Quaternion rotation
        {
            get => transform.rotation;
            set
            {
                var tr_CS = transform;
                var newTransform = TrTransform.R(value);
                newTransform = App.Scene.Pose * newTransform;
                tr_CS.rotation = newTransform.rotation;
                transform = tr_CS;
            }
        }

        public float scale
        {
            get => transform.scale;
            set
            {
                var tr_CS = transform;
                var newTransform = TrTransform.S(value);
                newTransform = App.Scene.Pose * newTransform;
                tr_CS.scale = newTransform.scale;
                transform = tr_CS;
            }
        }

        public static ModelApiWrapper Import(string location) => new ModelApiWrapper(ApiMethods.ImportModel(location));
        public void Select() => ApiMethods.SelectModel(index);
    }
}