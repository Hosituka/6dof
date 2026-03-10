using UnityEngine;
using Cysharp.Threading.Tasks;
//Webカメラからの映像を投影する役割を持つクラスです。
public class AR_BackGround : MonoBehaviour
{
    public static WebCamTexture WebCamTexture;
    public static int SelectedWebCamIndex;
    public static bool IsStopWebCam{get; private set;}
    [SerializeField]Camera _playerCam;
    [SerializeField]ShowWebCamPhase _showWebCamPhase;
    Transform _playerCamTr;
    
    enum ShowWebCamPhase{
        idle,
        //webカメラのテクスチャが開始されて、実際に解像度などが決まるのを待機する状態
        waitingWebCamPlay,

        //webカメラのテクスチャがセットされるフェーズ
        waitingWebCamSetToPropBlock,
        //スクリーンとwebCamTexのアスペクト比較、それによるスケール計算
        calculatingScale,
        //transformのscaleに反映
        Completed,
    }
    MeshRenderer _meshRenderer;
    float _startDistance;
    [SerializeField]WebCamTexture _webCamTexture;
    void LateUpdate()
    {
        if(_showWebCamPhase != ShowWebCamPhase.Completed) return;
        transform.position = _playerCamTr.position + _playerCamTr.forward * _startDistance;
        transform.rotation = Quaternion.LookRotation(_playerCamTr.forward);
    }
    public void BeginShowWebCam()
    {
        ShowWebCam().Forget();
    }
    async UniTaskVoid ShowWebCam()
    {
        Initialize();
        await PlayWebCamTex();
        SetWebCamTexToPropBlock();
        CaluculateScale();

        void Initialize()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _playerCamTr = _playerCam.transform;
            //プレイヤーのカメラからの距離を取得
            _startDistance = Vector3.Distance(_playerCamTr.position,transform.position);
            _showWebCamPhase = ShowWebCamPhase.waitingWebCamPlay;
        }

        async UniTask PlayWebCamTex()
        {
            WebCamTexture.Play();
            await UniTask.WaitWhile(()=>WebCamTexture.isPlaying == false);
            await UniTask.WaitWhile(()=>WebCamTexture.width < 100);
            _showWebCamPhase = ShowWebCamPhase.waitingWebCamSetToPropBlock;
        }

        //#webcamTexが設定されていたら、それをpropBlockに適応させる処理
        void SetWebCamTexToPropBlock()
        {
            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
            propBlock.SetTexture("_BaseMap",WebCamTexture);
            propBlock.SetTexture("_EmissionMap",WebCamTexture);
            _meshRenderer.SetPropertyBlock(propBlock);
            _showWebCamPhase = ShowWebCamPhase.calculatingScale;
        }


    }
    //この関数は数学雑魚すぎてAIに投げた　人間失格です。
    void CaluculateScale()
    {
        // MeshFilterコンポーネントを取得
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("MeshFilterコンポーネントが見つかりません。");
            _showWebCamPhase = ShowWebCamPhase.Completed;
            return;
        }

        // オブジェクトの「元」の幅・高さを取得 (ローカル空間でのサイズ)
        float baseObjectWidth = meshFilter.mesh.bounds.size.x;
        float baseObjectHeight = meshFilter.mesh.bounds.size.y;

        // 幅か高さが0に近い場合は、計算不可能なので処理を中断
        if (Mathf.Approximately(baseObjectWidth, 0) || Mathf.Approximately(baseObjectHeight, 0))
        {
            Debug.LogError("オブジェクトの元サイズが0のため、スケール計算を実行できません。");
            _showWebCamPhase = ShowWebCamPhase.Completed;
            return;
        }
        
        // --- アスペクト比を考慮したスケーリング処理 ---

        
        // 1. カメラの視野サイズをワールド単位で取得
        float distance = Vector3.Distance(transform.position, _playerCamTr.position);
        float screenWorldHeight = 2.0f * distance * Mathf.Tan(_playerCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float screenWorldWidth = screenWorldHeight * _playerCam.aspect;
        
        // 2. アスペクト比を計算
        float webcamAspect = (float)WebCamTexture.width / (float)WebCamTexture.height;
        float screenAspect = _playerCam.aspect;

        // 3. quadの新しいスケールを計算
        float scaleX, scaleY;
        if (webcamAspect > screenAspect)
        {
            // Webカメラがスクリーンより「幅広」の場合 (高さをスクリーンに合わせる)
            scaleY = screenWorldHeight / baseObjectHeight;
            scaleX = (screenWorldHeight * webcamAspect) / baseObjectWidth;
        }
        else
        {
            // Webカメラがスクリーンより「縦長」の場合 (幅をスクリーンに合わせる)
            scaleX = screenWorldWidth / baseObjectWidth;
            scaleY = (screenWorldWidth / webcamAspect) / baseObjectHeight;
        }

        // 4. 取得した値から、オブジェクトのスケールを調整
        transform.localScale = new Vector3(scaleX, scaleY, 1f);

        _showWebCamPhase = ShowWebCamPhase.Completed;
    }
    public void ReCaluculateScale()
    {
        CaluculateScale();
    }
    //#自動で背面カメラを調べwebcamTexを設定する関数。失敗したらfalseを返す。
    public bool TrySetBackCamTexture()
    {
        int checkingCameraIndex = 0;
        foreach (WebCamDevice webCamDevice in WebCamTexture.devices)
        {
            if (webCamDevice.isFrontFacing == false)
            {
                SetWebCamTex(checkingCameraIndex);
                return true;
            }
            checkingCameraIndex++;
        }

        return false;

    }
    //#手動でwebCamTexの送り主となるカメラを設定する関数
    public void SetWebCamTex(int webCamIndexForSet)
    {
        WebCamTexture = new WebCamTexture(WebCamTexture.devices[webCamIndexForSet].name,Screen.width, Screen.height, 30);
        SelectedWebCamIndex = webCamIndexForSet;
        IsStopWebCam = false;
    }
    //#現在動いているwebCamTexを停止させる処理
    public void StopWebCamTex()
    {
        if(WebCamTexture == null){Debug.LogError("停止させる先となるWebCamTexがありません");return;}
        WebCamTexture.Stop();
        Destroy(WebCamTexture);
        IsStopWebCam = true;
    }

}
