// Standalone tests link the production balance/effect code. Unity lifecycle,
// rendering, sound and native component lookup are replaced here; no balance logic is stubbed.
global using Object = UnityEngine.Object;
using System;

namespace UnityEngine
{
    public class Object
    {
        public string name;
        public int GetInstanceID() => GetHashCode();
    }
    public class ScriptableObject : Object { }
    public class Component : Object
    {
        public Transform transform;
        public GameObject gameObject = new();
        public T GetComponentInParent<T>() => default;
        public T GetComponent<T>() => default;
        public bool TryGetComponent<T>(out T value) { value = default; return false; }
    }
    public class MonoBehaviour : Component { public bool isActiveAndEnabled = true; }
    public class Transform : Component { public Transform parent; }
    public class Collider2D : Component { public bool isTrigger; }
    public class GameObject : Object { public T AddComponent<T>() where T : new() => new(); }
    public class AudioClip : Object { }
    public class Sprite : Object { }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r=r; this.g=g; this.b=b; this.a=a; }
        public static Color white => new(1,1,1);
        public static Color red => new(1,0,0);
        public static Color Lerp(Color a, Color b, float t) => new(
            a.r+(b.r-a.r)*t, a.g+(b.g-a.g)*t, a.b+(b.b-a.b)*t, a.a+(b.a-a.a)*t);
    }
    public static class Time { public static double timeAsDouble; }
    public static class Mathf
    {
        public static float Min(float a, float b) => Math.Min(a,b);
        public static float Max(float a, float b) => Math.Max(a,b);
        public static int Max(int a, int b) => Math.Max(a,b);
        public static float Clamp01(float a) => Math.Clamp(a,0,1);
        public static int Clamp(int a, int min, int max) => Math.Clamp(a,min,max);
    }
    [AttributeUsage(AttributeTargets.Field)] public class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public class HeaderAttribute : Attribute { public HeaderAttribute(string s) { } }
    [AttributeUsage(AttributeTargets.Field)] public class TooltipAttribute : Attribute { public TooltipAttribute(string s) { } }
    [AttributeUsage(AttributeTargets.Field)] public class TextAreaAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public class MinAttribute : Attribute { public MinAttribute(float x) { } }
    [AttributeUsage(AttributeTargets.Field)] public class RangeAttribute : Attribute { public RangeAttribute(float a,float b) { } }
    [AttributeUsage(AttributeTargets.Field)] public class InspectorNameAttribute : Attribute { public InspectorNameAttribute(string s) { } }
    [AttributeUsage(AttributeTargets.Class)] public class CreateAssetMenuAttribute : Attribute { public string fileName; public string menuName; }
}

public class PotionStatusVisual : UnityEngine.MonoBehaviour { }
public enum EnemyKind { Reaper }
public class EnemyStatus : IDamageable
{
    public EnemyKind EnemyType;
    public bool IsDead => false;
    public float CurrentHealth => 100;
    public event Action<float,float> OnHealthChanged;
    public event Action OnDeath;
    public void RestoreHealth(float amount) { }
    public void DecreaseHealth(float amount) { }
}
public class SoundInfo { public UnityEngine.AudioClip Clip; public float Volume; }
public static class SoundManager { public static SoundInfo GetSound(int id) => null; }
public static class GameSoundId { public const int VaporizationReaction = 0; }
public static class GameSoundData
{
    public static int GetAreaSoundId(PotionElement element) => 0;
    public static int GetVaporizationReaction() => 0;
}
