using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    private Collider2D _collider2D;
    private Rigidbody2D _rb2D;
    private FrameInput _input;
    private float _currentHorizontalSpeed;
    private float _currentVerticalSpeed;
    private float _apexPoint;

    private void Awake() {
        _collider2D = GetComponent<Collider2D>();
        _rb2D = GetComponent<Rigidbody2D>();
        _gravityScale = _rb2D.gravityScale;
        _direction = transform.right;
    }
    void Update() {
        CheckInput();
        //CheckCollisions();
        
        //Timer();

        //CalculateApexPoint();
        CalculateDirection();
        //CalculateDash();
        //CalculateWallJump();
        // if(_isDashing)
        //     return;
        CalculateMove();
        // if(_isWallJumping)
        //     return;
        //CalculateJump();
    }
    

    private void CheckInput()
    {
        _input.x = Input.GetAxisRaw("Horizontal");
        _input.jumpPressed = Input.GetButtonDown("Jump");
        _input.jumpReleased = Input.GetButtonUp("Jump");
        _input.dashPressed = Input.GetButtonDown("Dash");
    }

    #region Collisions
    [Header("Collisions")]
    [SerializeField] private float _boxCastDist = 0.1f;
    [SerializeField] private LayerMask _groundLayerMask;
    private void CheckCollisions()
    {
        if(IsGrounded() && _rb2D.velocity.y <= 0)
        {
            _isGrounded = true;
        }
        else
        {
            _isGrounded = false;
        }
        
        _isWalled = IsWallCollision();
    }
    [SerializeField] [Range(0,1)]public float _boxSize = 1;
    private bool IsGrounded()
    {
        var boxSize = _collider2D.bounds.size * _boxSize;
        RaycastHit2D raycastHit2D = Physics2D.BoxCast(_collider2D.bounds.center,boxSize,0,Vector2.down,_boxCastDist,_groundLayerMask);
        Debug.DrawRay(_collider2D.bounds.center,_collider2D.bounds.extents* Vector2.down,Color.blue);
        return raycastHit2D.collider != null;
    }
    [SerializeField] private float _wallCastDist = 0.5f;
    private bool IsWallCollision()
    {
        RaycastHit2D raycastHit2D = Physics2D.Raycast(_collider2D.bounds.center,_direction,_wallCastDist,_groundLayerMask);
        return raycastHit2D.collider != null;
    }
    #endregion

    #region Timer
    [Header("Timer")]
    [SerializeField] private float _lastGroundedTime;
    [SerializeField] private float _lastJumpTime;
    [SerializeField] private float _lastDashTime;
    private void Timer()
    {
        if(_input.jumpPressed)
        {
            _lastJumpTime = _jumpBufferTime; 
        }
        else
        {
            _lastJumpTime -= Time.deltaTime;
        }
        if(_isGrounded)
        {
            _lastGroundedTime = _CoyoteTime;
        }
        else
        {
            _lastGroundedTime -= Time.deltaTime;
        }
        if(_input.dashPressed)
        {
            _lastDashTime = _dashBufferTime;
        }
        else
        {
            _lastDashTime -= Time.deltaTime;
        }
    }
    #endregion

    #region Move
    [Header("Move")]
    [SerializeField] private float _characterSpeed = 5f;
    [SerializeField] private float _acceleration = 50f;
    [SerializeField] private float _apexBonusMultiplier = 2f;
    [SerializeField] private float _deacceleration = 100f;
    private void CalculateMove()
    {   
        if(_isDashing || _isWallJumping) return;
        float targetSpeed = _input.x * _characterSpeed;
        float speedDifference = targetSpeed - _rb2D.velocity.x;
        float acceleration = Mathf.Abs(_input.x) > 0.1f ? _acceleration : _deacceleration;
        _currentHorizontalSpeed = speedDifference * acceleration;
        
        float apex = _input.x * _apexBonusMultiplier * _apexPoint;
        _currentHorizontalSpeed += apex;
        
        _rb2D.AddForce(new Vector2(_currentHorizontalSpeed,0));
        Debug.Log(_currentHorizontalSpeed);
    }
    #endregion

    #region  Direction
    private void CalculateDirection()
    {
        if(_input.x != 0)
            {
                _direction.x = _input.x;
            }
        Debug.DrawRay(_collider2D.bounds.center,_direction,Color.red);
    }
    #endregion

    



    // private void CalculateSlide()
    // {

    // }

    
    #region Jump
    [Header("Jump")]
    [SerializeField] private float _jumpForce = 10;
    [SerializeField] private bool _isGrounded;
    [SerializeField] private float _jumpApexTreshold = 2f;
    [SerializeField] private float _minFallSpeed = 1f;
    [SerializeField] private float _maxFallSpeed = 100f;
    [SerializeField] private float _fallSpeed;
    [SerializeField] private bool _isSecondJump = true;
    [SerializeField] private float _CoyoteTime = 0.20f;
    [SerializeField] private float _jumpBufferTime = 0f;

    private void CalculateJump()
    {
        if(_lastJumpTime > 0f && _lastGroundedTime > 0f )
        {
            Debug.Log("Jump");
            Jump();          
            _lastJumpTime = 0f;
            _lastGroundedTime = 0f;
            _isSecondJump = true;
        }

        // if(_isWalled && !_isGrounded && _lastJumpTime>0)
        // {
        //     _lastJumpTime = 0;
        //     _isSecondJump = true;
        //     StartCoroutine(WallJump());
        // }

        if(_input.jumpReleased)
        {
            Debug.Log("JumpCut");
            JumpCut();
            _lastJumpTime = 0f;
            _lastGroundedTime = 0;
            _isGrounded = false;
            _input.jumpReleased = false;
            
        }
        
        if(_lastJumpTime > 0f && !_isGrounded && !_isWallJumping && _isSecondJump)
        {
            Debug.Log("Second Jump");
            Jump();
            _isSecondJump = false;
            _lastJumpTime = 0f;
        }
    }
    private void Jump()
    {
        _rb2D.velocity = Vector2.zero;
        _currentVerticalSpeed = _jumpForce;// + -_rb2D.velocity.y;
        _rb2D.AddForce(new Vector2(0,_currentVerticalSpeed),ForceMode2D.Impulse);
    }
    private void CalculateApexPoint()
    {
        if(!_isGrounded)
        {   
            _apexPoint = Mathf.InverseLerp(_jumpApexTreshold,0,Mathf.Abs(_rb2D.velocity.y));
            _fallSpeed = Mathf.Lerp(_minFallSpeed,_maxFallSpeed,_apexPoint);
        }
        else
        {
            _apexPoint = 0;
        }
    }
    [Header("JumpCut")]
    [Range(0,1)][SerializeField] private float _jumpCutMultiplier = 0.5f;
    private void JumpCut()
    {
        if(_rb2D.velocity.y > 0 && !_isGrounded)
        {
            _rb2D.AddForce(Vector2.down * _rb2D.velocity.y * (1-_jumpCutMultiplier),ForceMode2D.Impulse);
        }
    }
    #endregion
    
    #region Gravity
    [Header("Gravity")]
    [SerializeField] private float _gravityScale;
    [SerializeField] private float _fallGravityMultiplier = 2f;
    private void FallGravity()
    {
        if(_rb2D.velocity.y < 0)
        {
            _rb2D.gravityScale = _gravityScale * _fallSpeed;
        }
        else
        {
            _rb2D.gravityScale = _gravityScale;
        }
    }
    #endregion

    #region  Dash
    [Header("Dash")]
    [SerializeField] private float _dashForce;
    [SerializeField] private Vector2 _direction;
    [SerializeField] private bool _isDashing;
    [SerializeField] private float _dashStartTime;
    [SerializeField] private float _dashEndTime;
    [SerializeField] private float _dashDurationTime;
    [SerializeField] private float _dashBufferTime;
    [SerializeField] private float _dashAmount = 1;
    [SerializeField] private float _dashNumberLeft = 1;
    private void CalculateDash()
    {
        if(_lastDashTime > 0f && !_isDashing && _dashNumberLeft > 0)
        {  
            StartCoroutine(StartDash(_direction));
        }

        if(_dashAmount > _dashNumberLeft && !_isDashRefilling && !_isDashing)
        {
            StartCoroutine(RefillDash());
        }
        
    }
	private IEnumerator StartDash(Vector2 dir)
	{
		_isDashing = true;
        _dashStartTime = Time.time;
        _dashEndTime = _dashStartTime + _dashDurationTime;
        
		_lastGroundedTime = 0;
		_lastDashTime = 0;

		float startTime = Time.time;

		_dashNumberLeft--;
		//_isDashAttacking = true;

		_rb2D.gravityScale = 0;
        _rb2D.velocity = Vector2.zero;

		//We keep the player's velocity at the dash speed during the "attack" phase (in celeste the first 0.15s)
		while (Time.time <= _dashEndTime)
		{
			//_rb2D.velocity = dir.normalized * _dashForce;
			var desiredSpeed = _dashForce - Mathf.Abs(_rb2D.velocity.x);
            _rb2D.AddForce(dir.normalized * desiredSpeed,ForceMode2D.Force);
            yield return null;
		}

		//startTime = Time.time;

		//_isDashAttacking = false;

		//Begins the "end" of our dash where we return some control to the player but still limit run acceleration (see Update() and Run())
		_rb2D.gravityScale = _gravityScale;
        _rb2D.velocity = Vector2.zero;
		//RB.velocity = Data.dashEndSpeed * dir.normalized;

		// while (Time.time - startTime <= Data.dashEndTime)
		// {
		// 	yield return null;
		// }
		_isDashing = false;
	}
    [SerializeField] private bool _isDashRefilling = false;
    [SerializeField] private float _dashRefillTime = 0.15f;
	//Short period before the player is able to dash again
	private IEnumerator RefillDash()
	{
		//SHoet cooldown, so we can't constantly dash along the ground, again this is the implementation in Celeste, feel free to change it up
		_isDashRefilling = true;
		yield return new WaitForSeconds(_dashRefillTime);
		_isDashRefilling = false;
		_dashNumberLeft = Mathf.Min(_dashAmount, _dashNumberLeft + 1);
	}
	#endregion

    #region Slide
    [Header("Slide")]
    [SerializeField] private float _slideSpeed = 2f;
    [SerializeField] private float _slideAcceleration = 1f;
    private void Slide()
    {
        float speedDifference = _slideSpeed - _rb2D.velocity.y;
        float movement = speedDifference * _slideAcceleration;
        movement = Mathf.Clamp(movement, -Mathf.Abs(speedDifference)* (1/Time.fixedDeltaTime),Mathf.Abs(speedDifference)* (1/Time.fixedDeltaTime));
        _rb2D.AddForce(movement* Vector2.up);
    }
    #endregion

    #region WallJump
    [Header("Wall Jump")]
    [SerializeField] private float _wallJumpForce;
    [SerializeField] private Vector2 _wallJumpDirection;
    [SerializeField] private float _lastWallJumpTime;
    [SerializeField] private float _wallJumpBufferTime = 0.05f;
    [SerializeField] private bool _isWallJumping = false;
    [SerializeField] private bool _isWalled = false;
    
    private void CalculateWallJump()
    {
        if(_isWalled && !_isGrounded && _lastJumpTime>0)
        {
            Debug.Log("wall jump");
            _lastJumpTime = 0;
            _isSecondJump = true;
            _isWallJumping = true;
        //Vector2 dir = new Vector2(-_direction.x ,_wallJumpDirection.y).normalized;
            _lastWallJumpTime = Time.time + _wallJumpBufferTime;
            _direction = -_direction;
        //_rb2D.AddForce(new Vector2(0,1) * _jumpForce,ForceMode2D.Impulse);
            _wallJumpDirection.x = _direction.x;
        // while(_lastWallJumpTime > Time.time)
        // {
            //_rb2D.AddForce(new Vector2(_direction.x,0)*_characterSpeed,ForceMode2D.Force);
            _rb2D.AddForce(_wallJumpDirection.normalized*_wallJumpForce,ForceMode2D.Impulse);
        // }
            
            
        }
        if(_input.jumpReleased && _lastWallJumpTime < Time.time)
        {
            _isWallJumping = false;
            //_rb2D.velocity = Vector2.zero;
        }
    }

    
    // private void WallJump()
    // {
    //     if(_lastJumpTime > 0f && _isWallJumping && !_isGrounded)
    //     {
    //         Debug.Log("Jump Cut");
    //         _isWallJumping = false;
    //         _lastJumpTime = 0f;
    //         _rb2D.velocity = Vector2.zero;
    //         Vector2 force = new Vector2(-_direction.x,1).normalized * _wallJumpForce;
    //         _rb2D.AddForce(force, ForceMode2D.Impulse);  
            
    //     }
    // }
    private IEnumerator WallJump()
    {
        Debug.Log("Wall Jump");
        _lastJumpTime = 0;
        _isSecondJump = true;
        _isWallJumping = true;
        _rb2D.gravityScale = 0;
        //Vector2 dir = new Vector2(-_direction.x ,_wallJumpDirection.y).normalized;
        _lastWallJumpTime = Time.time + _wallJumpBufferTime;
        _direction = -_direction;
        //_rb2D.AddForce(new Vector2(0,1) * _jumpForce,ForceMode2D.Impulse);
        _wallJumpDirection.x = _direction.x;
        while(_lastWallJumpTime > Time.time)
        {
            if(_input.jumpReleased) yield break;
            //_rb2D.AddForce(new Vector2(_direction.x,0)*_characterSpeed,ForceMode2D.Force);
            _rb2D.AddForce(_wallJumpDirection.normalized*_wallJumpForce,ForceMode2D.Force);
            yield return null;
        }
        _rb2D.velocity = Vector2.zero;
        _rb2D.gravityScale = _gravityScale;
        _isWallJumping = false;
        
    }
    #endregion
}

public struct FrameInput
{
    public float x;
    public bool jumpPressed;
    public bool jumpReleased;
    public bool dashPressed;
}