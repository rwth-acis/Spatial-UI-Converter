using Microsoft.MixedReality.Toolkit.Experimental.UI;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Input.Utilities;
using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpatialUIConverter {
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
        }

        private enum ToggleType {
            CheckBox,
            Button,
            Radio,
            Switch
        }

        #region Serializable Fields

        [Header("Settings")]
        [Tooltip("The UXML file to be converted.")]      
        [SerializeField] private VisualTreeAsset uxmlToConvert;
        [Tooltip("The USS file to the above UXML file.")]
        [SerializeField] private List<StyleSheet> styleSheets;
        [Tooltip("The type of the converted \"Toggle\", it only defines the visual themes. If your toggle is originally toggled, remember to set the \"IsToggled\" property in the \"Interactable\" manually.")]
        [SerializeField] private ToggleType toggleType = ToggleType.CheckBox;
        [Tooltip("If true, you only need to set the WIDTH of the backplate, the height will be calculated based on the aspect ratio of the 2D UI backplate.")]
        [SerializeField] private bool preserveAspectRatio = true;
/*        [Tooltip("The width of the backplate.")]
        [SerializeField] private float backplateWidth = 10f;*/
        [Tooltip("The size on X and Y axes of the 3D UI backplate in cm, which represents the \"Canvas Size\" in UI Builder. " +
            "You might choose a size that best suitable for your 2D UI. The ratio between the sizes of VisualElements would be preserved.")]
        [SerializeField] private Vector2 backplateSize = new Vector2(10, 10);


        #endregion

        #region Non-Serializable Fields

        //defines
        private static readonly string mrtkVersion = "2.7.3";

        //The resolved UI from the UXML
        private TemplateContainer uiToConvert;
        //The height/width of the backplate, only used if preserveAspectRatio is true.
        private float heightWidthRatio;
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
            //Make sure the toggleBase corresponds to the toggleType.
            toggleBase = SetToggleBase();
            if (!AllPrefabExist()) {
                return;
            }
            if(uiToConvert == null) {
                Debug.LogError("Nothing to convert");
                return;
            }
            //Recalculate the backplateSize if user wants to preserve the aspect ratio
            if (preserveAspectRatio) {
                heightWidthRatio = UIToConvert.resolvedStyle.height / UIToConvert.resolvedStyle.width;
                backplateSize = new Vector2(backplateSize.x, backplateSize.x * heightWidthRatio);
            }
            else {
                heightWidthRatio = backplateSize.y / backplateSize.x;
            }
            GameObject backplate = InstantiateBackplate();
            ConvertChildren(UIToConvert, backplate, backplateSize);
        }

        #endregion

        #region Private Methods

        private void OnEnable() {
            backplateBase = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/StandardAssets/Prefabs/UI Backplate.prefab");
            buttonBase = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Features/UX/Interactable/Prefabs/PressableButtonHoloLens2.prefab");
            labelBase = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/StandardAssets/Prefabs/Text/UITextSelawik.prefab");
            toggleBase = SetToggleBase();
            sliderBase = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Features/UX/Prefabs/Sliders/PinchSlider.prefab");
            stepSliderBase = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Experimental/StepSlider/StepSlider.prefab");
            textFieldBase = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Experimental/MixedRealityKeyboard/Prefabs/MRKeyboardInputField_TMP.prefab");
        }

        private GameObject SetToggleBase() {
            GameObject toggleBase = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Features/UX/Interactable/Prefabs/PressableButtonHoloLens2ToggleCheckBox_32x32.prefab");
            switch (toggleType) {
                case ToggleType.CheckBox:
                    toggleBase = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Features/UX/Interactable/Prefabs/PressableButtonHoloLens2ToggleCheckBox_32x32.prefab");
                    break;
                case ToggleType.Button:
                    toggleBase = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Features/UX/Interactable/Prefabs/PressableButtonHoloLens2Toggle.prefab");
                    break;
                case ToggleType.Radio:
                    toggleBase = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Features/UX/Interactable/Prefabs/PressableButtonHoloLens2ToggleRadio_32x32.prefab");
                    break;
                case ToggleType.Switch:
                    toggleBase = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Features/UX/Interactable/Prefabs/PressableButtonHoloLens2ToggleSwitch_32x32.prefab");
                    break;
            }
            return toggleBase;
        }

        //Check if all prefab exist.
        private bool AllPrefabExist() {
            if (!backplateBase) {
                Debug.LogError($"UI Backplate is not found under Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/StandardAssets/Prefabs/, make sure you installed MRTK of a version at least {mrtkVersion}");
                return false;
            }
            if (!buttonBase) {
                Debug.LogError($"PressableButtonHoloLens2 is not found under Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Features/UX/Interactable/Prefabs/, make sure you installed MRTK of a version at least {mrtkVersion}.");
                return false;
            }
            if (!labelBase) {
                Debug.LogError($"UITextSelawik is not found under Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/StandardAssets/Prefabs/Text/, make sure you installed MRTK of a version at least {mrtkVersion}.");
                return false;
            }
            if (!toggleBase) {
                Debug.LogError($"PressableButtonHoloLens2Toggle is not found under Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Features/UX/Interactable/Prefabs/, make sure you installed MRTK of a version at least {mrtkVersion}.");
                return false;
            }
            if (!sliderBase) {
                Debug.LogError($"PinchSlider is not found under Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Features/UX/Prefabs/Sliders/, make sure you installed MRTK of a version at least {mrtkVersion}.");
                return false;
            }
            if (!stepSliderBase) {
                Debug.LogError($"StepSlider is not found under Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Experimental/StepSlider/, make sure you installed MRTK of a version at least {mrtkVersion}.");
            }
            if (!textFieldBase) {
                Debug.LogError($"MRKeyboardInputField_TMP is not found under Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/Experimental/MixedRealityKeyboard/Prefabs/, make sure you installed MRTK of a version at least {mrtkVersion}.");
            }
            return true;
        }

        //Convert the children of the parentVE recursively, the recursion terminates on "Controls", e.g. Button, Toggle, etc, but not on "Containers", e.g. Scroll View.
        private void ConvertChildren(VisualElement parentVE, GameObject parentGO, Vector2 convertedSizeParent) {
            foreach (VisualElement childVE in parentVE.Children()) {
                if(childVE is Label) {
                    GameObject label = InstantiateLabel((Label)childVE, parentGO, convertedSizeParent);
                }else if (childVE is Button) {
                    GameObject button = InstantiateButton((Button)childVE, parentGO, convertedSizeParent);
                }else if (childVE is Toggle) {
                    GameObject toggle = InstantiateToggle((Toggle)childVE, parentGO, convertedSizeParent);
                }else if (childVE is Slider) {
                    GameObject slider = InstantiateSlider((Slider)childVE, parentGO, convertedSizeParent);
                }else if (childVE is SliderInt) {
                    GameObject sliderInt = InstantiateSliderInt((SliderInt)childVE, parentGO, convertedSizeParent);
                }else if (childVE is TextField) {
                    GameObject textField = InstantiateTextField((TextField)childVE, parentGO, convertedSizeParent);
                }
            }
        }

        //Instantiate the backplate with the given size in cm.
        private GameObject InstantiateBackplate() {           
            GameObject backplate = Instantiate(backplateBase, Vector3.zero, Quaternion.identity);
            backplate.name = "Converted UI";
            backplate.GetComponent<BoxCollider>().enabled = false;
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
            Vector2 scaleMultiplyFactor = new Vector2(parentGO.transform.localScale.x, parentGO.transform.localScale.y);
            Transform parent = parentGO.transform;
            while (parent.parent != null) {
                parent = parent.parent;
                scaleMultiplyFactor = new Vector2(scaleMultiplyFactor.x * parent.localScale.x, scaleMultiplyFactor.y * parent.localScale.y);               
            }
            label.GetComponent<RectTransform>().localScale = new Vector3(label.GetComponent<RectTransform>().localScale.x / scaleMultiplyFactor.x,
                label.GetComponent<RectTransform>().localScale.y / scaleMultiplyFactor.y, 1);
            GameObject textGO = label.transform.Find("Text").gameObject;            
            //We use TextMeshPro instead of TextMesh
            DestroyImmediate(textGO.GetComponent<UnityEngine.UI.Text>());
            TextMeshPro tmp = textGO.AddComponent<TextMeshPro>();
            //For Label, we need set the font size manually, because it use the "RectTransform" and will not be scaled.
            //A little bit magic, but works well. A manual modification of the font size may be needed.
            tmp.fontSize = childVE.resolvedStyle.fontSize / (1 / backplateSize.y);
            ResolveTextAndFont(childVE, tmp, false);

            
            //Same code as in ResolveTransform, but we don't consider the scale here, only position.
            Vector2 parentSize = new Vector2(childVE.parent.resolvedStyle.width, childVE.parent.resolvedStyle.height);
            Vector2 selfSize = new Vector2(childVE.resolvedStyle.width, childVE.resolvedStyle.height);
            Vector2 center = new Vector2(childVE.resolvedStyle.left + selfSize.x / 2, childVE.resolvedStyle.top + selfSize.y / 2);
            Vector2 selfParentRatio = new Vector2(selfSize.x / parentSize.x, selfSize.y / parentSize.y);
            Vector2 convertedSizeSelf = new Vector2(selfParentRatio.x * convertedSizeParent.x, selfParentRatio.y * convertedSizeParent.y);

            //The width of the RectTransform of the Text is 500, which represents 25cm in the original scale, then we calculate the height by using the Height/Width ratio of the visual element.
            textGO.GetComponent<RectTransform>().sizeDelta = new Vector2(500 * (convertedSizeSelf.x / BaseSize.Label.x), 
                500 * (convertedSizeSelf.x / BaseSize.Label.x) * (childVE.resolvedStyle.height / childVE.resolvedStyle.width));

            Vector2 centerDistance = new Vector2(center.x - parentSize.x / 2, parentSize.y / 2 - center.y);
            Vector2 convertingRatio = new Vector2(convertedSizeParent.x / parentSize.x, convertedSizeParent.y / parentSize.y);
            Vector2 convertedCenterDistance = new Vector2(centerDistance.x * convertingRatio.x, centerDistance.y * convertingRatio.y);
            Vector2 convertedCenter = new Vector2(convertedCenterDistance.x / (convertedSizeParent.x / 2) * 0.5f, convertedCenterDistance.y / (convertedSizeParent.y / 2) * 0.5f);
            label.GetComponent<RectTransform>().localPosition = new Vector3(convertedCenter.x, convertedCenter.y, label.GetComponent<RectTransform>().localPosition.z);
            
            return label;
        }

        //Instantiate a button, and set its appearance accroding to childVE.resolvedStyle
        private GameObject InstantiateButton(Button childVE, GameObject parentGO, Vector2 convertedSizeParent) {
            GameObject button = Instantiate(buttonBase);
            button.name = childVE.name == "" ? "Button" : childVE.name;
            button.transform.parent = parentGO.transform;
            button.transform.localPosition = new Vector3(0, 0, -1);
            //We don't call ConvertChildren on Button, so we discard the output.
            ResolveStyle(childVE, button, BaseSize.Button, convertedSizeParent);
            ResolveTextAndFont(childVE, button.transform.Find("IconAndText/TextMeshPro").gameObject.GetComponent<TextMeshPro>(), true);
            //We scale the IconAndText of the hololens button prefab to a square for better appearance, and we keep the larger localScale on axis X and Y to 1,
            //which means we keep the final localScale on both axes smaller or equal to 1.
            //Similarly, we scale the SeeItSayItLabel.
            if (button.transform.localScale.x > button.transform.localScale.y) {
                button.transform.Find("IconAndText").localScale = new Vector3(button.transform.localScale.y / button.transform.localScale.x * heightWidthRatio, 1, 1);
                button.transform.Find("SeeItSayItLabel").transform.localScale = new Vector3(button.transform.localScale.y / button.transform.localScale.x * heightWidthRatio, 1, 1);
            }
            else {
                button.transform.Find("IconAndText").localScale = new Vector3(1, button.transform.localScale.x / button.transform.localScale.y / heightWidthRatio, 1);
                button.transform.Find("SeeItSayItLabel").transform.localScale = new Vector3(1, button.transform.localScale.x / button.transform.localScale.y / heightWidthRatio, 1);
            }           
            return button;
        }

        private GameObject InstantiateToggle(Toggle childVE, GameObject parentGO, Vector2 convertedSizeParent) {
            GameObject toggle = Instantiate(toggleBase);
            toggle.name = childVE.name == "" ? "Toggle" : childVE.name;
            toggle.transform.parent = parentGO.transform;
            toggle.transform.localPosition = new Vector3(0, 0, -1);
            if (childVE.value) {
                Debug.Log($"The VisualElement toggle {childVE} is toggled, remember to set the IsToggled property of the Interactable of {toggle} to true.");
            }
            ResolveStyle(childVE, toggle, BaseSize.Toggle, convertedSizeParent);
            ResolveTextAndFont(childVE.Q<Label>(), toggle.transform.Find("IconAndText/TextMeshPro").gameObject.GetComponent<TextMeshPro>(), true);
            //Scale the IconAndText
            if (toggle.transform.localScale.x > toggle.transform.localScale.y) {
                toggle.transform.Find("IconAndText").localScale = new Vector3(toggle.transform.localScale.y / toggle.transform.localScale.x * heightWidthRatio, 1, 1);
                toggle.transform.Find("SeeItSayItLabel").transform.localScale = new Vector3(toggle.transform.localScale.y / toggle.transform.localScale.x * heightWidthRatio, 1, 1);
            }
            else {
                toggle.transform.Find("IconAndText").localScale = new Vector3(1, toggle.transform.localScale.x / toggle.transform.localScale.y / heightWidthRatio, 1);
                toggle.transform.Find("SeeItSayItLabel").transform.localScale = new Vector3(1, toggle.transform.localScale.x / toggle.transform.localScale.y / heightWidthRatio, 1);
            }
            return toggle;
        }

        private GameObject InstantiateSlider(Slider childVE, GameObject parentGO, Vector2 convertedSizeParent) {
            GameObject slider = Instantiate(backplateBase);
            slider.GetComponent<BoxCollider>().enabled = false;
            slider.name = childVE.name == "" ? "SliderInt" : childVE.name;
            slider.transform.parent = parentGO.transform;
            slider.transform.localPosition = new Vector3(0, 0, -0.5f);

            VisualElement childSliderVE = childVE.Q<VisualElement>(name: "unity-drag-container");
            Vector2 convertedSizeWholeSlider = ResolveStyle(childVE, slider, BaseSize.Backplate, convertedSizeParent);

            //Instantiate a label, which corresponds to the label on the right side of the whole VisualElement Slider.
            Label childLabelVE = childVE.Q<Label>();
            GameObject label = InstantiateLabel(childLabelVE, slider, convertedSizeWholeSlider);
            label.GetComponent<RectTransform>().localPosition = new Vector3(label.GetComponent<RectTransform>().localPosition.x, label.GetComponent<RectTransform>().localPosition.y, -0.01f);

            //The container of the slider part of the VisualElement "Slider", it may also conatain the TextField of the slider value, if showInputField is set to true.
            VisualElement sliderContainerVE = childVE.Query<VisualElement>().AtIndex(2);
            GameObject sliderContainerGO = new GameObject("SliderContainer");
            sliderContainerGO.transform.parent = slider.transform;
            sliderContainerGO.transform.localPosition = Vector3.zero;
            Vector2 convertedSizeSilderContainer = ResolveStyle(sliderContainerVE, sliderContainerGO, BaseSize.EmptyGameObject, convertedSizeWholeSlider);

            //Instantiate the slider part.
            GameObject childSliderGO = Instantiate(sliderBase);
            childSliderGO.GetComponent<PinchSlider>().SliderValue = (childVE.value - childVE.lowValue) / (childVE.highValue - childVE.lowValue);    
            childSliderGO.transform.parent = sliderContainerGO.transform;
            childSliderGO.transform.localPosition = new Vector3(0, 0, -1 / sliderContainerGO.transform.localScale.z);
            Vector2 convertedSizePinchSlider = ResolveStyle(childSliderVE, childSliderGO, BaseSize.Slider, convertedSizeSilderContainer);
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
            return slider;
        }

        private GameObject InstantiateSliderInt(SliderInt childVE, GameObject parentGO, Vector2 convertedSizeParent) {
            GameObject slider = Instantiate(backplateBase);
            slider.GetComponent<BoxCollider>().enabled = false;
            slider.name = childVE.name == "" ? "Slider" : childVE.name;
            slider.transform.parent = parentGO.transform;
            slider.transform.localPosition = new Vector3(0, 0, -0.5f);

            VisualElement childSliderVE = childVE.Q<VisualElement>(name: "unity-drag-container");
            Vector2 convertedSizeWholeSlider = ResolveStyle(childVE, slider, BaseSize.Backplate, convertedSizeParent);

            //Instantiate a label, which corresponds to the label on the right side of the whole VisualElement Slider.
            Label childLabelVE = childVE.Q<Label>();
            GameObject label = InstantiateLabel(childLabelVE, slider, convertedSizeWholeSlider);
            label.GetComponent<RectTransform>().localPosition = new Vector3(label.GetComponent<RectTransform>().localPosition.x, label.GetComponent<RectTransform>().localPosition.y, -0.01f);

            //The container of the slider part of the VisualElement "Slider", it may also conatain the TextField of the slider value, if showInputField is set to true.
            VisualElement sliderContainerVE = childVE.Query<VisualElement>().AtIndex(2);
            GameObject sliderContainerGO = new GameObject("SliderContainer");
            sliderContainerGO.transform.parent = slider.transform;
            sliderContainerGO.transform.localPosition = Vector3.zero;
            Vector2 convertedSizeSilderContainer = ResolveStyle(sliderContainerVE, sliderContainerGO, BaseSize.EmptyGameObject, convertedSizeWholeSlider);

            //Instantiate the slider part.
            GameObject childSliderGO = Instantiate(stepSliderBase);
            childSliderGO.GetComponent<StepSlider>().SliderStepDivisions = childVE.highValue - childVE.lowValue;
            childSliderGO.GetComponent<StepSlider>().SliderValue = (childVE.value - childVE.lowValue) / (childVE.highValue - childVE.lowValue);
            childSliderGO.transform.parent = sliderContainerGO.transform;
            childSliderGO.transform.localPosition = new Vector3(0, 0, -1 / sliderContainerGO.transform.localScale.z);
            Vector2 convertedSizePinchSlider = ResolveStyle(childSliderVE, childSliderGO, BaseSize.Slider, convertedSizeSilderContainer);
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
            return slider;
        }

        private GameObject InstantiateTextField(TextField childVE, GameObject parentGO, Vector2 convertedSizeParent) {
            GameObject textField = Instantiate(backplateBase);
            textField.GetComponent<BoxCollider>().enabled = false;
            textField.name = childVE.name == "" ? "TextField" : childVE.name;
            textField.transform.parent = parentGO.transform;
            textField.transform.localPosition = new Vector3(0, 0, -0.5f);
            Vector2 convertedSizeTextField = ResolveStyle(childVE, textField, BaseSize.Backplate, convertedSizeParent);

            //Instantiate a label, which corresponds to the label on the right side of the whole VisualElement TextField.
            Label childLabelVE = childVE.Q<Label>();
            GameObject label = InstantiateLabel(childLabelVE, textField, convertedSizeParent);
            label.GetComponent<RectTransform>().localPosition = new Vector3(label.GetComponent<RectTransform>().localPosition.x, label.GetComponent<RectTransform>().localPosition.y, -0.01f);

            //Instantiate the input field part.
            GameObject inputField = InstantiateInputField(childVE.Q<VisualElement>(name: "unity-text-input"), textField, convertedSizeTextField);
            inputField.GetComponentInChildren<TMP_InputField>().text = childVE.text;

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
            Vector2 scaleMultiplyFactor = new Vector2(parentGO.transform.localScale.x, parentGO.transform.localScale.y);
            Transform parent = parentGO.transform;
            while (parent.parent != null) {
                parent = parent.parent;
                scaleMultiplyFactor = new Vector2(scaleMultiplyFactor.x * parent.localScale.x, scaleMultiplyFactor.y * parent.localScale.y);
            }
            textFieldCanvas.GetComponent<RectTransform>().localScale = new Vector3(textFieldCanvas.GetComponent<RectTransform>().localScale.x / scaleMultiplyFactor.x,
                textFieldCanvas.GetComponent<RectTransform>().localScale.y / scaleMultiplyFactor.y, 1);
            
            GameObject textGO = textFieldCanvas.transform.Find("Text").gameObject;

            GameObject inputField = Instantiate(textFieldBase, textFieldCanvas.transform);
            inputField.transform.position = new Vector3(inputField.transform.position.x, inputField.transform.position.y, -0.01f);
            TMP_InputField tmp_input = inputField.GetComponent<TMP_InputField>();

            DestroyImmediate(textGO);
            
            //Same code as in ResolveTransform, but we don't consider the scale here, only position.
            Vector2 parentSize = new Vector2(childVE.parent.resolvedStyle.width, childVE.parent.resolvedStyle.height);
            Vector2 selfSize = new Vector2(childVE.resolvedStyle.width, childVE.resolvedStyle.height);
            Vector2 center = new Vector2(childVE.resolvedStyle.left + selfSize.x / 2, childVE.resolvedStyle.top + selfSize.y / 2);
            Vector2 selfParentRatio = new Vector2(selfSize.x / parentSize.x, selfSize.y / parentSize.y);
            Vector2 convertedSizeSelf = new Vector2(selfParentRatio.x * convertedSizeParent.x, selfParentRatio.y * convertedSizeParent.y);

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

            Vector2 centerDistance = new Vector2(center.x - parentSize.x / 2, parentSize.y / 2 - center.y);
            Vector2 convertingRatio = new Vector2(convertedSizeParent.x / parentSize.x, convertedSizeParent.y / parentSize.y);
/*            Vector2 scale = new Vector2(convertedSizeSelf.x / BaseSize.TextField.x * inputField.transform.localScale.x, convertedSizeSelf.y / BaseSize.TextField.y * inputField.transform.localScale.y);
            inputField.transform.localScale = new Vector3(scale.x, scale.y, inputField.transform.localScale.z);*/
            Vector2 convertedCenterDistance = new Vector2(centerDistance.x * convertingRatio.x, centerDistance.y * convertingRatio.y);
            Vector2 convertedCenter = new Vector2(convertedCenterDistance.x / (convertedSizeParent.x / 2) * 0.5f, convertedCenterDistance.y / (convertedSizeParent.y / 2) * 0.5f);
            textFieldCanvas.GetComponent<RectTransform>().localPosition = new Vector3(convertedCenter.x, convertedCenter.y, textFieldCanvas.GetComponent<RectTransform>().localPosition.z);

            return textFieldCanvas;
        }

        //Resolve the style info and apply it correspondingly to the base prefab, return the converted size of the basePrefab in cm.
        private Vector2 ResolveStyle(VisualElement ve, GameObject basePrefab, Vector2 baseSizeSelf, Vector2 convertedSizeParent) {
            Vector2 convertedSizeSelf = ResolveTransform(ve, basePrefab, baseSizeSelf, convertedSizeParent);
            return convertedSizeSelf;
        }

        //Resolve the transform (position and scale) of the basePrefab repecting to its parent, return the converted size of the basePrefab in cm.
        private Vector2 ResolveTransform(VisualElement ve, GameObject basePrefab, Vector2 baseSizeSelf, Vector2 convertedSizeParent) {
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
            Vector2 scale = new Vector2(convertedSizeSelf.x / baseSizeSelf.x * basePrefab.transform.localScale.x, convertedSizeSelf.y / baseSizeSelf.y * basePrefab.transform.localScale.y);
            basePrefab.transform.localScale = new Vector3(scale.x, scale.y, basePrefab.transform.localScale.z);
            Vector2 convertedCenterDistance = new Vector2(centerDistance.x * convertingRatio.x, centerDistance.y * convertingRatio.y);
            //Calculate the position (center) of the basePrefab. We know localPositon.x=0.5 is on the right border of the parent, etc.
            //So we calculate here the ratio between the convertedCenterDistance and the half the the width/height of the parent's size, and then multiply with 0.5 as mentioned above.
            Vector2 convertedCenter = new Vector2(convertedCenterDistance.x / (convertedSizeParent.x / 2) * 0.5f, convertedCenterDistance.y / (convertedSizeParent.y / 2) * 0.5f);
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
        }

        #endregion

    }
}

