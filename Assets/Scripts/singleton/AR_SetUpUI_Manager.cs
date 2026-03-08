
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using AttitudeSensor = UnityEngine.InputSystem.AttitudeSensor;

public class AR_SetUpUI_Manager : MonoBehaviour
{
    public static AR_SetUpUI_Manager Current;
    void Awake()
    {
        if (Current == null)
        {
            Current = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
    [Header("#AR_SetUpUI")]
    [SerializeField]GameObject _startBackCamButtonObj;
    [SerializeField]GameObject _startAttitudeSensorButtonObj;

    [SerializeField]TextMeshProUGUI _notifierTextToUser;


    [SerializeField]AR_BackGround _aR_BackGround;
    [Header("##ManualCameraSelectorUI")]
    [SerializeField]GameObject _manualCameraSelectorUI_Obj;

    [SerializeField]TextMeshProUGUI _webCumNumberText;


    phase _currentPhase;
    enum phase
    {
        idle,
        waitingForWebCameraPermission,
        waitingForVertical,
        gettingWebCamTexOfBackCamera,

        waitingForAttitudeSensorPermission,
        completed 
    }
    Coroutine _currentSetUpBackCam;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    //ユーザの一回目のボタンクリックによって呼ばれる関数。
    public void StartBackCamButton()
    {
        //一度しかコールチンが走らないようにする。
        if(_currentSetUpBackCam != null) return;
        _startBackCamButtonObj.GetComponent<Button>().enabled = false;
        _currentSetUpBackCam = StartCoroutine(SetUpBackCam());
        IEnumerator SetUpBackCam()
        {
            _currentPhase = phase.waitingForWebCameraPermission;
            StartCoroutine(GetForWebCameraPermission());
            yield return new WaitWhile(()=>_currentPhase == phase.waitingForWebCameraPermission);
            StartCoroutine(RequestVerticalOrientation());
            yield return new WaitWhile(() => _currentPhase == phase.waitingForVertical);
            StartCoroutine(GetWebCamTexOfBackCamera());
            yield return new WaitWhile(() => _currentPhase == phase.gettingWebCamTexOfBackCamera);

        }
        IEnumerator GetForWebCameraPermission()
        {
            while(Application.HasUserAuthorization(UserAuthorization.WebCam) == false)
            {
                _notifierTextToUser.text = "カメラの許可をしてください";
                yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
                _notifierTextToUser.text = "";
            }
            _currentPhase = phase.waitingForVertical;

        }
        IEnumerator RequestVerticalOrientation()
        {
            if (GameManager.Current.IsRunningInEditor){
                _currentPhase = phase.gettingWebCamTexOfBackCamera;
                yield break;
            }

            //スマホが縦の時
            if(Screen.height > Screen.width)
            {
                _currentPhase = phase.gettingWebCamTexOfBackCamera;
            }//スマホが横の時
            else
            {
                _notifierTextToUser.text = "スマホを縦向きにしてください";
                yield return new WaitWhile(()=>!(Screen.height > Screen.width));
                _currentPhase = phase.gettingWebCamTexOfBackCamera;
            }
        }

        IEnumerator GetWebCamTexOfBackCamera()
        {
            _notifierTextToUser.text = "背面カメラを取得中";
            yield return new WaitForSeconds(1f);
            foreach (WebCamDevice webCamDevice in WebCamTexture.devices)
            {
                if (webCamDevice.isFrontFacing == false)
                {
                    GameManager.Current.WebCamTexture = new WebCamTexture(webCamDevice.name);
                    break;
                }
                GameManager.Current.CurrentWebCamIndex++;
            }
            //背面カメラが見つからないとき
            if (GameManager.Current.WebCamTexture == null)
            {
                GameManager.Current.CurrentWebCamIndex = 0;
                int currentWebCamIndex = GameManager.Current.CurrentWebCamIndex;
                currentWebCamIndex = (int)Mathf.Repeat(1, WebCamTexture.devices.Length);
                GameManager.Current.WebCamTexture = new WebCamTexture(WebCamTexture.devices[currentWebCamIndex].name);
                GameManager.Current.CurrentWebCamIndex = currentWebCamIndex;
                _aR_BackGround.StartShowWebCam();

                _notifierTextToUser.text = "背面カメラの取得に失敗、その代わりに<br>１のカメラを表示しました。背面<br>カメラでないなら変更を押してください";
                _startBackCamButtonObj.SetActive(false);
                _manualCameraSelectorUI_Obj.SetActive(true);
            }//背面カメラを検出できた時
            else
            {
                _notifierTextToUser.text = "背面カメラの取得に成功";
                yield return new WaitForSeconds(1f);
                _currentPhase = phase.waitingForAttitudeSensorPermission;
                _notifierTextToUser.text = "";
                _startBackCamButtonObj.SetActive(false);
                _startAttitudeSensorButtonObj.SetActive(true);

            }

        }
    }
    //ユーザの二回目のボタンクリックによって呼ばれる関数。
    bool _isStartAttitudeSensor;
    public void StartAttitudeSensorButton()
    {
        if(_isStartAttitudeSensor) return;
        _isStartAttitudeSensor = true;
        StartAttitudeSensor();
    }
    void StartAttitudeSensor()
    {    
        if (GameManager.Current.IsRunningInEditor){
            StartGame();
            return;
        }    
        if(AttitudeSensor.current == null)
        {
            _notifierTextToUser.text = "姿勢センサーを検出できませんでした。";
            return;
        }


        InputSystem.EnableDevice(AttitudeSensor.current);
        StartCoroutine(WaitForAttitudeSensorEnable());
        IEnumerator WaitForAttitudeSensorEnable()
        {
            yield return new WaitWhile(()=>GameManager.Current.CurrentAttitudeValue == Quaternion.identity);
            _notifierTextToUser.text = "必要な準備が完了。ゲームを開始します。";
            yield return new WaitForSecondsRealtime(1f);
            StartGame();
        }

        void StartGame()
        {
            _currentPhase = phase.completed;
            GameManager.Current.ResetRotation();
            GameManager.Current.LoadTitle();
        }

    }
    //#ここから下はGetWebCamTexOfBackCamera()が上手く背面カメラを検出できなかったとき、手動で設定するために呼ばれる関数群です。
    //##背面カメラを確定すると同時に、姿勢センサーのパーミッション許可を出す。
    public void ConfirmBackCameraButton()
    {
        if(_isStartAttitudeSensor) return;
        _isStartAttitudeSensor = true;
        _notifierTextToUser.text = "次に姿勢センサーを有効化します。";
        _manualCameraSelectorUI_Obj.SetActive(false);
        _currentPhase = phase.waitingForAttitudeSensorPermission;
        StartAttitudeSensor();
    }

    //##webカメラの変更をする処理
    public void ChangeWebCamIndexButton()
    {
        int currentWebCamIndex = GameManager.Current.CurrentWebCamIndex;
        currentWebCamIndex++;
        currentWebCamIndex = (int)Mathf.Repeat(currentWebCamIndex, WebCamTexture.devices.Length);
        if (GameManager.Current.WebCamTexture != null)
        {
            GameManager.Current.WebCamTexture.Stop();
            Destroy(GameManager.Current.WebCamTexture);
        }
        GameManager.Current.WebCamTexture = new WebCamTexture(WebCamTexture.devices[currentWebCamIndex].name, Screen.width, Screen.height, 30);
        _aR_BackGround.StartShowWebCam();
        _webCumNumberText.text = GameManager.Current.CurrentWebCamIndex.ToString();
        GameManager.Current.CurrentWebCamIndex = currentWebCamIndex;
        _notifierTextToUser.text = "";
    }

}
