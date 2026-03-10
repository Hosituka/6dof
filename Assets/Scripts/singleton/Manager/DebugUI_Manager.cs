using UnityEngine;
using TMPro;

public class DebugUI_Manager : MonoBehaviour
{
    public static DebugUI_Manager Current;
    void Awake()
    {
        if (Current != null){Destroy(gameObject);
            return;
        }
        Current = this;
        DontDestroyOnLoad(gameObject);
    }

    [SerializeField]TextMeshProUGUI _linearAccelerationTMP;
    [SerializeField]TextMeshProUGUI _attitudeTMP;
    [SerializeField]TextMeshProUGUI _linearVelocityTMP;
    [SerializeField]bool _isDebugging;
    
    void Update()
    {
        if(_isDebugging == false) return;
        _linearAccelerationTMP.text = "linearAcceleration:<br>" + MotionSensorUtility.LinearAcceleration;
        _attitudeTMP.text = "attitude:<br>" + MotionSensorUtility.CurrentAttitudeValue.eulerAngles;
    }
    public void UpdateLinearVelocity(Vector3 linearVelocity)
    {
        _linearVelocityTMP.text = "LinearVelocity:<br>" + linearVelocity;
    }
}
