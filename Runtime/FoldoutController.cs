using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Events;
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

        public void RegisterEvent() {
            UnityEventTools.AddPersistentListener(GetComponent<Interactable>().OnClick, OpenAndCloseObjectCollection);
        }
    }
}

