using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum WalkingState
{
	FORWARD, BACKWARD, IDLE
}; 

public class PhysicsWalkingController : MonoBehaviour
{

	/**TODO:
		 * SWING ARMS WHILE WALKING BACKWARDS
		 * RE-IMPLEMENT IDLE FUNCTION (IN IDLE STATE) - INCLUDING BALANCING LEFT AND RIGHT
		 * SMOOTH TILTING AND ROTATION
		 * FIGURE OUT WHY ARMS ARENT ROTATING PROPERLY
		 * ADJUST SETRELATIVEROTATION FUNCTION TO SET ROTATIONS RELATIVE TO LOCAL TRANSFORM
		 * ADD FORCE TO TORSO TO HELP BALANCING 
		 * */ 

	/**CLASS VARIABLES
	 * ---------------------------------------------------------------------------------------------------------------**/

	[SerializeField] KinematicBone hips;
	[SerializeField] KinematicBone rightFoot;
	[SerializeField] KinematicBone leftFoot;
	[SerializeField] KinematicBone rightHand;
	[SerializeField] KinematicBone leftHand; 
	
	[SerializeField] BoxCollider rightFootCollider;
	[SerializeField] BoxCollider leftFootCollider;

	[SerializeField] Transform centerOfMass;
	[SerializeField] Transform rightFootTarget;
	[SerializeField] Transform leftFootTarget;
	[SerializeField] Transform rightHandTarget;
	[SerializeField] Transform leftHandTarget; 
	Transform forwardFootTarget;
	Transform backFootTarget;
	Transform oppositeHandTarget; 

	[SerializeField] GameObject body;
	Vector3 hipRotation;
	Vector3 currentPosition; 

	float velocityForwardDirection;
	float velocityRightDirection;
	float legOffset; 
	[SerializeField] float walkingSpeed = 1.4f;
	[SerializeField] float forceStrength; 


	bool keyDown;

	WalkingState walkingState; 

	/**UNITY FUNCTIONS
	 * -----------------------------------------------------------------------------------------------------------------**/

	// Start is called before the first frame update
    void Start()
    {
		keyDown = false;
		velocityForwardDirection = 0;
		hipRotation = hips.transform.rotation.eulerAngles;
		currentPosition = body.transform.position; 
		walkingState = WalkingState.IDLE;
		legOffset = Vector3.Distance(rightFoot.transform.position, leftFoot.transform.position) / 2; 
	}

    // Update is called once per frame
    void Update()
    {
		getKeyboardInputs();
		moveBody(); 
		adjustIKTargets();
		keepBodyUpright(); 
	}

	/**KINEMATIC FUNCTIONS
	 * ---------------------------------------------------------------------------------------------------------------**/ 

	/**This function is to capture the input from the keyboard. When the forward keys are pressed, the body should
	 * tilt forward and begin moving in it's forward direction. If the back key is pressed the body should tilt back. 
	 * If the right / left keys are pressed, the body should rotate to the right or left. **/
	void getKeyboardInputs()
	{
		if (keyDown
			&& (Input.GetAxisRaw("Vertical") == 0)
			&& (Input.GetAxisRaw("Horizontal") == 0))
		{
			keyDown = false;
			velocityForwardDirection = 0;
			setHipRotation(0, hipRotation.y, hipRotation.z);
			walkingState = WalkingState.IDLE;
			currentPosition = body.transform.position; 
		}

		else if (!keyDown
					&& (Input.GetAxisRaw("Vertical") != 0)) /**Adjust rotation and velocity in the "forward" direction **/
		{
			if (Input.GetAxisRaw("Vertical") == 1)
			{
				setHipRotation(hipRotation.x + 10, hipRotation.y, hipRotation.z); 
				velocityForwardDirection = walkingSpeed; 
				keyDown = true;
				walkingState = WalkingState.FORWARD; 
			}
			if (Input.GetAxisRaw("Vertical") == -1)
			{
				setHipRotation(hipRotation.x - 10, hipRotation.y, hipRotation.z);
				velocityForwardDirection = -walkingSpeed; 
				keyDown = true;
				walkingState = WalkingState.BACKWARD;
			}
		} 

		else if (!keyDown
				&& (Input.GetAxisRaw("Horizontal") != 0)) /**Adjust rotation and velocity in the "right" direction **/
		{
			if (Input.GetAxisRaw("Horizontal") == 1)
			{
				setHipRotation(hipRotation.x, hipRotation.y - 10, hipRotation.z); 
				keyDown = true;
			}

			if (Input.GetAxisRaw("Horizontal") == -1)
			{
				setHipRotation(hipRotation.x, hipRotation.y + 10, hipRotation.z); 
				keyDown = true;
			}
		}
	}

