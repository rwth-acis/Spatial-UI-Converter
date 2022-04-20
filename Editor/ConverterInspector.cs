using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace i5.SpatialUIConverter {
    [CustomEditor(typeof(Converter))]
    public class ConverterInspector : Editor {

        private VisualElement inspectorVE;
        ////Used to update the inspector when click on the Converter asset.
        //private bool preserveAspectRatio;

        public override VisualElement CreateInspectorGUI() {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ConverterUtilities.ConverterPackageRootPath + "/Editor/UI Documents/ConverterInspector.uxml");
            inspectorVE = visualTree.CloneTree(); 
            Button openWindowButton = inspectorVE.Q<Button>(name: "openWindowButton");
            openWindowButton.clickable.activators.Clear();
            openWindowButton.RegisterCallback<MouseDownEvent, Converter>(OpenConverterWindow, (Converter)target);
            return inspectorVE;
        }

        private void OpenConverterWindow(MouseDownEvent e, Converter converter) {
            ConverterWindow.ShowWindow(converter);
        }
    }
}

