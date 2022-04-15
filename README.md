# Spatial-UI-Converter
This is an application that can convert 2D `VisualElement`-based UI to spatial UI with MRTK prefabs in Unity for Mixed Reality applications.
## Getting Started
### Introduction
The application is aimed to reduce the effort for developers on constructing spatial UIs since it is very time-consuming. With this application, one can first create 2D UIs using the UI Toolkit, which is very similar to the UI system in web development, and then using this application to convert it to 3D spatial UIs, which means one do not need to set the layout of the 3D prefabs. It supports most of [Controls](https://docs.unity3d.com/2022.2/Documentation/Manual/UIE-Controls.html) for runtime UIs. For each control type, the converter transforms it to a corresponding MRTK prefab (HoloLens 2 style). 

### Prerequisities 
 - Recommanded Unity 2020.3.24f1
 - [Microsoft Mixed Reality Toolkit v2.7.3](https://github.com/microsoft/MixedRealityToolkit-Unity/releases/tag/2.7.3)
 - [Unity UI Toolkit](https://docs.unity3d.com/2022.2/Documentation/Manual/UIToolkits.html) (already included)
 - [Unity UI Builder](https://docs.unity3d.com/2022.2/Documentation/Manual/UIBuilder.html) (recommanded for building `VisualElement`-based UI, included as of Unity 2021.1)

### Supported Controls
 - Button: PressableButtonHoloLens2
 - Toggle: PressableButtonHoloLens2Toggle (with its variants, depending on toggle types)
 - Label: UITextSelawik (with TextMeshPro instead of TextMesh)
 - Slider: PinchSlider
 - SliderInt: StepSlider
 - TextField: MRKeyboardInputField_TMP
 - ScrollView: [Scrolling Object Collection](https://docs.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/features/ux-building-blocks/scrolling-object-collection?view=mrtkunity-2021-05)
 - Foldout: [Object Collection](https://docs.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/features/ux-building-blocks/object-collection?view=mrtkunity-2021-05)
 - VisualElement as container (not any of other controls)

Due to technical limitations, Foldout is converted to an Object Collection that displays beside the converted UI. The place for Foldout is replaced by a button. By clicking it, one can open or close the Object Collection.

Especially, ListView, MinMaxSlider, IMGUIContainer, and Scroller are not supported. For ListView, you can directly use ScrollView, but you might need to write the Callbacks by yourself. For MinMaxSlider, you can use two text field. For Scroller, you can use ScrollView or Slider. IMGUI (Editor GUI) is not considered here.

## How To Use

### Create a Converter
The Converter is basically a `ScriptableObject`. You can use the menu item _Assets/Create/Spatial UI Converter/Converter_ to create it anywhere in your Assets folder. 

### Convert the 2D UI
First, you need to assign your UI Toolkit template (UXML) and possibly also style sheets (USS) on the Converter's inspector. There are also some other settings you can modify. For detailed information, you can read the tooltips by hovering on each item. Then, click on the `Open Converter Window` button, a window will be displayed. Your `VisualElement`-based UI is displayed there. At last, click the `Convert` button on the window, a GameObject called "Converted UI" will be created in the scene, and a result message will be displayed on the window. You might find some notice there and modify the converted UI accordingly.

## What You Should Notice
- The converter does not guarantee a suitable (position/scale) value on Z axis. 
- The converter does not guarantee a suitable font size for labels and text fields.
- Some controls have special cases that you need to take care of. If you are not familiar with them, please read the notice message carefully after you clicked the `Convert` button. It is displayed at the lower part of the converter window.
- The layout of the converted UI depends on the layout of your 2D UI in the converter window, not in the UI Builder. If you set some properties to "auto", make sure the layout in the converter window is what you really want.
- If you do not use the correct MRTK version, it will still work in most cases. However, some prefabs possibly cannot be found in some MRTK versions. If you are using a newer version, please write an issue.