	void setHipRotation(float xRotation, float yRotation, float zRotation)
	{
		hips.setRelativeRotation(Quaternion.Euler(xRotation, yRotation, zRotation));
		hipRotation = hips.transform.rotation.eulerAngles; 
	}

	void moveBody()
	{

		Vector3 forwardDirection = hips.transform.forward;
		forwardDirection.y = 0;
		forwardDirection.Normalize(); 
		body.transform.position += ((forwardDirection * velocityForwardDirection) * Time.deltaTime);

	}

	void adjustIKTargets()
	{
		/**These parameters will be used to determine the position of the feet relative to the center of mass **/ 
		Vector3 forwardDirection = hips.transform.forward;
		forwardDirection.y = 0;
		forwardDirection.Normalize();
		Vector3 centerOfMassProjection = Vector3.Project(centerOfMass.position, forwardDirection); 
		Vector3 rightFootTargetProjection = Vector3.Project(rightFootTarget.transform.position, forwardDirection);
		Vector3 leftFootTargetProjection = Vector3.Project(leftFootTarget.transform.position, forwardDirection);
		Vector3 rightFootDirection = rightFootTargetProjection - centerOfMassProjection;
		Vector3 leftFootDirection = leftFootTargetProjection - centerOfMassProjection; 

		if ((Vector3.Angle(forwardDirection, rightFootDirection) == 0)
			&& (Vector3.Angle(forwardDirection, leftFootDirection) == 0)) //Both feet in front of Center, need to step back
		{
			if(rightFootDirection.magnitude > leftFootDirection.magnitude)
			{
				forwardFootTarget = rightFootTarget;
				oppositeHandTarget = leftHandTarget; 
				backFootTarget = leftFootTarget; 
			}
			else
			{
				forwardFootTarget = leftFootTarget;
				oppositeHandTarget = rightHandTarget; 
				backFootTarget = rightFootTarget; 
			}
			if (walkingState != WalkingState.IDLE)
			{
				takeStep();
			}
			else
			{
				adjustBalance(); 
			}
		}

		else if((Vector3.Angle(forwardDirection, rightFootDirection) == 180)
			&& (Vector3.Angle(forwardDirection, leftFootDirection) == 180)) //Both feet behind center, step forward
		{
			if (rightFootDirection.magnitude > leftFootDirection.magnitude)
			{
				forwardFootTarget = leftFootTarget;
				backFootTarget = rightFootTarget;
				oppositeHandTarget = leftHandTarget; 
			}
			else
			{
				forwardFootTarget = rightFootTarget;
				backFootTarget = leftFootTarget;
				oppositeHandTarget = rightHandTarget; 
			}
			if(walkingState != WalkingState.IDLE)
			{
				takeStep();
			}
			else
			{
				adjustBalance(); 
			}
			
		}
		
	}

	void takeStep()
	{
		//Debug.Log("Taking Step"); 
		Vector3 forwardDirection = hips.transform.forward;
		forwardDirection.y = 0;
		forwardDirection.Normalize();
		Vector3 rightDirection = hips.transform.right;
		rightDirection.y = 0;
		rightDirection.Normalize(); 
		Vector3 forwardFootTargetProjection = Vector3.Project(forwardFootTarget.transform.position, forwardDirection);
		Vector3 backFootTargetProjection = Vector3.Project(backFootTarget.transform.position, forwardDirection);
		float forwardFootDistance = Vector3.Magnitude(forwardFootTargetProjection - backFootTargetProjection);
		Vector3 centerOfMassProjection = centerOfMass.position;
		centerOfMassProjection.y = 0; 

		//Debug.Log("Walking State = " + walkingState); 
		if (walkingState == WalkingState.FORWARD)
		{
			if (forwardFootTarget == rightFootTarget)
			{
				backFootTarget.position = centerOfMassProjection + (forwardDirection * (walkingSpeed / 2)) - (rightDirection * legOffset);
				oppositeHandTarget.position = backFootTarget.position + (hips.transform.position.y * new Vector3(0, 1, 0)) + (2 * legOffset * rightDirection); 
			}
			else
			{
				backFootTarget.position = centerOfMassProjection + (forwardDirection * (walkingSpeed / 2)) + (rightDirection * legOffset);
				oppositeHandTarget.position = backFootTarget.position + (hips.transform.position.y * new Vector3(0, 1, 0)) - (2 * legOffset * rightDirection);
			}
		}
		else if(walkingState == WalkingState.BACKWARD)
		{
			if (forwardFootTarget == rightFootTarget)
			{
				forwardFootTarget.position = centerOfMassProjection - (forwardDirection * (walkingSpeed / 2)) + (rightDirection * legOffset);
				oppositeHandTarget.position = forwardFootTarget.position + (hips.transform.position.y * new Vector3(0, 1, 0)) - (2 * legOffset * rightDirection); 
			}
			else
			{
				forwardFootTarget.position = centerOfMassProjection - (forwardDirection * (walkingSpeed / 2)) - (rightDirection * legOffset);
				oppositeHandTarget.position = forwardFootTarget.position + (hips.transform.position.y * new Vector3(0, 1, 0)) + (2 * legOffset * rightDirection);
			}
		}
		Transform tempFoot = forwardFootTarget;
		forwardFootTarget = backFootTarget;
		backFootTarget = tempFoot;
		forwardFootTarget.forward = forwardDirection;
		return;
	}

