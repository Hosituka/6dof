using UnityEngine;
using AttitudeSensor = UnityEngine.InputSystem.AttitudeSensor;
using UnityEngine.SceneManagement;
using System.Collections;

//ゲームの各シーンで共有してほしいデータをもたせる物
public class GameManager : MonoBehaviour
{
    public static GameManager Current;
    void Awake()
    {
        if (Current != null){Destroy(gameObject);
            return;
        }
        Current = this;
        DontDestroyOnLoad(gameObject);
    }
    [Header("#表示用")]
    public WebCamTexture WebCamTexture;
    public int CurrentWebCamIndex;
    public bool FadeInComplete = true;
    [Header("#設定用")]
    public Difficult CurrentDifficult = Difficult.Normal;
    public bool IsRunningInEditor;
    [Header("##シーン遷移時のアニメーション用")]

    [System.NonSerialized]public bool StopWebCam;
    [System.NonSerialized]public Quaternion InitialAttitudeValueOffset = Quaternion.identity;
    [System.NonSerialized]public Quaternion CurrentAttitudeValue = Quaternion.identity;

    [SerializeField]Animator _sceneLoadAnim;
    AttitudeSensor _attitudeSensor;

    public void Start()
    {
    }
    public void Update()
    {
        if (WebCamTexture != null)
        {
            if (StopWebCam == true)
            {
                WebCamTexture.Stop();
            }
            else
            {
                if(WebCamTexture.isPlaying == false)
                {
                    WebCamTexture.Play();   
                }

            }

        }
        
        _attitudeSensor = AttitudeSensor.current;
        if(_attitudeSensor != null)
        {
            CurrentAttitudeValue = ConvertQuaternionForUnity(_attitudeSensor.attitude.ReadValue());
            CurrentAttitudeValue = Quaternion.Euler(CurrentAttitudeValue.eulerAngles.x, CurrentAttitudeValue.eulerAngles.y, 0);
        }
    }
    public void ResetRotation()
    {
        _attitudeSensor = AttitudeSensor.current;
        if (_attitudeSensor != null)
        {
            InitialAttitudeValueOffset = ConvertQuaternionForUnity(_attitudeSensor.attitude.ReadValue());
            InitialAttitudeValueOffset = Quaternion.Euler(0, InitialAttitudeValueOffset.eulerAngles.y, 0);
            InitialAttitudeValueOffset = Quaternion.Inverse(InitialAttitudeValueOffset);
        }
    }
    public Quaternion ConvertQuaternionForUnity(Quaternion quaternion)
    {
        return new Quaternion(-quaternion.x, -quaternion.z, -quaternion.y, quaternion.w) * Quaternion.Euler(90f, 0f, 0f);
    }
    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
        Time.timeScale = 1;
    }
    public void LoadTitle()
    {
        LoadScene("title");
        Time.timeScale = 1;
    }
    AsyncOperation _asyncLoad;
    public void LoadScene(string targetSceneName)
    {
        if(_asyncLoad != null) return;
        _sceneLoadAnim.SetTrigger("FadeOut");
        _asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        //シーン遷移が勝手に始まらないようにする。
        _asyncLoad.allowSceneActivation = false;
    }
    //このメソッドはFadeOutと言う名のAnimationClipによるAnimationEventにより呼び出されます。
    public void OnFadeOutComplete()
    {
        Debug.Log("test1");
        FadeInComplete = false;
        StartCoroutine(SliderCoroutine());
        IEnumerator SliderCoroutine()
        {

            float assetLoadProgress = _asyncLoad.progress / 0.9f;
            while(assetLoadProgress < 1)
            {
                 assetLoadProgress = _asyncLoad.progress / 0.9f;
                yield return null;
            }
            _asyncLoad.allowSceneActivation = true;
            yield return new WaitWhile(()=>_asyncLoad.isDone == false);
            _asyncLoad = null;
        }

    }
    //これは遷移先のシーンごとに存在するUI_Managerにより呼び出されます。
    public void StartFadeIn()
    {
        _sceneLoadAnim.SetTrigger("FadeIn");
    }
    //このメソッドはFadeInと言う名のAnimationClipによるAnimationEventにより呼び出されます。
    public void OnFadeInComplete()
    {
        FadeInComplete = true;
    }


}
public enum Difficult
{
    Easy,
    Normal,
    Hard,
}
