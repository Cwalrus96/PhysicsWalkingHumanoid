using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** This class will be used to give an object the behavior of a kinematic object. Basically, it should function as a 3D version of the octopus project in Processing
 *	This object will have 2 main modes. In Kinematic mode it will adjust it's position and orientation based on the position and orientation of it's "kinematicParent" 
 *	This should function identically to simply parenting the objects in the Unity editor, but the objects will be independent. 
 *	The second mode is Inverse Kinematic mode. In this mode the endpoint will try to match it's position and rotation to an object in the scene, and it will update the 
 *	position and orientation of any parent objects. **/
public class KinematicBone : MonoBehaviour
{
	//CLASS VARIABLES
	//----------------------------------------------------------------------------------------------

	private List<KinematicBone> children;
	public KinematicBone parent;
	
	[SerializeField] GameObject boneMesh; //This object should contain a mesh and a mesh renderer

	/**Anchor and End points are based on the boneMesh's local Z axis **/
	Transform anchorPoint; //Represents the point where this object is "linked" to it's parent 
	Transform endPoint;    //Represents the point where this object is "linked" to it's children 

	/**These relative angles are used to adjust the relative position of the child bone either with code 
	 * or in the inspector **/
	[SerializeField] Vector3 relativeEulerAngles;

	/**anchorVector and endVector keep track of the positions of the joints relative to the bones **/
	Vector3 anchorVector;
	Vector3 endVector;

	Dictionary<KinematicBone, Vector3> childAnchors; /**This list ensures that all child bones remain in the same relative positions to the parent **/

	private Quaternion baseRotation;    //baseRotation is the "base" or "initial" orientation of the object relative to it's parent
	private Quaternion relativeRotation;    //relativeRotation is the rotation of the bone relative to it's initial orientation. 
											//This is very useful for applying restrictions on rotations

	[SerializeField] float maxRotX;
	[SerializeField] float minRotX;
	[SerializeField] float maxRotY;
	[SerializeField] float minRotY;
	[SerializeField] float maxRotZ;
	[SerializeField] float minRotZ;

	[SerializeField] bool rotateFromCenter; /**Some objects rotate from the center rather than from an endpoint **/

	//UNITY FUNCTIONS
	//----------------------------------------------------------------------------------------------

	/** Awake initializes this object, so all bones are initialized before being linked with 
	 * Kinematic System **/
	void Awake()
	{
		relativeRotation = Quaternion.identity;
		endPoint = new GameObject(this.name + "EndPoint").transform;
		endPoint.parent = transform; 
		anchorPoint = new GameObject(this.name + "AnchorPoint").transform;
		anchorPoint.parent = transform; 
		Renderer rend = boneMesh.GetComponent<MeshRenderer>();
		Mesh localMesh = boneMesh.GetComponent<MeshFilter>().mesh; 
		float zLength = (localMesh.bounds.extents.z * boneMesh.transform.localScale.z);
		//Debug.Log("Zlength of " + name + " = " + zLength);
		Vector3 forwardOffset = boneMesh.transform.forward * zLength;
		if (rotateFromCenter)
		{
			anchorPoint.position = rend.bounds.center; 
		}
		else
		{
			anchorPoint.position = rend.bounds.center - forwardOffset;
		}
		endPoint.position = rend.bounds.center + forwardOffset; 
		endVector = endPoint.position - transform.position;
		anchorVector = transform.position - anchorPoint.position;
		endVector = Quaternion.Inverse(transform.rotation) * endVector;
		anchorVector = Quaternion.Inverse(transform.rotation) * anchorVector;
		children = new List<KinematicBone>();
		childAnchors = new Dictionary<KinematicBone, Vector3>();
		relativeEulerAngles = Vector3.zero;
	}

	private void Start()
	{
		if (parent != null)//If parent isn't null, positions and orientations are all relative to parent
		{
			parent.children.Add(this);
			Transform parentTransform = parent.transform;
			Vector3 parentOffset = parent.endPoint.position - anchorPoint.transform.position;
			parentOffset = Quaternion.Inverse(parent.transform.rotation) * parentOffset;
			parent.childAnchors.Add(this, parentOffset);
			baseRotation = transform.rotation * Quaternion.Inverse(parent.transform.rotation);
		}
		else //Otherwise, everything is absolute, or "relative" to the scene
		{
			baseRotation = transform.localRotation;
		}
		baseRotation.Normalize();
	}

	
	void Update()
	{
		/** Update is not called on each individual IK bone. Instead, updates are controlled by the
		 * KinematicSystem class **/
	}

	//KINEMATIC FUNCTIONS
	//-----------------------------------------------------------------------------------------------

