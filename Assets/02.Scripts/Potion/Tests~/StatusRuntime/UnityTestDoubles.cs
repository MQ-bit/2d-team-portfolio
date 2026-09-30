// Unity 밖 결정적 수치/시간 검증용. Unity 컴파일은 별도로 Rider에서 검증한다.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        private static int _nextId;
        private readonly int _id = ++_nextId;
        public int GetInstanceID() => _id;
    }
    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() => new T();
    }
    public class Sprite : Object { }
    public struct Color
    {
        public static Color red => default;
        public static Color white => default;
    }
    public sealed class CreateAssetMenuAttribute : Attribute
    {
        public string fileName;
        public string menuName;
    }
    public sealed class TextAreaAttribute : Attribute { }
    public sealed class InspectorNameAttribute : Attribute { public InspectorNameAttribute(string _) { } }
    public class Component : Object
    {
        private Transform _transform;
        public Transform transform => this as Transform ?? (_transform ??= new Transform());
    }
    public class Transform : Component
    {
        public Transform parent;
        public readonly List<object> Components = new();
        public bool TryGetComponent<T>(out T value) where T : class
        {
            value = Components.Find(item => item is T) as T;
            return value != null;
        }
    }
    public class MonoBehaviour : Component
    {
        public bool isActiveAndEnabled = true;
        public readonly List<object> Components = new();
        public T GetComponent<T>() where T : class => Components.Find(item => item is T) as T;
        public T GetComponentInParent<T>() where T : class => GetComponent<T>();
    }
    public static class Time
    {
        public static double timeAsDouble;
        public static float deltaTime;
    }
    public static class Mathf
    {
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);
        public static float Clamp01(float value) => Clamp(value, 0, 1);
    }
    public sealed class SerializeField : Attribute { }
    public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string _) { } }
    public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string _) { } }
    public sealed class MinAttribute : Attribute { public MinAttribute(float _) { } }
    public sealed class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    public sealed class DisallowMultipleComponent : Attribute { }
}
