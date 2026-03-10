using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
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
    public bool FadeInComplete = true;
    [Header("#設定用")]
    public Difficult CurrentDifficult = Difficult.Normal;
    public bool IsRunningInEditor;
    [Header("##シーン遷移時のアニメーション用")]

    [SerializeField]Animator _sceneLoadAnim;
    void Start()
    {
        if(IsRunningInEditor){Debug.Log("エディターモード中です。");}
        //Application.targetFrameRate = 60;
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
        FadeInComplete = false;
        AsyncLoadScene().Forget();
        async UniTaskVoid AsyncLoadScene()
        {

            float assetLoadProgress = _asyncLoad.progress / 0.9f;
            while(assetLoadProgress < 1)
            {
                 assetLoadProgress = _asyncLoad.progress / 0.9f;
                 await UniTask.Yield(PlayerLoopTiming.Update);
            }
            _asyncLoad.allowSceneActivation = true;
           await UniTask.WaitWhile(()=>_asyncLoad.isDone == false);
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
