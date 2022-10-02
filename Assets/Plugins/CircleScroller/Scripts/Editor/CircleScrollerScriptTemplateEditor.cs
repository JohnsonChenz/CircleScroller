using UnityEditor;

public class CircleScrollerScriptTemplateEditor
{
    private static string TPL_CIRCLE_BUTTON_PATH
    {
        get
        {
            var guidToAssetPath = AssetDatabase.FindAssets("t:TextAsset TplCircleButton.cs");
            return AssetDatabase.GUIDToAssetPath(guidToAssetPath[0]);
        }
    }

    [MenuItem(itemName: "Assets/Create/CircleScroller/TplScripts/TplCircleButton", isValidateFunction: false, priority: 52)]
    public static void CreateEditorFromTemplate()
    {
        ProjectWindowUtil.CreateScriptAssetFromTemplateFile(CircleScrollerScriptTemplateEditor.TPL_CIRCLE_BUTTON_PATH, "NewTplCircleButton.cs");
    }
}
