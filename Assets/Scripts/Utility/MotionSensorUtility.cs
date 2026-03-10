using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
//姿勢センサーや線形加速度センサーの開始や、そのセンサーから帰ってくる値をunity用に加工する責務を持つクラス。
public static class MotionSensorUtility
{
    public static Quaternion InitialAttitudeValueOffset = Quaternion.identity;
    public static Quaternion CurrentAttitudeValue = Quaternion.identity;
    public static Vector3 LinearAcceleration;
    static AttitudeSensor _attitudeSensor;
    static LinearAccelerationSensor _linearAccelerationSensor;
    public static async UniTask<MotionSensorPhase> StartMotionSensors()
    {
        if(AttitudeSensor.current == null)
        {return MotionSensorPhase.NotFoundAttitudeSensor;}
        if(LinearAccelerationSensor.current == null)
        {return MotionSensorPhase.NotFoundLinearAccelerationSensor;}

        _attitudeSensor = AttitudeSensor.current;
        _linearAccelerationSensor = LinearAccelerationSensor.current;
        InputSystem.EnableDevice(_attitudeSensor);
        InputSystem.EnableDevice(_linearAccelerationSensor);
        //#姿勢センサーが有効化前の値を返している間待機、つまり有効化されるまで待機
        //##各種センサーの値更新のループを開始
        UpdateMotionSensor().Forget();
        await UniTask.WaitWhile(()=>CurrentAttitudeValue == Quaternion.identity);
        return MotionSensorPhase.CompletedMotionSensors;
    }
    static async UniTaskVoid UpdateMotionSensor()
    {
        while (true)
        {
            //#姿勢センサーの更新
            CurrentAttitudeValue = ConvertQuaternionForUnity(_attitudeSensor.attitude.ReadValue());
            CurrentAttitudeValue = Quaternion.Euler(CurrentAttitudeValue.eulerAngles.x, CurrentAttitudeValue.eulerAngles.y, 0);
            //#線形加速度センサー
            LinearAcceleration = _linearAccelerationSensor.acceleration.ReadValue();
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
    }
    public static void ResetRotation()
    {
        if(_attitudeSensor == null){Debug.LogError("対象となる姿勢センサーが見つかりません"); return;}

        InitialAttitudeValueOffset = ConvertQuaternionForUnity(_attitudeSensor.attitude.ReadValue());
        InitialAttitudeValueOffset = Quaternion.Euler(0, InitialAttitudeValueOffset.eulerAngles.y, 0);
        InitialAttitudeValueOffset = Quaternion.Inverse(InitialAttitudeValueOffset);
    }
    static Quaternion ConvertQuaternionForUnity(Quaternion quaternion)
    {
        return new Quaternion(-quaternion.x, -quaternion.z, -quaternion.y, quaternion.w) * Quaternion.Euler(90f, 0f, 0f);
    }

}
public enum MotionSensorPhase
{

    NotFoundAttitudeSensor,
    NotFoundLinearAccelerationSensor,
    CompletedMotionSensors,
}
