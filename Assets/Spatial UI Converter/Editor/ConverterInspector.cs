using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace SpatialUIConverter {
    [CustomEditor(typeof(Converter))]
    public class ConverterInspector : Editor {

        private VisualElement inspectorVE;
        ////Used to update the inspector when click on the Converter asset.
        //private bool preserveAspectRatio;

        public override VisualElement CreateInspectorGUI() {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Spatial UI Converter/Editor/ConverterInspector.uxml");
            inspectorVE = visualTree.CloneTree();       
            Button openWindowButton = inspectorVE.Q<Button>(name: "openWindowButton");
            openWindowButton.clickable.activators.Clear();
            openWindowButton.RegisterCallback<MouseDownEvent, Converter>(OpenConverterWindow, (Converter)target);
/*            openWindowButton.clickable.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            openWindowButton.clicked += ConverterWindow.ShowWindow;*/

            //inspectorVE.Q<PropertyField>(name: "preserveAspectRatio").RegisterValueChangeCallback(PreserveAspectRatioCallback);
            //PreserveAspectRatioCallback(SerializedPropertyChangeEvent.GetPooled());
            //uxmlVE.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Resources/tank_inspector_styles.uss"));
            return inspectorVE;
        }

        private void OpenConverterWindow(MouseDownEvent e, Converter converter) {
            ConverterWindow.ShowWindow(converter);
        }

/*        private void PreserveAspectRatioCallback(SerializedPropertyChangeEvent evt) {
            Debug.Log(preserveAspectRatio);
            if (evt.changedProperty != null) {
                preserveAspectRatio = evt.changedProperty.boolValue;
                if (evt.changedProperty.boolValue == true) {
                    inspectorVE.Q<PropertyField>(name: "backplateSize").style.display = DisplayStyle.None;
                    inspectorVE.Q<PropertyField>(name: "backplateWidth").style.display = DisplayStyle.Flex;
                }
                else {
                    inspectorVE.Q<PropertyField>(name: "backplateSize").style.display = DisplayStyle.Flex;
                    inspectorVE.Q<PropertyField>(name: "backplateWidth").style.display = DisplayStyle.None;
                }
            }
            //Used to update the inspector when click on the Converter asset.
            else {
                if (preserveAspectRatio) {
                    inspectorVE.Q<PropertyField>(name: "backplateSize").style.display = DisplayStyle.None;
                    inspectorVE.Q<PropertyField>(name: "backplateWidth").style.display = DisplayStyle.Flex;
                }
                else {
                    inspectorVE.Q<PropertyField>(name: "backplateSize").style.display = DisplayStyle.Flex;
                    inspectorVE.Q<PropertyField>(name: "backplateWidth").style.display = DisplayStyle.None;
                }
            }
        }*/
    }
}

