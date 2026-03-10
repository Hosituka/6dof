using UnityEngine;

public class MotionSensorMover : MonoBehaviour
{
    [SerializeField]float _linearDamping = 0.05f;
    [SerializeField]float _sensitivity = 10f;
    Vector3 _linearAcceleration;
    Vector3 _linearVelocity;
    Transform _transform;
    void Start()
    {
        _transform = GetComponent<Transform>();
    }
    void Update()
    {

        //#メインの処理
        _linearAcceleration = MotionSensorUtility.LinearAcceleration;
        //##現実の10cmの移動を1m(unityの世界のグリッド一つ分)にするための処理、
        _linearVelocity += _linearAcceleration * Time.deltaTime * _sensitivity;
        _transform.position += _linearVelocity * Time.deltaTime;

        //#上の処理はセンサーの誤差によるドリフト現象を引き起こす為、それを緩和するために、速度を減衰させる処理
        _linearVelocity *= 1 - _linearDamping * Time.deltaTime;
        DebugUI_Manager.Current.UpdateLinearVelocity(_linearVelocity);
    }
    /*物理学的に正しい処理は下の通りだが、加速度センサーの値が小さすぎる上にセンサーの誤差の蓄積よりドリフト現象を引き起こす問題がある。
    _linearAcceleration = MotionSensorUtility.LinearAcceleration;
    _linearVelocity += _linearAcceleration * Time.deltaTime;
    _transform.position += _linearVelocity * Time.deltaTime;*/

}
