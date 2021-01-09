using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This script requires you to have setup your animator with 3 parameters, "InputMagnitude", "InputX", "InputZ"
//With a blend tree to control the inputmagnitude and allow blending between animations.
[RequireComponent(typeof(CharacterController))]
public class MovementInput : MonoBehaviour {

    public float Velocity;
    [Space]

	public float InputX;
	public float InputZ;
	public Vector3 desiredMoveDirection;
	public bool blockRotationPlayer;
	public float desiredRotationSpeed = 0.1f;
	public Animator anim;
	public float Speed;
	public float allowPlayerRotation = 0.1f;
	private Camera cam;
	public CharacterController controller;
	public bool isGrounded;
	BuffsManager buffsManager;

    [Header("Animation Smoothing")]
    [Range(0, 1f)]
    public float HorizontalAnimSmoothTime = 0.2f;
    [Range(0, 1f)]
    public float VerticalAnimTime = 0.2f;
    [Range(0,1f)]
    public float StartAnimTime = 0.3f;
    [Range(0, 1f)]
    public float StopAnimTime = 0.15f;

    public float verticalVel;
    private Vector3 moveVector;

	[SerializeField]
	GameObject Bomb;

	public float bombCooldown = 3.0f;
	float last_boom_throw = 0.0f;

	// Use this for initialization
	void Start () {
		anim = this.GetComponent<Animator> ();
		cam = Camera.main;
		controller = this.GetComponent<CharacterController> ();
		buffsManager = GetComponent<BuffsManager>();
	}
	
	// Update is called once per frame
	void Update () {

        if (!GetComponent<PhotonView>().IsMine)
            return;

        InputMagnitude ();

        isGrounded = controller.isGrounded;
        if (isGrounded)
        {
            verticalVel -= 0;
        }
        else
        {
            verticalVel -= 1;
        }
        moveVector = new Vector3(0, verticalVel * .2f * Time.deltaTime, 0);
		controller.Move(moveVector);
		//GetComponent<PhotonView>().RPC("RPC_Movment", RpcTarget.All, moveVector);



		if (Input.GetKeyDown(KeyCode.Escape))
		{
			GameObject.Find("RoomController").GetComponent<RoomController>().GoToMenu();
		}

		if (Input.GetKeyDown(KeyCode.Space) && (Time.time - last_boom_throw) >= bombCooldown) 
		{
			GetComponent<PhotonView>().RPC("RPC_SpawnBomb", RpcTarget.All, transform.position);
			last_boom_throw = Time.time;
		}
	}

	[PunRPC]
	void RPC_Movment(Vector3 movement)
    {
		controller.Move(movement);
	}

	[PunRPC]
	void RPC_Rotate(Quaternion rotate)
	{
		transform.rotation = rotate;
	}

	[PunRPC]
	void RPC_SpawnBomb(Vector3 pos)
	{
		if (pos.x - Mathf.Abs(Mathf.Floor(pos.x) + 0.5f) < Mathf.Abs(Mathf.Ceil(pos.x) + 0.5f) - pos.x)
		{
			pos.x = Mathf.Floor(pos.x) + 0.5f;
		}
		else
		{
			pos.x = Mathf.Ceil(pos.x) + 0.5f;
		}

		if (pos.z - Mathf.Abs(Mathf.Floor(pos.z) + 0.5f) < Mathf.Abs(Mathf.Ceil(pos.z) + 0.5f) - pos.z)
		{
			pos.z = Mathf.Floor(pos.z) + 0.5f;
		}
		else
		{
			pos.z = Mathf.Ceil(pos.z) + 0.5f;
		}

		pos.y = 0.8f;
		gameObject.layer = 8;
		GameObject bomb = Instantiate(Bomb, pos, Bomb.transform.rotation);
		Bomb b = bomb.GetComponent<Bomb>();
		b.color = GetComponent<CharacterSkinController>().childColor;
		b.bomb_color = GetComponent<CharacterSkinController>().color;
		if (buffsManager.isBigBomb)
			b.tiles_to_paint = 4;
	}

	void PlayerMoveAndRotation() {
		InputX = Input.GetAxis ("Horizontal");
		InputZ = Input.GetAxis ("Vertical");

		var camera = Camera.main;
		var forward = camera.transform.forward;
		var right = camera.transform.right;

		forward.y = 0f;
		right.y = 0f;

		forward.Normalize ();
		right.Normalize ();

		desiredMoveDirection = forward * InputZ + right * InputX;

		if (blockRotationPlayer == false) {
			Quaternion q = Quaternion.Slerp (transform.rotation, Quaternion.LookRotation (desiredMoveDirection), desiredRotationSpeed);
			//GetComponent<PhotonView>().RPC("RPC_Rotate", RpcTarget.All, q);
			transform.rotation = q;
			Vector3 m = desiredMoveDirection * Time.deltaTime * Velocity;
			//GetComponent<PhotonView>().RPC("RPC_Movment", RpcTarget.All, m);
			controller.Move(m);
		}
	}

    public void LookAt(Vector3 pos)
    {
		Quaternion q = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(pos), desiredRotationSpeed);
		GetComponent<PhotonView>().RPC("RPC_Rotate", RpcTarget.All, q);

	}

	public void RotateToCamera(Transform t)
    {

        var camera = Camera.main;
        var forward = cam.transform.forward;
        var right = cam.transform.right;

        desiredMoveDirection = forward;

        t.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredMoveDirection), desiredRotationSpeed);
    }

	void InputMagnitude() {
		//Calculate Input Vectors
		InputX = Input.GetAxis ("Horizontal");
		InputZ = Input.GetAxis ("Vertical");

		//anim.SetFloat ("InputZ", InputZ, VerticalAnimTime, Time.deltaTime * 2f);
		//anim.SetFloat ("InputX", InputX, HorizontalAnimSmoothTime, Time.deltaTime * 2f);

		//Calculate the Input Magnitude
		Speed = new Vector2(InputX, InputZ).sqrMagnitude;

        //Physically move player

		if (Speed > allowPlayerRotation) {
			anim.SetFloat ("Blend", Speed, StartAnimTime, Time.deltaTime);
			PlayerMoveAndRotation ();
		} else if (Speed < allowPlayerRotation) {
			anim.SetFloat ("Blend", Speed, StopAnimTime, Time.deltaTime);
		}
	}
}
