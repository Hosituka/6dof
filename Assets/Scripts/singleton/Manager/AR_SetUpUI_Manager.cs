
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class AR_SetUpUI_Manager : MonoBehaviour
{
    public static AR_SetUpUI_Manager Current;
    void Awake()
    {
        if (Current != null){Destroy(gameObject);
            return;
        }
        Current = this;
    }
    [Header("#AR_SetUpUI")]
    [SerializeField]GameObject _startBackCamButtonObj;
    [SerializeField]GameObject _startAttitudeSensorButtonObj;

    [SerializeField]TextMeshProUGUI _notifierTextToUser;


    [SerializeField]AR_BackGround _aR_BackGround;
    [Header("##ManualCameraSelectorUI")]
    [SerializeField]GameObject _manualCameraSelectorUI_Obj;

    [SerializeField]TextMeshProUGUI _webCumNumberText;


    bool _isSettingUpBackCam;
    //ユーザの一回目のボタンクリックによって呼ばれる関数。
    public async void  StartBackCamButton()
    {
        //#一度しかこの関数が走らないようにする。
        if(_isSettingUpBackCam) return;
        _startBackCamButtonObj.GetComponent<Button>().enabled = false;
        _isSettingUpBackCam = true;

        await GetForWebCameraPermission();
        await RequestVerticalOrientation();
        await GetWebCamTexOfBackCamera();

        async UniTask GetForWebCameraPermission()
        {
            if(WebCamTexture.devices.Length == 0){
                _notifierTextToUser.text = "エラー：webカメラが一つも検出できませんでした。";
                return;
            }
            

            while(Application.HasUserAuthorization(UserAuthorization.WebCam) == false)
            {
                _notifierTextToUser.text = "カメラの許可をしてください";
                await Application.RequestUserAuthorization(UserAuthorization.WebCam);
                _notifierTextToUser.text = "";
            }

        }
        async UniTask RequestVerticalOrientation()
        {
            if (GameManager.Current.IsRunningInEditor){
                return;
            }

            //スマホが横向きの時
            if(Screen.height < Screen.width)
            {
                _notifierTextToUser.text = "スマホを縦向きにしてください";
                await UniTask.WaitWhile(()=>!(Screen.height > Screen.width));
            }
        }

        async UniTask GetWebCamTexOfBackCamera()
        {
            _notifierTextToUser.text = "背面カメラを取得中";
            await UniTask.WaitForSeconds(1f);
            //#背面カメラを自動検出できたか否かの処理
            //##背面カメラを検出できた時
            if (_aR_BackGround.TrySetBackCamTexture())
            {
                _aR_BackGround.BeginShowWebCam();
                _notifierTextToUser.text = "背面カメラの取得に成功";
                await UniTask.WaitForSeconds(1f);
                _notifierTextToUser.text = "";
                _startBackCamButtonObj.SetActive(false);
                _startAttitudeSensorButtonObj.SetActive(true);


            }//##背面カメラが見つからないとき
            else
            {
                _aR_BackGround.SetWebCamTex(0);
                _aR_BackGround.BeginShowWebCam();

                _notifierTextToUser.text = "背面カメラの取得に失敗、その代わりに<br>１のカメラを表示しました。背面<br>カメラでないなら変更を押してください";
                _startBackCamButtonObj.SetActive(false);
                _manualCameraSelectorUI_Obj.SetActive(true);

            }

        }

    }
    //ユーザの二回目のボタンクリックによって呼ばれる関数。モーションセンサー群の許可を求める
    bool _isSettingUpMotionSensors;
    public void StartMotionSensorsButton()
    {
        if(_isSettingUpMotionSensors) return;
        _isSettingUpMotionSensors = true;
        StartMotionSensors().Forget();
    }
    async UniTaskVoid StartMotionSensors()
    {    
        if (GameManager.Current.IsRunningInEditor){
            StartGame();
            return;
        } 
        MotionSensorPhase motionSensorPhase = await MotionSensorUtility.StartMotionSensors();   
        if(motionSensorPhase == MotionSensorPhase.NotFoundAttitudeSensor){
            _notifierTextToUser.text = "エラー：姿勢センサーを検出できませんでした。"; 
            return;
        }
        if(motionSensorPhase == MotionSensorPhase.NotFoundLinearAccelerationSensor){
            _notifierTextToUser.text = "エラー：線形加速度センサーを検出できませんでした。";
            return;
        }
        
        _notifierTextToUser.text = "必要な準備が完了。ゲームを開始します。";
        await UniTask.WaitForSeconds(1f);

        StartGame();
        void StartGame()
        {
            MotionSensorUtility.ResetRotation();
            GameManager.Current.LoadTitle();
        }

    }
    //#ここから下はGetWebCamTexOfBackCamera()が上手く背面カメラを検出できなかったとき、手動で設定するために呼ばれる関数群です。
    //##背面カメラを確定すると同時に、姿勢センサーのパーミッション許可を出す。
    public void ConfirmBackCameraButton()
    {
        if(_isSettingUpMotionSensors) return;
        _isSettingUpMotionSensors = true;
        _notifierTextToUser.text = "次にモーションセンサー群を有効化します。";
        _manualCameraSelectorUI_Obj.SetActive(false);
        StartMotionSensors().Forget();
    }

    //##webカメラの変更をする処理
    public void ChangeWebCamIndexButton()
    {
        int selectedWebCamIndex = AR_BackGround.SelectedWebCamIndex;
        selectedWebCamIndex++;
        selectedWebCamIndex = (int)Mathf.Repeat(selectedWebCamIndex, WebCamTexture.devices.Length);
        _aR_BackGround.StopWebCamTex();
        _aR_BackGround.SetWebCamTex(selectedWebCamIndex);
        _aR_BackGround.BeginShowWebCam();
        _webCumNumberText.SetText("{0:0}",AR_BackGround.SelectedWebCamIndex);
        _notifierTextToUser.text = "";
    }

}
