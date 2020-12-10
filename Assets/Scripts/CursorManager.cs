using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CursorManager : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] float sizingMultiplier = 20f;
    #endregion

    #region Private Fields
    private static CursorManager instance;
    private RectTransform cursorTransform;
    private Vector2 crosshairOriginalSizeDelta;
    #endregion

    #region public Properties
    public static CursorManager Instance { get => instance; }
    #endregion

    #region Unity Callbacks
    void Start()
    {
        instance = this;
        Cursor.visible = false;
        cursorTransform = transform.GetChild(0).GetComponent<RectTransform>();
        crosshairOriginalSizeDelta = cursorTransform.sizeDelta;
    }

    void Update()
    {
        cursorTransform.position = Input.mousePosition;
    }

    private void OnDestroy()
    {
        Cursor.visible = true;
    }
    #endregion

    #region Public Methods
    public void ResizeCrosshair(float inaccuracy)
    {
        var size = 1 + inaccuracy * sizingMultiplier;
        cursorTransform.sizeDelta = crosshairOriginalSizeDelta * size;
    }
    #endregion
}
