using UnityEditor;
using UnityEngine.UIElements;

namespace Daxode.MethodReference.Editor {
    [CustomPropertyDrawer(typeof(MethodRef<>))]
    class SerializedDelegateDr : PropertyDrawer {
        public override VisualElement CreatePropertyGUI(SerializedProperty property) {
            var container = new VisualElement();
            property.Next(true);
            container.AddMethodReferencePicker(property!, fieldInfo.FieldType.GenericTypeArguments[0], preferredLabel);
            return container;
        }
    }
}
