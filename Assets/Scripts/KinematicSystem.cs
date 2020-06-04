using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class KinematicSystem : MonoBehaviour
{
	/** CLASS VARIABLES 
	 * ---------------------------------------------------------------------------------------------------------**/

	[SerializeField] KinematicBone root;
	[SerializeField] private KinematicBone tip;
	[SerializeField] private List<KinematicBone> bones;

	[SerializeField] Transform IKTarget;

	Dictionary<KinematicBone, Vector3> currentAngles;

	[SerializeField] float samplingDistance;
	[SerializeField] float learningRate;
	[SerializeField] float thresholdDistance;


	[SerializeField] bool inverse;/**This determines whether the system is updating Kinematics or Inverse Kinematics **/
	private bool firstUpdateFlag; //This is necessary to link bones after all bones have been initialized

	[SerializeField] UnityEvent optimize;
	[HideInInspector] public float returnValue; //This is used to capture the return value from the Optimize function

	/** UNITY FUNCTIONS 
	 * ----------------------------------------------------------------------------------------------------------------**/

	void Start()
	{
		firstUpdateFlag = true; 
	}

	void firstUpdate()
	{
		bones = new List<KinematicBone>();
		currentAngles = new Dictionary<KinematicBone, Vector3>();
		recursiveAdd(root);
	}

	// Update is called once per frame
	void Update()
	{
		if(firstUpdateFlag)
		{
			firstUpdate();
			firstUpdateFlag = false; 
		}
		foreach (KinematicBone bone in bones)
		{
			currentAngles[bone] = bone.getRelativeEulerAngles();
		}
		if (inverse)
		{
			
			updateInverseKinematics();
		}
		else
		{
			updateKinematics();
		}
	}

	void recursiveAdd(KinematicBone current)
	{
		bones.Add(current);
		Vector3 currentAngle = current.transform.rotation.eulerAngles * 1; 
		currentAngles[current] = currentAngle; 
		if(current.getChildren().Count != 0 && current != tip)
		{
			foreach(KinematicBone child in current.getChildren())
			{
				recursiveAdd(child); 
			}
		}
	}

	/** KINEMATIC FUNCTIONS 
	 * ------------------------------------------------------------------------------------------------------**/

	public void updateInverseKinematics()
	{
		KinematicBone bone;
		for (int i = 0; i < bones.Count; i++)
		{
			bone = bones[i];
			singleAxisGradientDescent(Vector3.right, bone); //Test rotation around the X axis
			singleAxisGradientDescent(Vector3.up, bone); //Test rotation around the Y axis
			singleAxisGradientDescent(Vector3.forward, bone); //Test rotation around the Z axis
		}
		updateKinematics(); 
	}

	public void singleAxisGradientDescent(Vector3 axis, KinematicBone bone)
	{
		float randomSign = -1;
		float randomExp = Random.Range(1, 2);
		randomSign = Mathf.Pow(randomSign, randomExp);
		Vector3 savedAngle = currentAngles[bone];
		Vector3 currentAngle = savedAngle * 1;
		optimize.Invoke();
		float initialDistance = returnValue; 
		if (initialDistance > thresholdDistance)
		{
			currentAngle += (axis * (samplingDistance * randomSign));
			currentAngles[bone] = currentAngle;
			optimize.Invoke();
			float newDistance = returnValue; 
			float gradient = (newDistance - initialDistance) / (samplingDistance * randomSign);
			// Restores
			currentAngles[bone] = savedAngle;
			currentAngle = savedAngle;
			currentAngle -= (axis * (learningRate * gradient));
			currentAngles[bone] = currentAngle;
		}
	}

	public Vector3 updateKinematics()
	{
		KinematicBone current;
		for (int i = 0; i < bones.Count; i++)
		{
			current = bones[i]; 
			//Debug.Log("Updating Kinematics for " + bones[i].name + "\n");
			current.updateKinematics(currentAngles[current]);
		}
		return tip.getEndPoint().transform.position; 
	}

	/** GETTERS 
	 * --------------------------------------------------------------------------------------------------------**/

	public Transform getIKTarget()
	{
		return IKTarget; 
	}

	/**Optimization Functions
	 * ----------------------------------------------------------------------------------------------------**/

	public void minimizeTipEndpointDistance()
	{
		Vector3 tipLocation = updateKinematics();
		returnValue = Vector3.Distance(tipLocation, IKTarget.position);
	}

	public void minimizeTipCenterDistance()
	{
		Vector3 tipLocation = updateKinematics();
		returnValue = Vector3.Distance(tip.transform.position, IKTarget.position); 
	}

}

