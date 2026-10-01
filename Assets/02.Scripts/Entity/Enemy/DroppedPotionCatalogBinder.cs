using System.Reflection;
using UnityEngine;

// 적이 드랍하는 포션은 런타임에 생성되는 프리팹이라 씬의 PotionRunCatalog를 직렬화로 참조할 수 없다.
// PotionFieldItem은 카탈로그가 비어 있으면 Start에서 스스로 비활성화되므로, 그 전에(Awake) 씬의 카탈로그를 주입한다.
// PotionFieldItem을 수정하지 않기 위한 임시 방편이다.
// TODO : PotionFieldItem이 카탈로그를 직접 찾거나 외부에서 받을 수 있게 되면 이 컴포넌트는 제거한다.
[RequireComponent(typeof(PotionFieldItem))]
public class DroppedPotionCatalogBinder : MonoBehaviour
{
    private const string CatalogFieldName = "_potionRunCatalog";

    private static readonly FieldInfo CatalogField =
        typeof(PotionFieldItem).GetField(CatalogFieldName, BindingFlags.Instance | BindingFlags.NonPublic);

    private void Awake()
    {
        if (CatalogField == null)
        {
            Debug.LogError($"{gameObject.name} : PotionFieldItem.{CatalogFieldName} 필드를 찾을 수 없음");
            return;
        }

        PotionFieldItem fieldItem = GetComponent<PotionFieldItem>();
        if (CatalogField.GetValue(fieldItem) != null)
        {
            return;
        }

        PotionRunCatalog catalog = FindAnyObjectByType<PotionRunCatalog>();
        if (catalog == null)
        {
            Debug.LogError($"{gameObject.name} : 씬에 PotionRunCatalog가 없음");
            return;
        }

        CatalogField.SetValue(fieldItem, catalog);
    }
}
