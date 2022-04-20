using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace i5.SpatialUIConverter {
    internal static class ConverterUtilities {       
        //Converter
        public const string ConverterWindowTitle = "Spatial UI Converter";
        public const string ConverterPackageName = "com.i5.spatial_ui_converter";
        //Paths
#if SPATIAL_UI_CONVERTER_PACKAGE
        public const string ConverterPackageRootPath = "Packages/" + ConverterPackageName;  
        public const string ConverterPackageEditorPath = ConverterPackageRootPath + "/Editor";
        public const string ConverterPackageUIDocumentPath = ConverterPackageEditorPath + "/UI Documents";
        public const string ConverterPackageEditorScriptPaht = ConverterPackageEditorPath + "/Scripts";
#else
        public const string ConverterPackageRootPath = "Assets/i5 Spatial UI Converter";
        public const string ConverterPackageEditorPath = ConverterPackageRootPath + "/Editor";
        public const string ConverterPackageUIDocumentPath = ConverterPackageEditorPath + "/UI Documents";
        public const string ConverterPackageEditorScriptPaht = ConverterPackageEditorPath + "/Scripts";
#endif

        //MRTK
        public const string MRTKVersion = "2.7.3";
        public const string MRTKSDKRootPath = "Packages/com.microsoft.mixedreality.toolkit.foundation/SDK";
    }
}
