using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpatialUIConverter {
    [CreateAssetMenu(menuName = "Spatial UI Converter/Converter")]
    public class Converter : ScriptableObject {

        //Define the size of base prefabs for better readability. We only consider two-dimensional size (X and Y).
        private static class BaseSize {
            public static readonly Vector2 Backplate = new Vector2(10f, 10f);
            //The base size of label is calculated by using the Width and Height of the "Text" child of the prefab, which is 500 and 200, respectively.
            //And we can see that 500 represents to 25cm, so 200 represents to 10cm.
            public static readonly Vector2 Label = new Vector2(25f, 10f); 
            public static readonly Vector2 Button = new Vector2(3.2f, 3.2f);
        }

        #region Serializable Fields

        [Header("Settings")]
        [Tooltip("The UXML file to be converted.")]      
        [SerializeField] private VisualTreeAsset uxmlToConvert;
        [Tooltip("The USS file to the above UXML file.")]
        [SerializeField] private List<StyleSheet> styleSheets;
        [Tooltip("If true, you only need to set the WIDTH of the backplate, the height will be calculated based on the aspect ratio of the 2D UI backplate.")]
        [SerializeField] private bool preserveAspectRatio = true;
/*        [Tooltip("The width of the backplate.")]
        [SerializeField] private float backplateWidth = 10f;*/
        [Tooltip("The size on X and Y axes of the 3D UI backplate in cm, which represents the \"Canvas Size\" in UI Builder. " +
            "You might choose a size that best suitable for your 2D UI. The ratio between the sizes of VisualElements would be preserved.")]
        [SerializeField] private Vector2 backplateSize = new Vector2(10, 10);


        #endregion

        #region Non-Serializable Fields

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
        }

        //Check if all prefab exist.
        private bool AllPrefabExist() {
            if (!backplateBase) {
                Debug.LogError("UI Backplate is not found under Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/StandardAssets/Prefabs/, make sure you installed MRTK of a version at least 2.7.3.");
                return false;
            }
            if (!buttonBase) {
                Debug.LogError("PressableButtonHoloLens2 is not found under Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/StandardAssets/Prefabs/, make sure you installed MRTK of a version at least 2.7.3.");
                return false;
            }
            if (!labelBase) {
                Debug.LogError("UITextSelawik is not found under Packages/com.microsoft.mixedreality.toolkit.foundation/SDK/StandardAssets/Prefabs/Text/, make sure you installed MRTK of a version at least 2.7.3.");
                return false;
            }
            return true;
        }

        //Convert the children of the parentVE recursively, the recursion terminates on "Controls", e.g. Button, Toggle, etc, but not on "Containers", e.g. Scroll View.
        private void ConvertChildren(VisualElement parentVE, GameObject parentGO, Vector2 convertedSizeParent) {
            foreach (VisualElement childVE in parentVE.Children()) {
                if(childVE is Label) {
                    GameObject label = InstantiateLabel((Label)childVE, parentGO, convertedSizeParent);
                }
                if (childVE is Button) {
                    GameObject button = InstantiateButton((Button)childVE, parentGO, convertedSizeParent);
                }
                if (childVE.GetType() == typeof(Toggle)) {
/*                    foreach (VisualElement ve in childVE.Children()) {
                        if(ve is Label) {
                            Debug.Log(1);
                            ((Label)ve).text = "123";
                        }
                    }*/
                }
            }
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
            label.GetComponent<RectTransform>().localScale = new Vector3(label.GetComponent<RectTransform>().localScale.x / parentGO.transform.localScale.x,
                label.GetComponent<RectTransform>().localScale.y / parentGO.transform.localScale.y, label.GetComponent<RectTransform>().localScale.x / parentGO.transform.localScale.y);
            //The width of the RectTransform of the Text is 500, which represents 25cm in the original scale, then we calculate the height by using the Height/Width ratio of the visual element.
            GameObject textGO = label.transform.Find("Text").gameObject;
            textGO.GetComponent<RectTransform>().sizeDelta = new Vector2(500 * (convertedSizeParent.x / BaseSize.Label.x), 500 * (childVE.resolvedStyle.height / childVE.resolvedStyle.width));
            //We use TextMeshPro instead of TextMesh
            DestroyImmediate(textGO.GetComponent<UnityEngine.UI.Text>());
            TextMeshPro tmp = textGO.AddComponent<TextMeshPro>();
            //For Label, we need set the font size manually, because it use the "RectTransform" and will not be scaled.
            tmp.fontSize = childVE.resolvedStyle.fontSize / (1 / convertedSizeParent.y);
            ResolveTextAndFont(childVE, tmp, false);
            
            //Same code as in ResolveTransform, but we don't consider the scale here, only position.
            Vector2 parentSize = new Vector2(childVE.parent.resolvedStyle.width, childVE.parent.resolvedStyle.height);
            Vector2 selfSize = new Vector2(childVE.resolvedStyle.width, childVE.resolvedStyle.height);
            Vector2 center = new Vector2(childVE.resolvedStyle.left + selfSize.x / 2, childVE.resolvedStyle.top + selfSize.y / 2);
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
            Vector2 center = new Vector2(ve.resolvedStyle.left + selfSize.x / 2, ve.resolvedStyle.top + selfSize.y / 2);
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
        }

        #endregion

    }
}

