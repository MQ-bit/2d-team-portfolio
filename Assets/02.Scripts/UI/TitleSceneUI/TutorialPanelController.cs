using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TutorialPanelController : MonoBehaviour
{
    [SerializeField] private GameObject _tutorialWindowSet;
    [SerializeField] private GameObject[] _pages;
    [SerializeField] private Button[] _escButtons;

    private Sprite[] _escNormalSprites;
    private int _currentPage;
    private bool _isOpen;

    private void Awake()
    {
        if (_tutorialWindowSet == null || _pages == null || _pages.Length == 0)
        {
            Debug.LogError("TutorialPanelController의 창과 페이지를 Inspector에서 연결하세요.");
            enabled = false;
            return;
        }

        if (_escButtons == null)
            _escButtons = new Button[0];

        _escNormalSprites = new Sprite[_escButtons.Length];

        for (int i = 0; i < _escButtons.Length; i++)
        {
            Image image = GetButtonImage(_escButtons[i]);

            if (image != null)
                _escNormalSprites[i] = image.sprite;
        }

        _tutorialWindowSet.SetActive(false);
        _currentPage = 0;
        UpdatePage();
    }

    private void Update()
    {
        if (!_isOpen || Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetEscPressedSprite(true);
        }

        if (Keyboard.current.escapeKey.wasReleasedThisFrame)
        {
            SetEscPressedSprite(false);
            CloseTutorial();
        }
    }

    public void OpenTutorial()
    {
        _currentPage = 0;
        _isOpen = true;

        SetEscPressedSprite(false);
        _tutorialWindowSet.SetActive(true);
        UpdatePage();
    }

    public void CloseTutorial()
    {
        SetEscPressedSprite(false);
        _isOpen = false;
        _tutorialWindowSet.SetActive(false);
    }

    public void NextPage()
    {
        if (!_isOpen)
            return;

        _currentPage = (_currentPage + 1) % _pages.Length;
        UpdatePage();
    }

    public void PreviousPage()
    {
        if (!_isOpen)
            return;

        _currentPage = (_currentPage - 1 + _pages.Length) % _pages.Length;
        UpdatePage();
    }

    private void UpdatePage()
    {
        for (int i = 0; i < _pages.Length; i++)
        {
            if (_pages[i] != null)
                _pages[i].SetActive(i == _currentPage);
        }
    }

    private void SetEscPressedSprite(bool pressed)
    {
        for (int i = 0; i < _escButtons.Length; i++)
        {
            Button button = _escButtons[i];
            Image image = GetButtonImage(button);

            if (button == null || image == null)
                continue;

            Sprite pressedSprite = button.spriteState.pressedSprite;

            if (pressed && pressedSprite != null)
                image.sprite = pressedSprite;
            else
                image.sprite = _escNormalSprites[i];
        }
    }

    private Image GetButtonImage(Button button)
    {
        if (button == null)
            return null;

        return button.targetGraphic as Image;
    }
}