	void adjustBalance()
	{
		Vector3 forwardDirection = hips.transform.forward;
		forwardDirection.y = 0;
		forwardDirection.Normalize();
		Vector3 centerOfMassProjection = Vector3.Project(centerOfMass.position, forwardDirection);
		Vector3 rightDirection = hips.transform.right;
		rightDirection.y = 0;
		rightDirection.Normalize();
		Vector3 forwardFootTargetProjection = Vector3.Project(forwardFootTarget.transform.position, forwardDirection);
		Vector3 backFootTargetProjection = Vector3.Project(backFootTarget.transform.position, forwardDirection);
		Vector3 forwardFootDirection = forwardFootTargetProjection - centerOfMassProjection;
		Vector3 backFootDirection = backFootTargetProjection - centerOfMassProjection;


		if ((Vector3.Angle(forwardDirection, forwardFootDirection) == 0)
			&& (Vector3.Angle(forwardDirection, backFootDirection) == 0)) //Both feet in front of Center, need to step back 
		{
			float forwardFootDistance = Vector3.Magnitude(forwardFootTargetProjection - centerOfMassProjection);
			if (forwardFootTarget == rightFootTarget)
			{ 
				forwardFootTarget.position = centerOfMassProjection - (forwardDirection * (forwardFootDistance)) + (rightDirection * legOffset);
				oppositeHandTarget.position = forwardFootTarget.position + (hips.transform.position.y * new Vector3(0, 1, 0)) - (2 * legOffset * rightDirection);
			}
			else
			{
				forwardFootTarget.position = centerOfMassProjection - (forwardDirection * (forwardFootDistance)) - (rightDirection * legOffset);
				oppositeHandTarget.position = forwardFootTarget.position + (hips.transform.position.y * new Vector3(0, 1, 0)) + (2 * legOffset * rightDirection);
			}
		}
		else if ((Vector3.Angle(forwardDirection, forwardFootDirection) == 180)
			&& (Vector3.Angle(forwardDirection, backFootDirection) == 180)) //Both feet behind center, step forward
		{
			float backFootDistance = Vector3.Magnitude(backFootTargetProjection - centerOfMassProjection);
			if (forwardFootTarget == rightFootTarget)
			{
				backFootTarget.position = centerOfMassProjection + (forwardDirection * (backFootDistance)) - (rightDirection * legOffset);
				oppositeHandTarget.position = forwardFootTarget.position + (hips.transform.position.y * new Vector3(0, 1, 0)) + (2 * legOffset * rightDirection);
			}
			else
			{
				backFootTarget.position = centerOfMassProjection + (forwardDirection * (backFootDistance)) + (rightDirection * legOffset);
				oppositeHandTarget.position = forwardFootTarget.position + (hips.transform.position.y * new Vector3(0, 1, 0)) - (2 * legOffset * rightDirection);
			}
		}

	}

	void keepBodyUpright()
	{
		leftFootCollider.transform.up = leftFootTarget.up;
		rightFootCollider.transform.up = rightFootTarget.up;
		Vector3 bodyDirection = body.transform.up.normalized;
		float bodyAngle = Vector3.Angle(bodyDirection, Vector3.up);
		Debug.Log("Body direction = " + bodyDirection + ", Angle = " + bodyAngle); 
		if(bodyAngle > 15 || bodyAngle < -15)
		{
			Rigidbody rigidBody = body.GetComponent<Rigidbody>();
			Vector3 forceDirection = Vector3.up - body.transform.up;
			//forceDirection.y = 0; 
			Vector3 force = forceDirection.normalized * forceStrength;
			Debug.Log("Force = " + force);
			Debug.DrawRay(body.transform.position, bodyDirection, Color.blue);
			Debug.DrawRay(body.transform.position, Vector3.up, Color.red);
			Debug.DrawRay(body.transform.position + bodyDirection, force, Color.green); 
			rigidBody.AddForceAtPosition(force, centerOfMass.position,  ForceMode.Impulse); 
		}
	}

}
