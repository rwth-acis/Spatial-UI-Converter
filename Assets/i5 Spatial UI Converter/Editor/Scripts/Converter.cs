using Microsoft.MixedReality.Toolkit.Experimental.UI;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Input.Utilities;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace i5.SpatialUIConverter {
    [CreateAssetMenu(menuName = "Spatial UI Converter/Converter")]
    public class Converter : ScriptableObject {

        //Define the size of base prefabs for better readability. We only consider two-dimensional size (X and Y).
        private static class BaseSize {
            //Default size of Unity GameObject
            public static readonly Vector2 EmptyGameObject = new Vector2(100f, 100f);
            public static readonly Vector2 Backplate = new Vector2(10f, 10f);
            //The base size of label is calculated by using the Width and Height of the "Text" child of the prefab, which is 500 and 200, respectively.
            //And we can see that 500 represents to 25cm, so 200 represents to 10cm.
            public static readonly Vector2 Label = new Vector2(25f, 10f); 
            public static readonly Vector2 Button = new Vector2(3.2f, 3.2f);
            public static readonly Vector2 Toggle = new Vector2(3.2f, 3.2f);
            //The X axis of BaseSize.Slider only corresponds to the slider part of the VisualElement "Slider", not the Label or TextField parts.
            public static readonly Vector2 Slider = new Vector2(25f, 4f);
            public static readonly Vector2 TextField = new Vector2(8f, 1.5f);
            //The size of one cell is 25cm * 25cm.
            public static readonly Vector2 ScrollingObjectCollection = new Vector2(25f, 25f);
        }

        private enum ConversionResult {
            Successful,
            Failed
        }

        private enum ToggleType {
            CheckBox,
            Button,
            Radio,
            Switch
        }

        private enum ScrollViewDirection {
            Vertical,
            Horizontal
        }

        #region Serializable Fields

        [Header("Settings")]
        [Tooltip("The UXML file to be converted.")]      
        [SerializeField] private VisualTreeAsset uxmlToConvert;
        [Tooltip("The USS file to the above UXML file.")]
        [SerializeField] private List<StyleSheet> styleSheets;
        [Tooltip("The type of the converted \"Toggle\", it only defines the visual themes. If your toggle is originally toggled, remember to set the \"IsToggled\" property in the \"Interactable\" manually.")]
        [SerializeField] private ToggleType toggleType = ToggleType.CheckBox;
        [Tooltip("The direction of the scroll view. The VisualElement ScrollView is always bidirectional, but the MRTK prefab doesn't support it.")]
        [SerializeField] private ScrollViewDirection scrollViewDirection = ScrollViewDirection.Vertical;
        [Tooltip("If true, you only need to set the WIDTH of the backplate, the height will be calculated based on the aspect ratio of the 2D UI backplate.")]
        [SerializeField] private bool preserveAspectRatio = true;
        [Tooltip("Show the GridObjectCollection that comes from the foldout by default. You can also easily set them active or inactive in the inpsector.")]
        [SerializeField] private bool showFoldoutObjectCollection = true;
        [Tooltip("Whether show the backplate of controls or not. It may give a better appearance.")]
        [SerializeField] private bool showControlBackplate = true;
/*        [Tooltip("The width of the backplate.")]
        [SerializeField] private float backplateWidth = 10f;*/
        [Tooltip("The size on X and Y axes of the 3D UI backplate in cm, which represents the \"Canvas Size\" in UI Builder. " +
            "You might choose a size that best suitable for your 2D UI. The ratio between the sizes of VisualElements would be preserved.")]
        [SerializeField] private Vector2 backplateSize = new Vector2(30, 30);
        [Tooltip("The size of the GridObjectCollection which comes from Foldout. You might also change it manually for every collection.")]
        [SerializeField] private Vector2 foldoutCellSize = new Vector2(10, 10);


        #endregion

        #region Non-Serializable Fields

        //whether there are these elements or not. Influence the notice message.
        private bool hasInputField;
        private bool hasLabel;
        private bool hasButtonOrToggle;
        private bool hasScrollView;
        private bool hasFoldout;
        private bool hasSlider;

        //A List of strings to pass to the notice window.
        private List<string> noticeMessage;
        //Successful or failed.
        private ConversionResult conversionResult;
        //The resolved UI from the UXML
        private TemplateContainer uiToConvert;
        //The height/width of the backplate, only used if preserveAspectRatio is true.
        private float heightWidthRatio;
        //The backplate prefab of the converted UI.
        private GameObject backplate;
        #endregion


        #region MRTK Prefabs
        //A 10cm * 10cm backplate
        private GameObject backplateBase;
        //A label prefab
        private GameObject labelBase;
        //A 3.2cm * 3.2cm button
        private GameObject buttonBase;
        //A 3.2cm * 3.2cm toggle, can be a checkmark, radio, or switch, based on settings.
        private GameObject toggleBase;
        //A 25cm * 4cm slider
        private GameObject sliderBase;
        //A 25cm * 4cm step slider (SliderInt)
        private GameObject stepSliderBase;
        //A 8cm * 1.5cm text field (input field)
        private GameObject textFieldBase;
        #endregion

        /// <summary>
        /// The UXML file to convert, which is assigned in the inspector.
        /// </summary>
        public VisualTreeAsset UxmlToConvert
        {
            get => uxmlToConvert;
            set => uxmlToConvert = value;
        }

        /// <summary>
        /// The style sheets of the UxmlToConvert.
        /// </summary>
        public List<StyleSheet> StyleSheets
        {
            get => styleSheets;
        }

        /// <summary>
        /// The resolved UI from the UxmlToConvert.
        /// </summary>
        public TemplateContainer UIToConvert
        {
            get => uiToConvert;
            set => uiToConvert = value;
        }

        #region Public Methods 

        /// <summary>
        /// Convert the UxmlToConvert to 3D UI
        /// </summary>
        public void Convert() {
            //Init
            noticeMessage = new List<string>();
            hasLabel = false;
            hasInputField = false;
            hasButtonOrToggle = false;
            hasScrollView = false;
            hasFoldout = false;
            hasSlider = false;
            //Make sure the toggleBase corresponds to the toggleType.
            conversionResult = ConversionResult.Successful;
            toggleBase = SetToggleBase();
            if (!AllPrefabExist()) {
                return;
            }
            if(uiToConvert == null) {
                noticeMessage.Add("Nothing to convert. Make sure you set the UXML file in the inspector of the converter.");
                conversionResult = ConversionResult.Failed;
            }
            else {
                //Recalculate the backplateSize if user wants to preserve the aspect ratio
                if (preserveAspectRatio) {
                    heightWidthRatio = UIToConvert.resolvedStyle.height / UIToConvert.resolvedStyle.width;
                    backplateSize = new Vector2(backplateSize.x, backplateSize.x * heightWidthRatio);
                }
                else {
                    heightWidthRatio = backplateSize.y / backplateSize.x;
                }
                backplate = InstantiateBackplate();
                ConvertChildren(UIToConvert, backplate, backplateSize);
            }
            if (conversionResult == ConversionResult.Successful) {
                noticeMessage.Insert(0, "Conversion Succeed");
                if (hasButtonOrToggle) {
                    noticeMessage.Add("SeeItSayItLabels are deactivated because their scale will change in Play Mode");
                }
                if (hasLabel) {
                    noticeMessage.Add("You might need to adjust the font size of labels.");
                }
                if (hasSlider) {
                    noticeMessage.Add("You might need to set the Z value of the scale of the slider for a better appearance.");
                }
                if (hasInputField) {
                    noticeMessage.Add("You might need to adjust the font size and the RectTransform of the text input field");
                }
                if (hasScrollView) {
                    noticeMessage.Add("You might adjust the size of the scroll view so that it can always fully display at least one object, because objects cannot be clicked if it not fully displayed in the scrolling object collection");
                }
                if (hasFoldout) {
                    noticeMessage.Add("You might adjust the objects' size in the object collections.");
                }
                noticeMessage.Add("You might need to adjust Z values of objects' position manually for a better appearance.");
            }
            else {
                noticeMessage.Insert(0, "Conversion Failed");
            }
            ConverterWindow.ShowResultNotice(noticeMessage);
        }

        #endregion

        #region Private Methods

        private void OnEnable() {
            backplateBase = AssetDatabase.LoadAssetAtPath<GameObject>(ConverterUtilities.MRTKSDKRootPath + "/StandardAssets/Prefabs/UI Backplate.prefab");
            buttonBase = AssetDatabase.LoadAssetAtPath<GameObject>(ConverterUtilities.MRTKSDKRootPath + "/Features/UX/Interactable/Prefabs/PressableButtonHoloLens2.prefab");
            labelBase = AssetDatabase.LoadAssetAtPath<GameObject>(ConverterUtilities.MRTKSDKRootPath + "/StandardAssets/Prefabs/Text/UITextSelawik.prefab");
            toggleBase = SetToggleBase();
            sliderBase = AssetDatabase.LoadAssetAtPath<GameObject>(ConverterUtilities.MRTKSDKRootPath + "/Features/UX/Prefabs/Sliders/PinchSlider.prefab");
            stepSliderBase = AssetDatabase.LoadAssetAtPath<GameObject>(ConverterUtilities.MRTKSDKRootPath + "/Experimental/StepSlider/StepSlider.prefab");
            textFieldBase = AssetDatabase.LoadAssetAtPath<GameObject>(ConverterUtilities.MRTKSDKRootPath + "/Experimental/MixedRealityKeyboard/Prefabs/MRKeyboardInputField_TMP.prefab");
        }

        private GameObject SetToggleBase() {
            GameObject toggleBase = AssetDatabase.LoadAssetAtPath<GameObject>(ConverterUtilities.MRTKSDKRootPath + "/Features/UX/Interactable/Prefabs/PressableButtonHoloLens2ToggleCheckBox_32x32.prefab");
            switch (toggleType) {
                case ToggleType.CheckBox:
                    toggleBase = AssetDatabase.LoadAssetAtPath<GameObject>(ConverterUtilities.MRTKSDKRootPath + "/Features/UX/Interactable/Prefabs/PressableButtonHoloLens2ToggleCheckBox_32x32.prefab");
                    break;
                case ToggleType.Button:
                    toggleBase = AssetDatabase.LoadAssetAtPath<GameObject>(ConverterUtilities.MRTKSDKRootPath + "/Features/UX/Interactable/Prefabs/PressableButtonHoloLens2Toggle.prefab");
                    break;
                case ToggleType.Radio:
                    toggleBase = AssetDatabase.LoadAssetAtPath<GameObject>(ConverterUtilities.MRTKSDKRootPath + "/Features/UX/Interactable/Prefabs/PressableButtonHoloLens2ToggleRadio_32x32.prefab");
                    break;
                case ToggleType.Switch:
                    toggleBase = AssetDatabase.LoadAssetAtPath<GameObject>(ConverterUtilities.MRTKSDKRootPath + "/Features/UX/Interactable/Prefabs/PressableButtonHoloLens2ToggleSwitch_32x32.prefab");
                    break;
            }
            return toggleBase;
        }

        //Check if all prefab exist.
        private bool AllPrefabExist() {
            if (!backplateBase) {
                Debug.LogError($"UI Backplate is not found under Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/StandardAssets/Prefabs/, make sure you installed MRTK of a version at least {ConverterUtilities.MRTKVersion}");
                return false;
            }
            if (!buttonBase) {
                Debug.LogError($"PressableButtonHoloLens2 is not found under Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Features/UX/Interactable/Prefabs/, make sure you installed MRTK of a version at least {ConverterUtilities.MRTKVersion}.");
                return false;
            }
            if (!labelBase) {
                Debug.LogError($"UITextSelawik is not found under Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/StandardAssets/Prefabs/Text/, make sure you installed MRTK of a version at least {ConverterUtilities.MRTKVersion}.");
                return false;
            }
            if (!toggleBase) {
                Debug.LogError($"PressableButtonHoloLens2Toggle is not found under Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Features/UX/Interactable/Prefabs/, make sure you installed MRTK of a version at least {ConverterUtilities.MRTKVersion}.");
                return false;
            }
            if (!sliderBase) {
                Debug.LogError($"PinchSlider is not found under Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Features/UX/Prefabs/Sliders/, make sure you installed MRTK of a version at least {ConverterUtilities.MRTKVersion}.");
                return false;
            }
            if (!stepSliderBase) {
                Debug.LogError($"StepSlider is not found under Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Experimental/StepSlider/, make sure you installed MRTK of a version at least {ConverterUtilities.MRTKVersion}.");
            }
            if (!textFieldBase) {
                Debug.LogError($"MRKeyboardInputField_TMP is not found under Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Experimental/MixedRealityKeyboard/Prefabs/, make sure you installed MRTK of a version at least {ConverterUtilities.MRTKVersion}.");
            }
            return true;
        }

        //Convert the children of the parentVE recursively, the recursion terminates on "Controls", e.g. Button, Toggle, etc, but not on "Containers", e.g. Scroll View.
        private List<GameObject> ConvertChildren(VisualElement parentVE, GameObject parentGO, Vector2 convertedSizeParent) {
            List<GameObject> children = new List<GameObject>();
            foreach (VisualElement childVE in parentVE.Children()) {
                if(childVE is Label) {
                    children.Add(InstantiateLabel((Label)childVE, parentGO, convertedSizeParent));
                }else if (childVE is Button) {
                    children.Add(InstantiateButton((Button)childVE, parentGO, convertedSizeParent));
                }else if (childVE is Toggle) {
                    children.Add(InstantiateToggle((Toggle)childVE, parentGO, convertedSizeParent));
                }else if (childVE is Slider) {
                    children.Add(InstantiateSlider((Slider)childVE, parentGO, convertedSizeParent));
                }else if (childVE is SliderInt) {
                    children.Add(InstantiateSliderInt((SliderInt)childVE, parentGO, convertedSizeParent));
                }else if (childVE is TextField) {
                    children.Add(InstantiateTextField((TextField)childVE, parentGO, convertedSizeParent));
                }else if (childVE is ScrollView) {
                    children.Add(InstantiateScrollingObjectCollection((ScrollView)childVE, parentGO, convertedSizeParent));
                }else if (childVE is Foldout) {
                    children.Add(InstantiateGridObjectCollection((Foldout)childVE, parentGO, convertedSizeParent));
                }
                //The element is not any supported controls or it is only a VisualElement with or without background image.
                //If it is with background image, we treat it as a slate.
                //If it isn't with a background image, we treat it as a container.
                else { 
                    children.Add(InstantiateEmptyElement(childVE, parentGO, convertedSizeParent));
                }
            }
            return children;
        }

        //Instantiate the backplate with the given size in cm.
        private GameObject InstantiateBackplate() {           
            GameObject backplate = Instantiate(backplateBase, Vector3.zero, Quaternion.identity);
            backplate.name = "Converted UI";
            //The default size of the backplate is 10cm * 10cm
            Vector2 scale = new Vector2(backplateSize.x/10, backplateSize.y/10);
            //The localScale is 0.1/0.1/0.01 by the default prefab.
            backplate.transform.localScale = new Vector3(scale.x * 0.1f, scale.y * 0.1f, backplate.transform.localScale.z);
            return backplate;
        }

        private GameObject InstantiateLabel(Label childVE, GameObject parentGO, Vector2 convertedSizeParent) {
            GameObject label = Instantiate(labelBase);
            label.name = childVE.name == "" ? "Label" : childVE.name;          
            label.GetComponent<RectTransform>().localPosition = new Vector3(0, 0, -0.1f);
            label.GetComponent<RectTransform>().SetParent(parentGO.transform, false);
            //The multiplied scale towards the root gameObject.
            Vector2 scaleMultiplyFactor = ComputeScaleMultiplyFactor(label);
            label.GetComponent<RectTransform>().localScale = new Vector3(label.GetComponent<RectTransform>().localScale.x / scaleMultiplyFactor.x,
                label.GetComponent<RectTransform>().localScale.y / scaleMultiplyFactor.y, 1);
            GameObject textGO = label.transform.Find("Text").gameObject;            
            //We use TextMeshPro instead of TextMesh
            DestroyImmediate(textGO.GetComponent<UnityEngine.UI.Text>());
            TextMeshPro tmp = textGO.AddComponent<TextMeshPro>();
            //For Label, we need set the font size manually, because it use the "RectTransform" and will not be scaled.
            //A little bit magic, but works well. A manual modification of the font size may be needed.
            tmp.fontSize = childVE.resolvedStyle.fontSize / (1 / backplateSize.y);
            Vector2 convertedCenter;
            Vector2 convertedSizeSelf = ResolveTransform(childVE, label, BaseSize.Label, convertedSizeParent, out _, out convertedCenter);                

            //The width of the RectTransform of the Text is 500, which represents 25cm in the original scale, then we calculate the height by using the Height/Width ratio of the visual element.
            textGO.GetComponent<RectTransform>().sizeDelta = new Vector2(500 * (convertedSizeSelf.x / BaseSize.Label.x), 
                500 * (convertedSizeSelf.x / BaseSize.Label.x) * (childVE.resolvedStyle.height / childVE.resolvedStyle.width));

            label.GetComponent<RectTransform>().localPosition = new Vector3(convertedCenter.x, convertedCenter.y, label.GetComponent<RectTransform>().localPosition.z);

            ResolveTextAndFont(childVE, tmp, false);

            hasLabel = true;

            return label;
        }

        //Instantiate a button, and set its appearance accroding to childVE.resolvedStyle
        private GameObject InstantiateButton(Button childVE, GameObject parentGO, Vector2 convertedSizeParent) {
            GameObject button = Instantiate(buttonBase);
            button.name = childVE.name == "" ? "Button" : childVE.name;
            button.transform.parent = parentGO.transform;
            button.transform.localPosition = new Vector3(0, 0, -1);
            //We don't call ConvertChildren on Button, so we discard the output.
            ResolveTransformAndApply(childVE, button, BaseSize.Button, convertedSizeParent);
            ResolveTextAndFont(childVE, button.transform.Find("IconAndText/TextMeshPro").gameObject.GetComponent<TextMeshPro>(), true);
            //We scale the IconAndText of the hololens button prefab to a square for better appearance, and we keep the larger localScale on axis X and Y to 1,
            //which means we keep the final localScale on both axes smaller or equal to 1.
            //Similarly, we scale the SeeItSayItLabel.
            //If the button is a child of Foldout, we do nothing.
            if(!(childVE.parent is Foldout)) {
                Vector2 scaleMultiplyFactor = ComputeScaleMultiplyFactor(button); //The multiplied scale towards the root gameObject.
                float heightWidthRatio = scaleMultiplyFactor.y / scaleMultiplyFactor.x;
                //Since the buttonBase has same height and width, we can directly use the scaleMultiplyFactor to see on which axis it is stretched.
                if (scaleMultiplyFactor.x > scaleMultiplyFactor.y) {
                    button.transform.Find("IconAndText").localScale = new Vector3(button.transform.localScale.y / button.transform.localScale.x * heightWidthRatio, 1, 1);
                    button.transform.Find("SeeItSayItLabel").transform.localScale = new Vector3(button.transform.localScale.y / button.transform.localScale.x * heightWidthRatio, 1, 1);
                }
                else {
                    button.transform.Find("IconAndText").localScale = new Vector3(1, button.transform.localScale.x / button.transform.localScale.y / heightWidthRatio, 1);
                    button.transform.Find("SeeItSayItLabel").transform.localScale = new Vector3(1, button.transform.localScale.x / button.transform.localScale.y / heightWidthRatio, 1);
                }
            }
            if (!showControlBackplate) {
                button.transform.Find("BackPlate").gameObject.SetActive(false);
            }
            button.GetComponent<ButtonConfigHelper>().SeeItSayItLabelEnabled = false;
            hasButtonOrToggle = true;
            return button;
        }

        private GameObject InstantiateToggle(Toggle childVE, GameObject parentGO, Vector2 convertedSizeParent) {
            GameObject toggle = Instantiate(toggleBase);
            toggle.name = childVE.name == "" ? "Toggle" : childVE.name;
            toggle.transform.parent = parentGO.transform;
            toggle.transform.localPosition = new Vector3(0, 0, -1);
            if (childVE.value) {
                noticeMessage.Add($"The VisualElement toggle {childVE} is toggled, remember to set the IsToggled property of the Interactable of {toggle} to true.");
            }
            ResolveTransformAndApply(childVE, toggle, BaseSize.Toggle, convertedSizeParent);
            ResolveTextAndFont(childVE.Q<Label>(), toggle.transform.Find("IconAndText/TextMeshPro").gameObject.GetComponent<TextMeshPro>(), true);
            //Scale the IconAndText, Similar to buttons
            if(!(childVE.parent is Foldout)) {
                Vector2 scaleMultiplyFactor = ComputeScaleMultiplyFactor(toggle); //The multiplied scale towards the root gameObject.
                float heightWidthRatio = scaleMultiplyFactor.y / scaleMultiplyFactor.x;
                if (scaleMultiplyFactor.x > scaleMultiplyFactor.y) {
                    toggle.transform.Find("IconAndText").localScale = new Vector3(toggle.transform.localScale.y / toggle.transform.localScale.x * heightWidthRatio, 1, 1);
                    toggle.transform.Find("SeeItSayItLabel").transform.localScale = new Vector3(toggle.transform.localScale.y / toggle.transform.localScale.x * heightWidthRatio, 1, 1);
                }
                else {
                    toggle.transform.Find("IconAndText").localScale = new Vector3(1, toggle.transform.localScale.x / toggle.transform.localScale.y / heightWidthRatio, 1);
                    toggle.transform.Find("SeeItSayItLabel").transform.localScale = new Vector3(1, toggle.transform.localScale.x / toggle.transform.localScale.y / heightWidthRatio, 1);
                }
            }
            if (!showControlBackplate) {
                toggle.transform.Find("BackPlate").gameObject.SetActive(false);
            }
            toggle.GetComponent<ButtonConfigHelper>().SeeItSayItLabelEnabled = false;
            hasButtonOrToggle = false;
            return toggle;
        }

        private GameObject InstantiateSlider(Slider childVE, GameObject parentGO, Vector2 convertedSizeParent) {
            GameObject slider = Instantiate(backplateBase);
            slider.GetComponent<BoxCollider>().enabled = false;
            slider.name = childVE.name == "" ? "Slider" : childVE.name;
            slider.transform.parent = parentGO.transform;
            slider.transform.localPosition = new Vector3(0, 0, -0.5f);

            VisualElement childSliderVE = childVE.Q<VisualElement>(name: "unity-drag-container");
            Vector2 convertedSizeWholeSlider = ResolveTransformAndApply(childVE, slider, BaseSize.Backplate, convertedSizeParent);

            //Instantiate a label, which corresponds to the label on the right side of the whole VisualElement Slider.
            Label childLabelVE = childVE.Q<Label>();
            GameObject label = InstantiateLabel(childLabelVE, slider, convertedSizeWholeSlider);
            label.GetComponent<RectTransform>().localPosition = new Vector3(label.GetComponent<RectTransform>().localPosition.x, label.GetComponent<RectTransform>().localPosition.y, -0.01f);

            //The container of the slider part of the VisualElement "Slider", it may also conatain the TextField of the slider value, if showInputField is set to true.
            VisualElement sliderContainerVE = childVE.Query<VisualElement>().AtIndex(2);
            GameObject sliderContainerGO = new GameObject("SliderContainer");
            sliderContainerGO.transform.parent = slider.transform;
            sliderContainerGO.transform.localPosition = Vector3.zero;
            Vector2 convertedSizeSilderContainer = ResolveTransformAndApply(sliderContainerVE, sliderContainerGO, BaseSize.EmptyGameObject, convertedSizeWholeSlider);

            //Instantiate the slider part.
            GameObject childSliderGO = Instantiate(sliderBase);
            childSliderGO.GetComponent<PinchSlider>().SliderValue = (childVE.value - childVE.lowValue) / (childVE.highValue - childVE.lowValue);    
            childSliderGO.transform.parent = sliderContainerGO.transform;
            childSliderGO.transform.localPosition = new Vector3(0, 0, -1 / sliderContainerGO.transform.localScale.z);
            ResolveTransformAndApply(childSliderVE, childSliderGO, BaseSize.Slider, convertedSizeSilderContainer);
            //We set localScale.y based on the scale of its parents to get a better apearance, and we use X as the base.
            float scaleY = childSliderGO.transform.localScale.y * sliderContainerGO.transform.localScale.x / sliderContainerGO.transform.localScale.y;
            childSliderGO.transform.localScale = new Vector3(childSliderGO.transform.localScale.x, scaleY, childSliderGO.transform.localScale.z);
            

            //If showInputeField is true, we need another label to display the slider value on the right of the PinchSlider.
            if (childVE.showInputField) {
                TextField childValueField = sliderContainerVE.Q<TextField>(name: "unity-text-field");
                GameObject textFieldCanvasGO = InstantiateInputField(childValueField, sliderContainerGO, convertedSizeSilderContainer);
                textFieldCanvasGO.name = "ValueField";
                var inputField = textFieldCanvasGO.GetComponentInChildren<TMP_InputField>();
                inputField.text = childVE.value.ToString();
                //register event
                SliderValueSynchronizer synchronizer = sliderContainerGO.AddComponent<SliderValueSynchronizer>();
                synchronizer.Initialize(childSliderGO.GetComponent<PinchSlider>(), inputField, childVE.lowValue, childVE.highValue, false);
                synchronizer.RegisterEvent();

            }
            if (!showControlBackplate) {
                slider.GetComponent<MeshRenderer>().enabled = false;
            }
            hasSlider = true;
           
            return slider;
        }

        private GameObject InstantiateSliderInt(SliderInt childVE, GameObject parentGO, Vector2 convertedSizeParent) {
            GameObject slider = Instantiate(backplateBase);
            slider.GetComponent<BoxCollider>().enabled = false;
            slider.name = childVE.name == "" ? "SliderInt" : childVE.name;
            slider.transform.parent = parentGO.transform;
            slider.transform.localPosition = new Vector3(0, 0, -0.5f);

            VisualElement childSliderVE = childVE.Q<VisualElement>(name: "unity-drag-container");
            Vector2 convertedSizeWholeSlider = ResolveTransformAndApply(childVE, slider, BaseSize.Backplate, convertedSizeParent);

            //Instantiate a label, which corresponds to the label on the right side of the whole VisualElement Slider.
            Label childLabelVE = childVE.Q<Label>();
            GameObject label = InstantiateLabel(childLabelVE, slider, convertedSizeWholeSlider);
            label.GetComponent<RectTransform>().localPosition = new Vector3(label.GetComponent<RectTransform>().localPosition.x, label.GetComponent<RectTransform>().localPosition.y, -0.01f);

            //The container of the slider part of the VisualElement "Slider", it may also conatain the TextField of the slider value, if showInputField is set to true.
            VisualElement sliderContainerVE = childVE.Query<VisualElement>().AtIndex(2);
            GameObject sliderContainerGO = new GameObject("SliderContainer");
            sliderContainerGO.transform.parent = slider.transform;
            sliderContainerGO.transform.localPosition = Vector3.zero;
            Vector2 convertedSizeSilderContainer = ResolveTransformAndApply(sliderContainerVE, sliderContainerGO, BaseSize.EmptyGameObject, convertedSizeWholeSlider);

            //Instantiate the slider part.
            GameObject childSliderGO = Instantiate(stepSliderBase);
            childSliderGO.GetComponent<StepSlider>().SliderStepDivisions = childVE.highValue - childVE.lowValue;
            childSliderGO.GetComponent<StepSlider>().SliderValue = ((float)childVE.value - childVE.lowValue) / (childVE.highValue - childVE.lowValue);
            childSliderGO.transform.parent = sliderContainerGO.transform;
            childSliderGO.transform.localPosition = new Vector3(0, 0, -1 / sliderContainerGO.transform.localScale.z);
            ResolveTransformAndApply(childSliderVE, childSliderGO, BaseSize.Slider, convertedSizeSilderContainer);
            //We set localScale.y based on the scale of its parents to get a better apearance, and we use X as the base.
            float scaleY = childSliderGO.transform.localScale.y * sliderContainerGO.transform.localScale.x / sliderContainerGO.transform.localScale.y;
            childSliderGO.transform.localScale = new Vector3(childSliderGO.transform.localScale.x, scaleY, childSliderGO.transform.localScale.z);


            //If showInputeField is true, we need another label to display the slider value on the right of the PinchSlider.
            if (childVE.showInputField) {
                TextField childValueField = sliderContainerVE.Q<TextField>(name: "unity-text-field");
                GameObject textFieldCanvasGO = InstantiateInputField(childValueField, sliderContainerGO, convertedSizeSilderContainer);
                textFieldCanvasGO.name = "ValueField";
                var inputField = textFieldCanvasGO.GetComponentInChildren<TMP_InputField>();
                inputField.text = childVE.value.ToString();
                //Set the synchronizer
                SliderValueSynchronizer synchronizer = sliderContainerGO.AddComponent<SliderValueSynchronizer>();
                synchronizer.Initialize(childSliderGO.GetComponent<PinchSlider>(), inputField, childVE.lowValue, childVE.highValue, true);
                synchronizer.RegisterEvent();

            }
            if (!showControlBackplate) {
                slider.GetComponent<MeshRenderer>().enabled = false;
            }
            hasSlider = true;

            return slider;
        }

        private GameObject InstantiateTextField(TextField childVE, GameObject parentGO, Vector2 convertedSizeParent) {
            GameObject textField = Instantiate(backplateBase);
            textField.GetComponent<BoxCollider>().enabled = false;
            textField.name = childVE.name == "" ? "TextField" : childVE.name;
            textField.transform.parent = parentGO.transform;
            textField.transform.localPosition = new Vector3(0, 0, -0.5f);
            Vector2 convertedSizeTextField = ResolveTransformAndApply(childVE, textField, BaseSize.Backplate, convertedSizeParent);

            //Instantiate a label, which corresponds to the label on the right side of the whole VisualElement TextField.
            Label childLabelVE = childVE.Q<Label>();
            GameObject label = InstantiateLabel(childLabelVE, textField, convertedSizeParent);
            label.GetComponent<RectTransform>().localPosition = new Vector3(label.GetComponent<RectTransform>().localPosition.x, label.GetComponent<RectTransform>().localPosition.y, -0.01f);

            //Instantiate the input field part.
            GameObject inputField = InstantiateInputField(childVE.Q<VisualElement>(name: "unity-text-input"), textField, convertedSizeTextField);
            inputField.GetComponentInChildren<TMP_InputField>().text = childVE.text;

            if (!showControlBackplate) {
                textField.GetComponent<MeshRenderer>().enabled = false;
            }
            return textField;
        }

        private GameObject InstantiateInputField(VisualElement childVE, GameObject parentGO, Vector2 convertedSizeParent) {
            GameObject textFieldCanvas = Instantiate(labelBase);
            textFieldCanvas.AddComponent<CanvasUtility>();
            textFieldCanvas.AddComponent<NearInteractionTouchableUnityUI>().EventsToReceive = TouchableEventType.Pointer;
            textFieldCanvas.name = childVE.name == "" ? "InputField" : childVE.name;
            textFieldCanvas.GetComponent<RectTransform>().localPosition = new Vector3(0, 0, -0.1f);
            textFieldCanvas.GetComponent<RectTransform>().SetParent(parentGO.transform, false);
            //The scale factor from the root to itself
            Vector2 scaleMultiplyFactor = ComputeScaleMultiplyFactor(textFieldCanvas);
            textFieldCanvas.GetComponent<RectTransform>().localScale = new Vector3(textFieldCanvas.GetComponent<RectTransform>().localScale.x / scaleMultiplyFactor.x,
                textFieldCanvas.GetComponent<RectTransform>().localScale.y / scaleMultiplyFactor.y, 1);
            
            GameObject textGO = textFieldCanvas.transform.Find("Text").gameObject;

            GameObject inputField = Instantiate(textFieldBase, textFieldCanvas.transform);
            inputField.transform.position = new Vector3(inputField.transform.position.x, inputField.transform.position.y, -0.01f);
            TMP_InputField tmp_input = inputField.GetComponent<TMP_InputField>();

            DestroyImmediate(textGO);
            Vector2 convertedCenter;
            Vector2 convertedSizeSelf = ResolveTransform(childVE, textFieldCanvas, BaseSize.TextField, convertedSizeParent, out _, out convertedCenter);
            
            //The orgininal width and height of the RectTransform of the TextField is 160 and 30, which represents 8cm in the original scale.
            //However, changing the height may make the text invisible with a readable font size, so we don't change it here.
            //It may need manual modification.
            inputField.GetComponent<RectTransform>().sizeDelta = new Vector2(160 * (convertedSizeSelf.x / BaseSize.TextField.x), 30 * convertedSizeSelf.y / BaseSize.TextField.y);

            //The original (with 160*30) offsets are top: 7, bottom: 6, left: 10, right: 10, so the text area is 17 units height.
            //We reset the offsets to make the text be displayed properly, and firstly we make the top and bottom the same (6.5), and then top + 0.5, bottom - 0.5, top>0.
            //We keep the text area 17 units high, and we only calculate the scale of the top/bottom/left/right.
            //offsetMin is left and bottom, clamp y value to 0.
            inputField.transform.Find("Text Area").GetComponent<RectTransform>().offsetMin = new Vector2(10 * (inputField.GetComponent<RectTransform>().rect.width / 160), 6f * (Mathf.Clamp((inputField.GetComponent<RectTransform>().rect.height / 2 - 9f) / 6f, 0, float.MaxValue)));
            //offsetMax is right and top, and we need to take the minus value because offsetMax is reversed, and we clamp the y value to 0.
            inputField.transform.Find("Text Area").GetComponent<RectTransform>().offsetMax = new Vector2(-10 * (inputField.GetComponent<RectTransform>().rect.width / 160), -7f * (Mathf.Clamp((inputField.GetComponent<RectTransform>().rect.height / 2 - 8f) / 7f, 0, float.MaxValue)));
           
            //For Text, we need set the font size manually, because it use the "RectTransform" and will not be scaled.
            //A little bit magic, but works well. A manual modification of the font size may be needed.
            tmp_input.pointSize = childVE.resolvedStyle.fontSize;

            textFieldCanvas.GetComponent<RectTransform>().localPosition = new Vector3(convertedCenter.x, convertedCenter.y, textFieldCanvas.GetComponent<RectTransform>().localPosition.z);

            hasInputField = true;

            return textFieldCanvas;
        }

        private GameObject InstantiateScrollingObjectCollection(ScrollView childVE, GameObject parentGO, Vector2 convertedSizeParent) {
            GameObject scrollView = new GameObject();
            ScrollingObjectCollection collection = scrollView.AddComponent<ScrollingObjectCollection>();       
            scrollView.transform.parent = parentGO.transform;
            scrollView.name = childVE.name == "" ? "ScrollView" : childVE.name;
            //The VisualElement ScrollView is always bidirectional.
            if(scrollViewDirection == ScrollViewDirection.Vertical) {
                collection.ScrollDirection = ScrollingObjectCollection.ScrollDirectionType.UpAndDown;
            }
            else {
                collection.ScrollDirection = ScrollingObjectCollection.ScrollDirectionType.LeftAndRight;
            }
            //The scrolling object collection always has one cell. Since we don't use an addition grid object collection here, the cell is only used to set the size.
            collection.TiersPerPage = 1;
            Vector2 scale;
            Vector2 convertedCenter;
            Vector2 convertedSizeSelf = ResolveTransform(childVE, scrollView, BaseSize.ScrollingObjectCollection, convertedSizeParent, out scale, out convertedCenter);
            //We don't directly adjust scale here, but the width and height of the cell.
            //Convert cm to m.
            collection.CellWidth = convertedSizeSelf.x / 100;
            collection.CellHeight = convertedSizeSelf.y / 100;
            //We first compute the position because the width and height will be changed later to match its absolute size, but not relative size.
            //Since the center of the scrolling object collection is at the upper left corner but not the real center, we need to adjust it.
            scrollView.transform.localPosition = new Vector3(convertedCenter.x - collection.CellWidth / 2 * scrollView.transform.localScale.x , convertedCenter.y + collection.CellHeight / 2 * scrollView.transform.localScale.y, scrollView.transform.localPosition.z);

            //Create a temp gameObject as a container to make sure that the children of the scrollView are instantiated correctly.
            //Then we set the parent of its children to the container of the scrolling object collection.
            GameObject temp = new GameObject();
            temp.transform.localScale = new Vector3(collection.CellWidth, collection.CellHeight, 1);
            temp.transform.localPosition = new Vector3(scrollView.transform.position.x + collection.CellWidth / 2, scrollView.transform.position.y - collection.CellHeight / 2, -0.01f);
            ConvertChildren(childVE.Q<VisualElement>(name: "unity-content-container"), temp, convertedSizeSelf);
            List<GameObject> tempList = new List<GameObject>();
            foreach (Transform child in temp.transform) {
                child.localPosition = new Vector3(child.localPosition.x, child.localPosition.y, 0);
                tempList.Add(child.gameObject);
            }
            foreach (GameObject go in tempList) {
                go.transform.parent = scrollView.transform.Find("Container");
            }
            DestroyImmediate(temp);
            hasScrollView = true;
            return scrollView;
        }

        private GameObject InstantiateGridObjectCollection(Foldout childVE, GameObject parentGO, Vector2 convertedSizeParent) {
            if(childVE.value == true) {
                conversionResult = ConversionResult.Failed;
                noticeMessage.Add("Please fold the Foldout, then try again.");
                return null;
            }
            GameObject foldoutButton = Instantiate(buttonBase);
            foldoutButton.name = childVE.name == "" ? "Foldout" : childVE.name;
            foldoutButton.transform.parent = parentGO.transform;
            foldoutButton.transform.localPosition = new Vector3(0, 0, -1);
            //We don't call ConvertChildren on Button, so we discard the output.
            ResolveTransformAndApply(childVE, foldoutButton, BaseSize.Button, convertedSizeParent);
            ResolveTextAndFont(childVE.Q<Toggle>().Q<Label>(), foldoutButton.transform.Find("IconAndText/TextMeshPro").gameObject.GetComponent<TextMeshPro>(), true);
            //We scale the IconAndText of the hololens button prefab to a square for better appearance, and we keep the larger localScale on axis X and Y to 1,
            //which means we keep the final localScale on both axes smaller or equal to 1.
            //Similarly, we scale the SeeItSayItLabel.        
            Vector2 scaleMultiplyFactor = ComputeScaleMultiplyFactor(foldoutButton); //The multiplied scale towards the root gameObject.
            float heightWidthRatio = scaleMultiplyFactor.y / scaleMultiplyFactor.x;
            if (foldoutButton.transform.localScale.x > foldoutButton.transform.localScale.y) {
                foldoutButton.transform.Find("IconAndText").localScale = new Vector3(foldoutButton.transform.localScale.y / foldoutButton.transform.localScale.x * heightWidthRatio, 1, 1);
                foldoutButton.transform.Find("SeeItSayItLabel").transform.localScale = new Vector3(foldoutButton.transform.localScale.y / foldoutButton.transform.localScale.x * heightWidthRatio, 1, 1);
            }
            else {
                foldoutButton.transform.Find("IconAndText").localScale = new Vector3(1, foldoutButton.transform.localScale.x / foldoutButton.transform.localScale.y / heightWidthRatio, 1);
                foldoutButton.transform.Find("SeeItSayItLabel").transform.localScale = new Vector3(1, foldoutButton.transform.localScale.x / foldoutButton.transform.localScale.y / heightWidthRatio, 1);
            }

            if (!showControlBackplate) {
                foldoutButton.transform.Find("BackPlate").gameObject.SetActive(false);
            }
            foldoutButton.GetComponent<ButtonConfigHelper>().SeeItSayItLabelEnabled = false;

            GameObject foldout = new GameObject();
            GridObjectCollection collection = foldout.AddComponent<GridObjectCollection>();
            foldout.transform.parent = foldoutButton.transform;
            foldout.name = "GridObjectCollection";
            foldout.transform.position = new Vector3(foldoutButton.transform.position.x + 0.3f, foldoutButton.transform.position.y, foldoutButton.transform.position.z);
            collection.CellWidth = foldoutCellSize.x / 100;
            collection.CellHeight = foldoutCellSize.y / 100;
            collection.Layout = LayoutOrder.Vertical;
            //Create a temp gameObject as a container to make sure that the children of the scrollView are instantiated correctly.
            //Then we set the parent of its children to the container of the scrolling object collection.
            GameObject temp = new GameObject();
            temp.transform.localScale = new Vector3(collection.CellWidth, collection.CellHeight, 1);    
            List<GameObject> collectionElements = ConvertChildren(childVE.Q<VisualElement>(name: "unity-content"), temp, foldoutCellSize);
            List<GameObject> tempList = new List<GameObject>();
            foreach (Transform child in temp.transform) {
                child.localPosition = new Vector3(child.localPosition.x, child.localPosition.y, 0);
                tempList.Add(child.gameObject);
            }
            foreach (GameObject go in tempList) {
                go.transform.parent = foldout.transform;
            }
            foreach (GameObject go in collectionElements) {
                go.transform.localScale = new Vector3(1, 1, 1);
            }
            collection.UpdateCollection();
            if (showFoldoutObjectCollection) {
                foldout.SetActive(true);
            }
            else {
                foldout.SetActive(false);
            }
            DestroyImmediate(temp);
            foldoutButton.AddComponent<FoldoutController>().RegisterEvent();
            hasFoldout = true;
            return foldout;
        }

        private GameObject InstantiateEmptyElement(VisualElement childVE, GameObject parentGO, Vector2 convertedSizeParent) {
            GameObject emptyVE = new GameObject();
            emptyVE.transform.parent = parentGO.transform;
            emptyVE.name = childVE.name == "" ? "VisualElement" : childVE.name;
            Vector2 convertedSizeSelf = ResolveTransformAndApply(childVE, emptyVE, BaseSize.EmptyGameObject, convertedSizeParent);
            emptyVE.transform.localScale = new Vector3(emptyVE.transform.localScale.x, emptyVE.transform.localScale.y, 1);
            ConvertChildren(childVE, emptyVE, convertedSizeSelf);
            return emptyVE;    
        }

        //Resolve the transform (position and scale) of the basePrefab repecting to its parent, return the converted size of the basePrefab, the targeted scale, and the converted center.
        private Vector2 ResolveTransform(VisualElement ve, GameObject basePrefab, Vector2 baseSizeSelf, Vector2 convertedSizeParent, out Vector2 scale, out Vector2 convertedCenter) {
            //Some data with unit in px, top left corner is (0,0)
            Vector2 parentSize = new Vector2(ve.parent.resolvedStyle.width, ve.parent.resolvedStyle.height);
            Vector2 selfSize = new Vector2(ve.resolvedStyle.width, ve.resolvedStyle.height);
            Vector2 selfParentRatio = new Vector2(selfSize.x / parentSize.x, selfSize.y / parentSize.y);
            Vector2 center = new Vector2(ve.layout.x + selfSize.x / 2, ve.layout.y + selfSize.y / 2);
            //distance in px between the center of parent and itself
            //since the origin of the UXML is on the top left corner, and the origin of GameObjects are at the center, which needs a translation to the -X and Y directions, we need to reverse the Y value here.
            Vector2 centerDistance = new Vector2(center.x - parentSize.x / 2, parentSize.y / 2 - center.y);
            //convert the above data to size in cm:
            Vector2 convertedSizeSelf = new Vector2(selfParentRatio.x * convertedSizeParent.x, selfParentRatio.y * convertedSizeParent.y);
            //ratio between size in cm and px.
            Vector2 convertingRatio = new Vector2(convertedSizeParent.x / parentSize.x, convertedSizeParent.y / parentSize.y);
            scale = new Vector2(convertedSizeSelf.x / baseSizeSelf.x * basePrefab.transform.localScale.x, convertedSizeSelf.y / baseSizeSelf.y * basePrefab.transform.localScale.y);
            Vector2 convertedCenterDistance = new Vector2(centerDistance.x * convertingRatio.x, centerDistance.y * convertingRatio.y);
            //Calculate the position (center) of the basePrefab. We know localPositon.x=0.5 is on the right border of the parent, etc.
            //So we calculate here the ratio between the convertedCenterDistance and the half the the width/height of the parent's size, and then multiply with 0.5 as mentioned above.
            convertedCenter = new Vector2(convertedCenterDistance.x / (convertedSizeParent.x / 2) * 0.5f, convertedCenterDistance.y / (convertedSizeParent.y / 2) * 0.5f);
            return convertedSizeSelf;
        }

        //Resolve the transform (position and scale) of the basePrefab repecting to its parent, return the converted size of the basePrefab in cm.
        private Vector2 ResolveTransformAndApply(VisualElement ve, GameObject basePrefab, Vector2 baseSizeSelf, Vector2 convertedSizeParent) {
            Vector2 scale;
            Vector2 convertedCenter;
            Vector2 convertedSizeSelf = ResolveTransform(ve, basePrefab, baseSizeSelf, convertedSizeParent, out scale, out convertedCenter);
            basePrefab.transform.localScale = new Vector3(scale.x, scale.y, basePrefab.transform.localScale.z);
            basePrefab.transform.localPosition = new Vector3(convertedCenter.x, convertedCenter.y, basePrefab.transform.localPosition.z);

            return convertedSizeSelf;
        }

        //Apply the font style of the VE to TMP, the input is a TextElement and the TMP. The TextElement can be a Label or Button, or the child element "Label" of Toggle, Slider, etc.
        //handelFontSize is only set to false when "ve" is a standalone Label, which means it is NOT a child of other controls like Toggles or Silders.
        private void ResolveTextAndFont(TextElement ve, TextMeshPro tmp, bool handelFontSize) {
            tmp.text = ve.text;
            if (handelFontSize) {
                //We construct a relation between the two initial values of both sides, i.e. font size 0.04 of the TMP (MRTK prefabs) = font size 12 of the VisualElement
                tmp.fontSize = 0.04f * (ve.resolvedStyle.fontSize / 12);
            }
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            tmp.color = ve.resolvedStyle.color;
            switch (ve.resolvedStyle.unityFontStyleAndWeight) {
                case FontStyle.Bold:
                    tmp.fontStyle = FontStyles.Bold;
                    break;
                case FontStyle.Italic:
                    tmp.fontStyle = FontStyles.Italic;
                    break;
                case FontStyle.BoldAndItalic:
                    tmp.fontStyle = FontStyles.Bold | FontStyles.Italic;
                    break;
                default:
                    tmp.fontStyle = FontStyles.Normal;
                    break;
            }
        }

        //Compute the multiplied scale from the parent of the give gameObject towards the root gameObject.
        private Vector2 ComputeScaleMultiplyFactor(GameObject go) {
            GameObject parentGO = go.transform.parent.gameObject;
            Vector2 scaleMultiplyFactor = new Vector2(parentGO.transform.localScale.x, parentGO.transform.localScale.y);
            Transform parent = parentGO.transform;
            while (parent.parent != null) {
                parent = parent.parent;
                scaleMultiplyFactor = new Vector2(scaleMultiplyFactor.x * parent.localScale.x, scaleMultiplyFactor.y * parent.localScale.y);
            }
            return scaleMultiplyFactor;
        }
        #endregion

    }
}

