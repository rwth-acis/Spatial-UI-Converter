# Spatial-UI-Converter
This is an application that can convert 2D `VisualElement`-based UI to spatial UI with MRTK prefabs in Unity for Mixed Reality applications.
## Getting Started
### Introduction
The application is aimed to reduce the effort for developers on constructing spatial UIs since it is very time-consuming. With this application, one can first create 2D UIs using the UI Toolkit, which is very similar to the UI system in web development, and then using this application to convert it to 3D spatial UIs, which means one do not need to set the layout of the 3D prefabs. It supports most of [Controls](https://docs.unity3d.com/2022.2/Documentation/Manual/UIE-Controls.html) for runtime UIs. For each control type, the converter transforms it to a corresponding MRTK prefab (HoloLens 2 style). 

### Prerequisities 
 - Recommanded Unity 2020.3.24f1
 - [Microsoft Mixed Reality Toolkit v2.7.3](https://github.com/microsoft/MixedRealityToolkit-Unity/releases/tag/2.7.3)
 - [Unity UI Toolkit](https://docs.unity3d.com/2022.2/Documentation/Manual/UIToolkits.html) (already included)
 - [Unity UI Builder](https://docs.unity3d.com/2022.2/Documentation/Manual/UIBuilder.html) (recommanded for building `VisualElement`-based UI, already included)
 
 To make it possible to import MRTK automatically, you need to add the following scoped regestry in `Packages/manifest.json`:
 ```
 "scopedRegistries": [
  {
    "name": "Microsoft Mixed Reality",
    "url": "https://pkgs.dev.azure.com/aipmr/MixedReality-Unity-Packages/_packaging/Unity-packages/npm/registry/",
    "scopes": [
      "com.microsoft.mixedreality",
      "com.microsoft.spatialaudio"
    ]
  },
  ...some other scoped registries of your project
],
 ```

### Supported Controls
 - Button: PressableButtonHoloLens2
 - Toggle: PressableButtonHoloLens2Toggle (with its variants, depending on toggle types)
 - Label: UITextSelawik (with TextMeshPro instead of TextMesh)
 - Slider: PinchSlider
 - SliderInt: StepSlider
 - TextField: MRKeyboardInputField_TMP
 - ScrollView: [Scrolling Object Collection](https://docs.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/features/ux-building-blocks/scrolling-object-collection?view=mrtkunity-2021-05)
 - Foldout: [Object Collection](https://docs.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/features/ux-building-blocks/object-collection?view=mrtkunity-2021-05)
 - VisualElement (i.e. not any of other controls, you can also use an empty VisualElement to tune the layout.)

Due to technical limitations, Foldout is converted to an Object Collection that displays beside the converted UI. The place for Foldout is replaced by a button. By clicking it, one can open or close the Object Collection. You can set the cell size on the inspector of the converter. The original layout of UXML files will not be retained.

Especially, ListView, MinMaxSlider, IMGUIContainer, and Scroller are not supported. For ListView, you can directly use ScrollView, but you might need to write the Callbacks by yourself. For MinMaxSlider, you can use two text field. For Scroller, you can use ScrollView or Slider. IMGUIContainer is not considered here, since it is used for Editor GUI.

Besides, the application can automatically convert texts on Labels, Buttons, etc. It supports font size, color, alighment and style (bold/italic).

## How To Use

### Get the Package
You have two ways to install the package for your project:
- Download the package under "Release" directly on GitHub, and go to `Assets > Import Package > Custom Package` in Unity, and select the downloaded package.
- Open the Unity Package Manager under `Window > Package Manager` , click on the plus-button and choose "Add package from git url", and enter `https://github.com/rwth-acis/Spatial-UI-Converter.git#[version]`. For example, if you want to use the version v1.0.1, then replace `[version]` by `v1.0.1`. At last, confirm the download by clicking on the "add" button.

### Create a Converter
The Converter is basically a `ScriptableObject`. You can use the menu item _Assets/Create/Spatial UI Converter/Converter_ to create it anywhere in your Assets folder. 

### Convert the 2D UI
First, you need to assign your UI Toolkit template (UXML) and possibly also style sheets (USS) on the Converter's inspector. There are also some other settings you can modify. For detailed information, you can read the tooltips by hovering on each item. Then, click on the `Open Converter Window` button, a window will be displayed. Your `VisualElement`-based UI is displayed there. At last, click the `Convert` button on the window, a GameObject called "Converted UI" will be created in the scene, and a result message will be displayed on the window. You might find some notice there and modify the converted UI accordingly.

### Samples
There are one sample UXML file and its USS file under `i5 Spatial UI Converter/Sample` folder, you can use the converter in this folder to test it. The UXML file contains all supported Controls.

## What You Should Notice
- The converter does not guarantee a suitable (position/scale) value on Z axis. 
- The converter does not guarantee a suitable font size for labels and text fields.
- Some controls have special cases that you need to take care of. If you are not familiar with them, please read the notice message carefully after you clicked the `Convert` button. It is displayed at the lower part of the converter window.
- Scrolling Object Collection can cause problems on the Buttons' "IconAndText", which is a limitation of MRTK. You need to disable different type of icons, or it will not be displayed correctly.
- The layout of the converted UI depends on the layout of your 2D UI in the converter window, not in the UI Builder. If you set some properties to "auto", make sure the layout in the converter window is what you really want. You might need to adjust the window to get a proper layout.
- If you do not use the correct MRTK version, it will still work in most cases. However, some prefabs possibly cannot be found in some MRTK versions. If you are using a newer version, please write an issue.
