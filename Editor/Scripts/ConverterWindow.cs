using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace i5.SpatialUIConverter {

    public class ConverterWindow : EditorWindow {

        private static Converter converter;
        private static VisualElement root;
        private TemplateContainer uiToConvert;

        public static void ShowWindow(Converter converter) {
            ConverterWindow.converter = converter;
            ConverterWindow wnd = GetWindow<ConverterWindow>();
            wnd.titleContent = new GUIContent(ConverterUtilities.ConverterWindowTitle);
        }

        public void CreateGUI() {

            // Each editor window contains a root VisualElement object
            root = rootVisualElement;

            // Import UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ConverterUtilities.ConverterPackageUIDocumentPath + "/ConverterWindow.uxml");
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
                uiToConvert = null;
                Debug.LogError("The UXML to convert is not set in the converter");
            }

            // Register Callbacks
            Button convertButton = windowUI.Q<Button>(name: "convertButton");
            convertButton.clickable.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            convertButton.clicked += converter.Convert;
        }

        public static void ShowResultNotice(List<string> noticeMessage) {
            Label result = root.Q<Label>(name: "resultLabel");
            result.text = noticeMessage[0];
            if (noticeMessage[0] == "Conversion Succeed") {
                result.style.color = Color.green;
            }
            else {
                result.style.color = Color.red;
            }
            Label notice = root.Q<Label>(name: "noticeLabel");
            notice.text = "";
            for (int i = 1; i < noticeMessage.Count; i++) {
                notice.text += noticeMessage[i] + "\n\n";
            }
        }
    }
}
