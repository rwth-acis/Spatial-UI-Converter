using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace i5.SpatialUIConverter {
    public class FoldoutController : MonoBehaviour {
        // Start is called before the first frame update

        public void OpenAndCloseObjectCollection() {
            GameObject collection = gameObject.transform.Find("GridObjectCollection").gameObject;
            if (collection.activeSelf) {
                collection.SetActive(false);
            }
            else {
                collection.SetActive(true);
            }
        }
#if UNITY_EDITOR
        public void RegisterEvent() {
            UnityEditor.Events.UnityEventTools.AddPersistentListener(GetComponent<Interactable>().OnClick, OpenAndCloseObjectCollection);
        }
#endif
    }
}

