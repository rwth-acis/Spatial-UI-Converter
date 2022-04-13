using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

public class MyElement : EditorWindow {
    [MenuItem("Window/UI Toolkit/MyElement")]
    public static void ShowExample() {
        MyElement wnd = GetWindow<MyElement>();
        wnd.titleContent = new GUIContent("MyElement");
    }

    private void RegisterCallbacks() {
        var root = rootVisualElement;
        var button = root.Query<Button>("1").First();
        button.clickable.activators.Clear();
        button.RegisterCallback<MouseDownEvent>(OnMouseDown);

    }

    private void OnMouseDown(MouseDownEvent evt) {
        //evt.StopImmediatePropagation();
        //evt.StopPropagation();
        var root = rootVisualElement;
        var button = root.Query<Button>("1").First();
    }

    public void CreateGUI() {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Spatial UI Converter/UI Documents/MyElement.uxml");
        var ui = visualTree.CloneTree();
        root.Add(ui);

        // A stylesheet can be added to a VisualElement.
        // The style will be applied to the VisualElement and all of its children.
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Spatial UI Converter/UI Documents/MyElement.uss");
        root.styleSheets.Add(styleSheet);
        RegisterCallbacks();
    }

}