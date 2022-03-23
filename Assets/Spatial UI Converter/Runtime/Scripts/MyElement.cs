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
        /*        button.clickable.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
                button.clicked += () => Debug.Log(1);*/

        /*        button2.clickable.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
                button2.clicked += () => Debug.Log(2);*/
        //var root = treeAsset.CloneTree();
        //var button = root.Q<Button>(name: "1");

    }

    private void OnMouseDown(MouseDownEvent evt) {
        //evt.StopImmediatePropagation();
        //evt.StopPropagation();
        var root = rootVisualElement;
        var button = root.Query<Button>("1").First();
        Debug.Log(button.resolvedStyle.height);
        Debug.Log(button.resolvedStyle.width);
        Debug.Log(button.resolvedStyle.left);
        Debug.Log(button.resolvedStyle.right);
        Debug.Log(button.resolvedStyle.top);
        Debug.Log(button.resolvedStyle.bottom);
        Debug.Log(button.resolvedStyle.marginLeft);
        Debug.Log(button.resolvedStyle.marginRight);
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
    /*    public void CreateGUI() {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        VisualElement label = new Label("Hello World! From C#");
        root.Add(label);

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Spatial UI Builder/MyElement.uxml");
        VisualElement labelFromUXML = visualTree.Instantiate();
        root.Add(labelFromUXML);

        // A stylesheet can be added to a VisualElement.
        // The style will be applied to the VisualElement and all of its children.
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Spatial UI Builder/MyElement.uss");
        VisualElement labelWithStyle = new Label("Hello World! With Style");
        labelWithStyle.styleSheets.Add(styleSheet);
        root.Add(labelWithStyle);
    }*/

}