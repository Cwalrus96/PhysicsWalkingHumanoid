using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/**This class is designed to hold multiple possible optimization functions that can be switched in and out for
	different Kinematic Systems **/
public class Optimizer : UnityEvent <KinematicSystem>
{

	/**This function can be used to optimize the distance from the endpoint of the tip bone
	 * to the IKTarget **/ 
	public static void minimizeTipEndpointDistance(KinematicSystem kinematicSystem)
	{
		Vector3 tipLocation = kinematicSystem.updateKinematics();
		kinematicSystem.returnValue =  Vector3.Distance(tipLocation, kinematicSystem.getIKTarget().position); 
	}

}
