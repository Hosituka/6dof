using UnityEngine;

public class TitleUI_Manager : MonoBehaviour
{
    public static TitleUI_Manager Current;
    void Awake()
    {
        if (Current != null){Destroy(gameObject);
            return;
        }
        Current = this;
    }
    [SerializeField]AR_BackGround _aR_BackGround;
    void Start()
    {
        GameManager.Current.StartFadeIn();
        _aR_BackGround.BeginShowWebCam();
    }
}
