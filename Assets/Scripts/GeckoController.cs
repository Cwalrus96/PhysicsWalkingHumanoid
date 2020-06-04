using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeckoController : MonoBehaviour
{
	[SerializeField] Transform headTarget;
	[SerializeField] Transform frontRightFootTarget;
	[SerializeField] Transform frontLeftFootTarget;
	[SerializeField] Transform backRightFootTarget;
	[SerializeField] Transform backLeftFootTarget; 

	[SerializeField] KinematicBone tip;
	[SerializeField] KinematicBone shoulders;
	[SerializeField] KinematicBone hips;
	[SerializeField] KinematicBone root;
	[SerializeField] KinematicBone frontRightFoot;
	[SerializeField] KinematicBone frontLeftFoot;
	[SerializeField] KinematicBone backRightFoot;
	[SerializeField] KinematicBone backLeftFoot; 

	[SerializeField] float movementSpeed;
	[SerializeField] float minDistance;
	[SerializeField] float maxDistance;
	[SerializeField] float minFootDistance;
	[SerializeField] float maxFootDistance;
	[SerializeField] float closeEnough; 

	Vector3 frontRightFootVector;
	Vector3 frontLeftFootVector;
	Vector3 backRightFootVector;
	Vector3 backLeftFootVector;

	bool step1; //This will step with the right front and back left legs
	bool step2; //This will step with the left front and right back legs; 
	
	

	// Start is called before the first frame update
    void Start()
    {
		frontRightFootVector = Quaternion.Inverse(shoulders.transform.rotation) * (frontRightFootTarget.position - shoulders.transform.position);
		frontLeftFootVector = Quaternion.Inverse(shoulders.transform.rotation) * (frontLeftFootTarget.position - shoulders.transform.position);
		backRightFootVector = Quaternion.Inverse(hips.transform.rotation) * (backRightFootTarget.position - hips.transform.position);
		backLeftFootVector = Quaternion.Inverse(hips.transform.rotation) * (backLeftFootTarget.position - hips.transform.position);
		step1 = false;
		step2 = false; 
	}

	void LateUpdate()
    {
		updateGeckoPosition();
		updateFeetPositions(); 
    }

	void updateGeckoPosition()
	{
		if (!(step1 || step2))
		{
			//We want to move the Gecko towards and away from the target, but only in the X and Z directions
			float xDistance = headTarget.position.x - tip.getEndPoint().position.x;
			float zDistance = headTarget.position.z - tip.getEndPoint().position.z;
			Vector3 movementVector = new Vector3(xDistance, 0, zDistance);
			if (movementVector.magnitude > maxDistance)
			{
				movementVector = movementVector.normalized * (movementSpeed * Time.deltaTime);
				root.getAnchorPoint().position += movementVector;
				root.calculateEndpoints();
			}
			else if (movementVector.magnitude < minDistance)
			{
				movementVector = movementVector.normalized * (movementSpeed * Time.deltaTime);
				root.getAnchorPoint().position -= movementVector;
				root.calculateEndpoints();
			}
		}
	}

	void updateFeetPositions()
	{

		if(step1)
		{
			if((Vector3.Distance(frontRightFoot.getEndPoint().transform.position, frontRightFootTarget.position) < closeEnough) 
				&& (Vector3.Distance(backLeftFoot.getEndPoint().transform.position, backLeftFootTarget.position) < closeEnough))
			{
				Debug.Log("Done Stepping 1"); 
				step1 = false; 
			}
			else
			{
				Debug.Log("frontRightFootDistance = " + Vector3.Distance(frontRightFoot.getEndPoint().transform.position, frontRightFootTarget.position)); 
			}
		}

		else if(step2)
		{
			if ((Vector3.Distance(frontLeftFoot.getEndPoint().transform.position, frontLeftFootTarget.position) < closeEnough)
				&& (Vector3.Distance(backRightFoot.getEndPoint().transform.position, backRightFootTarget.position) < closeEnough))
			{
				Debug.Log("Done Stepping 2"); 
				step2 = false;
			}
		}

		else
		{ 
			float frontRightFootDistance = Vector3.Distance(frontRightFootTarget.position, shoulders.transform.position);
			float frontLeftFootDistance = Vector3.Distance(frontLeftFootTarget.position, shoulders.transform.position);
			float backRightFootDistance = Vector3.Distance(backRightFootTarget.position, hips.transform.position);
			float backLeftFootDistance = Vector3.Distance(backLeftFootTarget.position, hips.transform.position);

			Debug.Log("FRDist = " + frontRightFootDistance + ", FLDist = " + frontLeftFootDistance + ", BRDist = " + backRightFootDistance + ", BLDist = " + backLeftFootDistance); 

			if ((frontRightFootDistance > maxFootDistance) || (frontRightFootDistance < minFootDistance)
				||(backLeftFootDistance > maxFootDistance) || (backLeftFootDistance < minFootDistance))
			{
				Debug.Log("Stepping 1"); 
				frontRightFootTarget.position = shoulders.transform.position + (shoulders.transform.rotation * frontRightFootVector);
				backLeftFootTarget.position = hips.transform.position + (hips.transform.rotation * backLeftFootVector);
				step1 = true; 
			}

			else if((frontLeftFootDistance > maxFootDistance) || (frontLeftFootDistance < minFootDistance)
				|| (backRightFootDistance > maxFootDistance) || (backRightFootDistance < minFootDistance))
			{
				Debug.Log("Stepping 2"); 
				frontLeftFootTarget.position = shoulders.transform.position + (shoulders.transform.rotation * frontLeftFootVector);
				backRightFootTarget.position = hips.transform.position + (hips.transform.rotation * backRightFootVector);
				step2 = true;
			}

		}
	}

}
