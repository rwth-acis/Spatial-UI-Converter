using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpatialUIConverter {

    public class ConverterWindow : EditorWindow {

        private static Converter converter;
        private TemplateContainer uiToConvert;

        public static void ShowWindow(Converter converter) {
            ConverterWindow.converter = converter;
            ConverterWindow wnd = GetWindow<ConverterWindow>();
            wnd.titleContent = new GUIContent("Spatial UI Converter");
        }

        public void OnEnable() {
/*            converter = AssetDatabase.LoadAssetAtPath<Converter>("Assets/Spatial UI Converter/Converter.asset");
            if (converter == null) {
                Debug.LogError("You haven't create a converter yet. Please create a Spatial UI Converter under using the \"Create \" menu ");
            }*/
        }

        public void CreateGUI() {

            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;
            VisualElement toConvertUIcontainer = root.Q<VisualElement>("uiContainer");

            // Import UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Spatial UI Converter/Runtime/UI Documents/ConverterWindow.uxml");
            var windowUI = visualTree.CloneTree();
            root.Add(windowUI);

            if (converter.UxmlToConvert != null) {
                uiToConvert = converter.UxmlToConvert.CloneTree();
                uiToConvert.name = "uiToConvert";
                if (converter.StyleSheets.Count > 0) {
                    foreach (StyleSheet uss in converter.StyleSheets) {
                        uiToConvert.styleSheets.Add(uss);
                    }
                }
                converter.UIToConvert = uiToConvert;

                windowUI.Q<VisualElement>(name: "uiToConvert").Add(uiToConvert);
            }
            else {
                Debug.LogError("The UXML to convert is not set in the converter");
            }
            
            // Register Callbacks
            Button convertButton = windowUI.Q<Button>(name: "convertButton");
            convertButton.clickable.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            convertButton.clicked += converter.Convert;
        }

    }
}
