using UnityEngine;

public class PlayerShield : MonoBehaviour
{
    [SerializeField] private GameObject _shieldPrefab;
    [SerializeField] private float _shieldTime = 5f;
    [SerializeField] private bool _isShieldActive;
    [SerializeField] private float _sheildRecoveryTime = 5f;
    private float _shieldTimer;
    private float _sheildRecoveryTimer;

    private GameObject _shield;


    [SerializeField] private Transform playerTranform;


    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            if (!_isShieldActive && _shieldTimer <= 0)
            {
                ActivateShield();
                _shieldTimer = _shieldTime;
                _isShieldActive = true;
            }
        }

        _shieldTimer -= Time.deltaTime;
       

        CheckTimerAndDeactivateShield();

    }

    private void ActivateShield()
    {
        Vector3 targetSheildPos = playerTranform.position + 3f * Vector3.forward;

        _shield = Instantiate(_shieldPrefab, targetSheildPos, Quaternion.identity , playerTranform);

    }

    private void CheckTimerAndDeactivateShield()
    {
        if (_isShieldActive)
        {
            if(_shieldTimer <= 0)
            {
                _isShieldActive = false;
                _shieldTimer = 0;
                Destroy(_shield);
                StartSheildRecovery();
            }
        }
    }

    private void StartSheildRecovery()
    {

    }

}
