using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerControl : MonoBehaviour {
	private static CardboardControl cardboard;
	public Camera cam;
	public float groundCheckDistance = 0.01f; // distance for checking if the controller is grounded ( 0.01f seems to work
	public float stickToGroundHelperDistance = 0.5f;
	private Vector3 groundContactNormal;
	public float CurrentTargetSpeed = 0f;
	private Rigidbody playerBody;
	private CapsuleCollider playerCollider;

	private GameObject backWall;
	private GameObject sphere;
	private AudioSource sphereSound;
	private Color desiredColor;


	// Use this for initialization
	void Start () {
		playerBody = GetComponent<Rigidbody> ();
		playerCollider = GetComponent<CapsuleCollider> ();
		cardboard = GameObject.Find("CardboardControlManager").GetComponent<CardboardControl>();

		cardboard.gaze.OnChange += CardboardFocusChanges; 
		backWall = GameObject.Find ("backwall");
		sphere = GameObject.Find ("Sphere");
		sphereSound = sphere.GetComponent<AudioSource> ();
		desiredColor = new Color (0f, 0f, 0f);
	}

	void FixedUpdate() {
		Vector2 input = GetInput ();
		GroundCheck ();
		if ((Mathf.Abs (input.x) > float.Epsilon || Mathf.Abs (input.y) > float.Epsilon)) {
			//Debug.Log("got input "+input);
			// always move along the camera forward as it is the direction that it being aimed at
			Vector3 desiredMove = cam.transform.forward * input.y + cam.transform.right * input.x;
			desiredMove = Vector3.ProjectOnPlane (desiredMove, groundContactNormal).normalized;
			
			desiredMove.x = desiredMove.x * CurrentTargetSpeed;
			desiredMove.z = desiredMove.z * CurrentTargetSpeed;
			desiredMove.y = desiredMove.y * CurrentTargetSpeed;
			if (playerBody.velocity.sqrMagnitude <
			    (CurrentTargetSpeed * CurrentTargetSpeed)) {
				playerBody.AddForce (desiredMove, ForceMode.Impulse);
			}
			StickToGroundHelper();
		}
	}

	void Update(){
		if (backWall.GetComponent<Renderer> ().material.color != desiredColor) {
			backWall.GetComponent<Renderer> ().material.color = Color.Lerp(backWall.GetComponent<Renderer> ().material.color, desiredColor, Time.deltaTime);
			DynamicGI.SetEmissive (backWall.GetComponent<Renderer>(), backWall.GetComponent<Renderer> ().material.color * 1.5f);
		}

		if (cardboard.gaze.IsHeld () && cardboard.gaze.Object () == sphere) {
			sphere.transform.localScale = Vector3.Lerp (sphere.transform.localScale, new Vector3 (4, 4, 4), Time.deltaTime);
			sphere.GetComponent<ParticleSystem>().startColor = desiredColor;
			sphere.GetComponent<ParticleSystem>().enableEmission = true;
			sphereSound.volume = Mathf.Lerp (sphereSound.volume, 1f, Time.deltaTime);
			if(!sphereSound.isPlaying){
				sphereSound.time = 0;
				sphereSound.Play();
			}
		} else {
			sphere.transform.localScale = Vector3.Lerp (sphere.transform.localScale, new Vector3(2,2,2), Time.deltaTime);
			sphere.GetComponent<ParticleSystem>().enableEmission = false;
			sphereSound.volume = Mathf.Lerp (sphereSound.volume, 0f, Time.deltaTime);
		}
	}

	private void StickToGroundHelper ()
	{
		RaycastHit hitInfo;
		if (Physics.SphereCast (transform.position, playerCollider.radius, Vector3.down, out hitInfo,
		                        ((playerCollider.height / 2f) - playerCollider.radius) +
		                        stickToGroundHelperDistance)) {
			if (Mathf.Abs (Vector3.Angle (hitInfo.normal, Vector3.up)) < 85f) {
				playerBody.velocity = Vector3.ProjectOnPlane (playerBody.velocity, hitInfo.normal);
			}
		}
	}

	private void GroundCheck()
	{
		RaycastHit hitInfo;
		if (Physics.SphereCast(transform.position, playerCollider.radius, Vector3.down, out hitInfo,
		                       ((playerCollider.height/2f) - playerCollider.radius) + groundCheckDistance))
		{
			groundContactNormal = hitInfo.normal;
		}
		else
		{
			groundContactNormal = Vector3.up;
		}
	}

	private Vector2 GetInput ()
	{
		float x,y;
		if (Input.GetMouseButton(0)){
			x=0;
			y = 1;
		} else {
			x = Input.GetAxis("Horizontal");
			y = Input.GetAxis("Vertical");
		}
		Vector2 input = new Vector2(x,y);
		return input;
	}

	private void CardboardFocusChanges(object sender){
		CardboardControlGaze gaze = sender as CardboardControlGaze;
		if (gaze.IsHeld() && gaze.Object() == sphere) {
			float redOrBlue = Random.value;
			desiredColor= new Color(redOrBlue, Random.value, 1 - redOrBlue);
			cardboard.pointer.Show();
			cardboard.pointer.ClearHighlight();
		}
	}
}
