using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomPropertyDrawer(typeof(ReadOnlyInInspectorAttribute))]
	public class ReadOnlyInInspectorDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
		{
			var valueStr = prop.propertyType switch
			{
				SerializedPropertyType.Integer => prop.intValue.ToString(),
				SerializedPropertyType.Boolean => prop.boolValue.ToString(),
				SerializedPropertyType.Float => prop.floatValue.ToString("0.00000"),
				SerializedPropertyType.String => prop.stringValue,
				_ => "(not supported)"
			};

			EditorGUI.LabelField(position, label.text, valueStr);
		}
	}

	//[CustomPropertyDrawer(typeof(UiScreensAttribute))]
	//public class ScriptableObjectDrawer : PropertyDrawer
	//{
	//	public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
	//	{
	//		base.OnGUI(position, prop, label);
	//	}

	//	[MenuItem("Assets/_ Network Test/ScreenSet", false, -1)]
	//	private static void Create()
	//	{
	//		var mayFolder = Selection.GetFiltered<DefaultAsset>(SelectionMode.Assets | SelectionMode.TopLevel);
	//		foreach (var defaultAsset in mayFolder)
	//		{
	//			if (defaultAsset == null)
	//			{
	//				continue;
	//			}

	//			var path = AssetDatabase.GetAssetPath(defaultAsset);

	//			if (!AssetDatabase.IsValidFolder(path))
	//			{
	//				continue;
	//			}

	//			var root = ScriptableObject.CreateInstance<PresenterRoot>();
	//			root.Asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_content/ui/ui_main.prefab");
	//			root.PresenterDebug = ScriptableObject.CreateInstance<PresenterDebug>();
	//			root.PresenterDebug.Asset = AssetDatabase.LoadAssetAtPath<CompMatch>("Assets/_content/ui/scr_debug.prefab");

	//			AssetDatabase.CreateAsset(root, $"{path}/ui.panel.asset");
	//		}
	//	}
	//}
}
