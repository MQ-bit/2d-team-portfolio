// using UnityEngine;

// public class PotionDrinkEffect : MonoBehaviour
// {
//     public bool Apply(PotionData data, GameObject targetObject)
//     {
//         if (data == null || targetObject == null)
//         {
//             return false;
//         }

//         IPotionEffectTarget target = PotionEffectResolver.FindTarget(targetObject.transform);
//         if (target == null)
//         {
//             return false;
//         }

//         PotionEffectResolver.ApplyDrinkEffect(data, target);
//         return true;
//     }
// }
