using UnityEngine;

public class PlayerShield : MonoBehaviour
{
  [SerializeField] private GameObject _shieldPrefab;
  [SerializeField] private float _shieldDuration = 5f;
  [SerializeField] private float _cooldownDuration = 10f;

  private enum SheildState { Ready, Active, Cooldown }
  private SheildState _state = SheildState.Ready;

  private float _timer;

  private GameObject _shieldInstance;

  public float sheildHeightOffset = 0.7f;

  private Animator sheildAnimator;


  private void Update()
  {
    HandleInput();
    RunTimer();

  }

  public void HandleInput()
  {
    if (Input.GetKeyDown(KeyCode.LeftShift) && _state == SheildState.Ready)
    {
      ActivateShield();
      sheildAnimator.SetTrigger("sheildActivate");
      AudioManager.Instance.StartShieldSound();
    }
  }

  private void ActivateShield()
  {
    Vector3 sheildPos = transform.position + transform.up * sheildHeightOffset;

    _shieldInstance = Instantiate(_shieldPrefab, sheildPos, Quaternion.identity, transform);

    sheildAnimator = _shieldInstance.GetComponent<Animator>();

    SetTimer(_shieldDuration);
    _state = SheildState.Active;

  }

  private void SetTimer(float duration)
  {
    _timer = duration;
  }

  private void RunTimer()
  {
    if (_state == SheildState.Ready) return;

    _timer -= Time.deltaTime;

    if (_timer < 0)
    {
      _timer = 0;

      if (_state == SheildState.Active)
      {
        DeactivateShield();
        AudioManager.Instance.StopShieldSound();
      }
      else if (_state == SheildState.Cooldown) _state = SheildState.Ready;
    }
  }

  private void DeactivateShield()
  {
    if (_shieldInstance != null)
    {
      sheildAnimator.SetTrigger("sheildDeactivate");
      Invoke("DestroySheild", 1f);
    }

    _state = SheildState.Cooldown;

    SetTimer(_cooldownDuration);
  }

  private void DestroySheild()
  {
    Destroy(_shieldInstance);
  }

}
