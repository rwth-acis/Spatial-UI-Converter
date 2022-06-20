using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;

namespace i5.SpatialUIConverter {
    /// <summary>
    /// Used to synchronize the slider value and the value field.
    /// </summary>
    public class SliderValueSynchronizer : MonoBehaviour {

        [SerializeField, HideInInspector]
        private PinchSlider slider;
        [SerializeField, HideInInspector]
        private TMP_InputField tmp_input;
        [SerializeField, HideInInspector]
        private float minValue;
        [SerializeField, HideInInspector]
        private float maxValue;
        [SerializeField, HideInInspector]
        private bool isStepSlider;

        private bool canUpdateValue = true;
        private bool needValidateValue = false;

        public void Initialize(PinchSlider slider, TMP_InputField tmp_input, float minValue, float maxValue, bool isStepSlider) {
            this.slider = slider;
            this.tmp_input = tmp_input;
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.isStepSlider = isStepSlider;
        }

        // Update is called once per frame
        void Update() {
            if (!tmp_input.isFocused) {
                if (needValidateValue) {
                    if (tmp_input.text == "") {
                        tmp_input.text = "0";
                    }
                    else {
                        if (isStepSlider) {
                            try {
                                tmp_input.text = Mathf.RoundToInt(Mathf.Clamp(float.Parse(tmp_input.text), minValue, maxValue)).ToString();
                            }
                            catch {
                                tmp_input.text = (slider.SliderValue * (maxValue - minValue) + minValue).ToString();
                            }
                        }
                        else {
                            try {
                                tmp_input.text = Mathf.Clamp(float.Parse(tmp_input.text), minValue, maxValue).ToString();
                            }
                            catch {
                                tmp_input.text = (slider.SliderValue * (maxValue - minValue) + minValue).ToString();
                            }
                        }

                    }
                    needValidateValue = false;
                }

            }
        }

#if UNITY_EDITOR
        public void RegisterEvent() {
            UnityEditor.Events.UnityEventTools.AddPersistentListener(slider.OnValueUpdated, OnSliderValueChange);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(tmp_input.onValueChanged, OnValueFieldChange);
        }
#endif
        public void OnSliderValueChange(SliderEventData evt) {
            if (canUpdateValue) {
                canUpdateValue = false;
                if (isStepSlider) {
                    tmp_input.text = ((int)(slider.SliderValue * (maxValue - minValue) + minValue)).ToString();
                }
                else {
                    tmp_input.text = (slider.SliderValue * (maxValue - minValue) + minValue).ToString();
                }

                canUpdateValue = true;
            }          
        }

        public void OnValueFieldChange(string str) {
            if (canUpdateValue) {
                canUpdateValue = false;
                if (isStepSlider) {
                    int value;
                    try {
                        value = (str == "" ? 0 : Mathf.RoundToInt(float.Parse(str)));
                    }
                    catch {
                        value = (int)(slider.SliderValue * (maxValue - minValue) + minValue);
                    }
                    value = (int)Mathf.Clamp(value, minValue, maxValue);
                    slider.SliderValue = (value - minValue) / (maxValue - minValue);
                }
                else {
                    float value;
                    try {
                        value = (str == "" ? 0 : float.Parse(str));
                    }
                    catch {
                        value = slider.SliderValue * (maxValue - minValue) + minValue;
                    }
                    value = Mathf.Clamp(value, minValue, maxValue);
                    slider.SliderValue = (value - minValue) / (maxValue - minValue);
                }
                canUpdateValue = true;
                needValidateValue = true;
            }
        }

    }
}