	/**Update the position and orientation of this object, then update all child objects based on 
	the new position and orientation of this object. **/
	public void updateKinematics(Vector3 Angles)
	{
		setRelativeRotation(Quaternion.Euler(Angles)); 
		calculateEndpoints();
		if (children.Count != 0)
		{
			foreach(KinematicBone child in children)
			{
				child.calculateEndpoints();  
			}
		}
		//Vector3 facingDirection = endPoint.position - anchorPoint.position;
		//Debug.DrawRay(anchorPoint.position, facingDirection, Color.black, 0.01f, false);

		
	}



	/**For now I will simply update the positions of the endPoint based on the position
	 * of the anchor, the bone orientation, and the boneLength **/
	public void calculateEndpoints()
	{
		if (parent != null)
		{
			anchorPoint.position = parent.endPoint.position - (parent.transform.rotation * parent.childAnchors[this]);
			//Debug.DrawLine(parent.endPoint.position, parent.endPoint.position - (parent.transform.rotation * parent.childAnchors[this]), Color.cyan, 1.0f);
		}
		transform.position = anchorPoint.position + (transform.rotation * anchorVector);
		endPoint.position = transform.position + (transform.rotation * endVector);
		
	}


	/**Make sure that the rotations do not exceed the maximum or minimum values in any axis. 
	 * These values are all relative to the bone's base orientation **/
	public Vector3 getConstrainedRotation(Vector3 eulerAngles)
	{
		Vector3 baseAngles = baseRotation.eulerAngles;
		float tempX = eulerAngles.x - baseAngles.x;
		float tempY = eulerAngles.y - baseAngles.y;
		float tempZ = eulerAngles.z - baseAngles.z;

		if (tempX > 180)
		{
			tempX -= 360;
		}
		if (tempY > 180)
		{
			tempY -= 360;
		}
		if (tempZ > 180)
		{
			tempZ -= 360;
		}

		if (tempX > maxRotX)
		{
			tempX = maxRotX;
		}
		else if (tempX < minRotX)
		{
			tempX = minRotX;
		}
		if (tempY > maxRotY)
		{
			tempY = maxRotY;
		}
		else if (tempY < minRotY)
		{
			tempY = minRotY;
		}
		if (tempZ > maxRotZ)
		{
			tempZ = maxRotZ;
		}
		else if (tempZ < minRotZ)
		{
			tempZ = minRotZ;
		}

		if (tempX < 0)
		{
			tempX += 360;
		}
		if (tempY < 0)
		{
			tempY += 360;
		}
		if (tempZ < 0)
		{
			tempZ += 360;
		}

		return new Vector3(tempX, tempY, tempZ);
	}

	//GETTERS
	//-------------------------------------------------------------------------------------------------

	public List<KinematicBone> getChildren()
	{
		return children;
	}

	public Transform getEndPoint()
	{
		return endPoint;
	}

	public Transform getAnchorPoint()
	{
		return anchorPoint;
	}

	public Vector3 getAnchorVector()
	{
		return anchorVector;
	}

	public Vector3 getEndVector()
	{
		return endVector;
	}

	public Dictionary<KinematicBone, Vector3> getChildAnchors()
	{
		return childAnchors; 
	}

	public Vector3 getRelativeEulerAngles()
	{
		return relativeEulerAngles; 
	}


	//SETTERS
	//------------------------------------------------------------------------------------------------


	/*This function should always be called when setting the relative rotation of a bone - should never be set independently */
	public void setRelativeRotation(Quaternion rotation)
	{
		Transform objectTransform = GetComponent<Transform>();
		relativeEulerAngles = getConstrainedRotation(rotation.eulerAngles);
		relativeRotation.eulerAngles = relativeEulerAngles;
		if (parent != null)
		{
			Vector3 combinedRotation = relativeRotation.eulerAngles + baseRotation.eulerAngles + parent.transform.rotation.eulerAngles;
			objectTransform.rotation = Quaternion.Euler(combinedRotation); 
			/**if (name == "Hips" || name == "RightUpperLeg")
			{
				Debug.Log("Bone = " + name + ", Relative Rotation = " + relativeRotation.eulerAngles + ", baseRotation = " + baseRotation.eulerAngles + ", parent rotation = " + parent.transform.rotation.eulerAngles);
				Debug.Log("Intended Total Rotation = " + combinedRotation + ", Actual Total Rotation = " + objectTransform.rotation.eulerAngles);
			}*/
		}
		else
		{
			Quaternion parentRotation = Quaternion.identity; 
			if(transform.parent != null)
			{
				parentRotation = parentRotation * transform.parent.rotation; 
			}
			objectTransform.rotation = relativeRotation * baseRotation * parentRotation;
		} 
		
	}

}
