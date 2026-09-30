using Coffee.UIEffects;
using UnityEngine;
using UnityEngine.UI;

public class MeleeAttackView : MonoBehaviour
{
    [SerializeField] private Image _attackIcon;
    [SerializeField] private Sprite[] _icons;
    [SerializeField] private UIEffect _enableSquare;

    private void Awake()
    {
        _attackIcon.sprite = _icons[0];
        if (_enableSquare == null)
        {
            _attackIcon = GetComponentInChildren<Image>();
        }
    }

    public void SetMeleeAttackEnabled(bool isEnabled)
    {
        _attackIcon.sprite = _icons[isEnabled ? 1 : 0];
        _enableSquare.gameObject.SetActive(isEnabled);
    }

}
