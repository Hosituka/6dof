using UnityEngine;



public class MotionSensorRotator : MonoBehaviour
{
    Quaternion _initialAttitudeValueOffset;
    Quaternion _currentAttitudeValue;

    // Update is called once per frame
    void Update()
    {
        _initialAttitudeValueOffset = MotionSensorUtility.InitialAttitudeValueOffset;
        _currentAttitudeValue = MotionSensorUtility.CurrentAttitudeValue;

        transform.rotation =  _initialAttitudeValueOffset * _currentAttitudeValue ;
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 0);
    }
}
