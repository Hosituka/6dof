using System.Collections;
using UnityEngine;

public class AR_BackGround : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]Camera _playerCam;
    [SerializeField]phase _currentPhase;
    
    enum phase{
        idle,
        //webカメラのテクスチャが開始された後、セットされるのを待機する状態
        waitingWebCamSet,
        //webカメラのテクスチャが開始されて、実際に解像度などが決まるのを待機する状態
        waitingWebCamResolution,
        //スクリーンとwebCamTexのアスペクト比較、それによるスケール計算
        calculatingScale,
        //transformのscaleに反映
        Completed,
    }
    MeshRenderer _meshRenderer;
    float _startDistance;

    void Start()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        //プレイヤーのカメラからの距離を取得
        _startDistance = Vector3.Distance(_playerCam.transform.position,transform.position);
    }
    void LateUpdate()
    {
        transform.position = _playerCam.transform.forward * _startDistance;
        transform.rotation = Quaternion.LookRotation(_playerCam.transform.forward);
    }
    // Update is called once per frame
    public void StartShowWebCam()
    {
        _currentPhase = phase.waitingWebCamSet;
        StartCoroutine(main());
        IEnumerator main()
        {
            StartCoroutine(SetWebCamTex());
            yield return new WaitWhile(()=> _currentPhase == phase.waitingWebCamSet);
            StartCoroutine(WaitWebCamResolution());
            yield return new WaitWhile(()=> _currentPhase == phase.waitingWebCamResolution);
            CaluculateScale();
            yield return new WaitWhile(()=> _currentPhase == phase.calculatingScale);
        }


    }
    IEnumerator SetWebCamTex()
    {
        
        if(GameManager.Current.WebCamTexture == null)
        {
            Debug.LogError("webCamTextureの参照渡し元が存在しません");
            _currentPhase = phase.waitingWebCamResolution;
            yield break;
        }
        yield return new WaitWhile(()=>GameManager.Current.WebCamTexture.isPlaying == false);
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        propBlock.SetTexture("_BaseMap",GameManager.Current.WebCamTexture);
        propBlock.SetTexture("_EmissionMap",GameManager.Current.WebCamTexture);
        _meshRenderer.SetPropertyBlock(propBlock);
        _currentPhase = phase.waitingWebCamResolution;
    }
    IEnumerator WaitWebCamResolution()
    {
        yield return new WaitWhile(()=>GameManager.Current.WebCamTexture.width < 100);
        _currentPhase = phase.calculatingScale;
    }
    //この関数は数学雑魚すぎてAIに投げた　人間失格です。
    void CaluculateScale()
    {
        // MeshFilterコンポーネントを取得
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("MeshFilterコンポーネントが見つかりません。");
            _currentPhase = phase.Completed;
            return;
        }

        // オブジェクトの「元」の幅・高さを取得 (ローカル空間でのサイズ)
        float baseObjectWidth = meshFilter.mesh.bounds.size.x;
        float baseObjectHeight = meshFilter.mesh.bounds.size.y;

        // 幅か高さが0に近い場合は、計算不可能なので処理を中断
        if (Mathf.Approximately(baseObjectWidth, 0) || Mathf.Approximately(baseObjectHeight, 0))
        {
            Debug.LogError("オブジェクトの元サイズが0のため、スケール計算を実行できません。");
            _currentPhase = phase.Completed;
            return;
        }
        
        // --- アスペクト比を考慮したスケーリング処理 ---

        // 1. WebCamTextureを取得
        WebCamTexture webCamTexture = GameManager.Current.WebCamTexture;
        
        // 2. カメラの視野サイズをワールド単位で取得
        float distance = Vector3.Distance(transform.position, _playerCam.transform.position);
        float screenWorldHeight = 2.0f * distance * Mathf.Tan(_playerCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float screenWorldWidth = screenWorldHeight * _playerCam.aspect;
        
        // 3. アスペクト比を計算
        float webcamAspect = (float)webCamTexture.width / (float)webCamTexture.height;
        float screenAspect = _playerCam.aspect;

        // 4. quadの新しいスケールを計算
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

        // 5. 取得した値から、オブジェクトのスケールを調整
        transform.localScale = new Vector3(scaleX, scaleY, 1f);

        _currentPhase = phase.Completed;
    }
    public void ReCaluculateScale()
    {
        CaluculateScale();
    }
}
