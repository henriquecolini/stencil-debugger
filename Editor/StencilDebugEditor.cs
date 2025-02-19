using UnityEditor;

namespace StencilDebugger.Editor
{
    [CustomEditor(typeof(StencilDebug))]
    public class StencilDebugEditor : UnityEditor.Editor
    {
        private SerializedProperty showInSceneView;
        private SerializedProperty injectionPoint;
        private SerializedProperty scale;
        private SerializedProperty margin;

        private void OnEnable()
        {
            showInSceneView = serializedObject.FindProperty("showInSceneView");
            injectionPoint = serializedObject.FindProperty("injectionPoint");
            scale = serializedObject.FindProperty("scale");
            margin = serializedObject.FindProperty("margin");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(showInSceneView, EditorGUIUtility.TrTextContent("Show In Scene View", "Sets whether to render the pass in the scene view."));
            EditorGUILayout.PropertyField(injectionPoint, EditorGUIUtility.TrTextContent("Stage", "Controls when the render pass executes."));
            EditorGUILayout.PropertyField(scale, EditorGUIUtility.TrTextContent("Scale", "The scale of the stencil digits overlay."));
            EditorGUILayout.PropertyField(margin, EditorGUIUtility.TrTextContent("Margin", "The margin around each stencil digit."));
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}