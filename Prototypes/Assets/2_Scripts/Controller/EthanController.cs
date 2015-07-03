﻿namespace TheVandals
{
	using UnityEngine;
	using System.Collections;
	
	[RequireComponent (typeof (NavMeshAgent), typeof (Animator))]
	public class EthanController : MonoBehaviour {
		
		#region Properties
		[Header("Configuration")]
		[Tooltip("Double Click Delays")]
		float delay = 0.25F;
		[SerializeField]
		State state = State.Idle;
		[SerializeField]
		PlayerState plState = PlayerState.Free;
		
		private float lastClickTimeL = 0F;
		private float lastClickTimeR = 0F;

		private Vector3 linkStart_ ;
		private Vector3 linkEnd_ ;
		private Quaternion linkRot_;

		private NavMeshAgent agent;
		private Animator animator;
		private TargetDestination initDes;

		#endregion
		
		#region Unity
		void Start () 
		{
			agent = GetComponent<NavMeshAgent>();
			animator = GetComponent<Animator>();

			agent.autoTraverseOffMeshLink = false;

			initDes.position = transform.position;
			initDes.eulerAngles = transform.eulerAngles;
		}	
		
		void Update () 
		{
			if(plState == PlayerState.Free)
			{
				if(transform.position.x >= 20 || transform.position.x <= -20)
					isCaught();

				if(state != State.Idle && agent.remainingDistance == 0 && state != State.Climb)
					state = State.Idle;

				if(state != State.Climb)
				{
					if(Input.GetKeyDown(KeyCode.Mouse0))	
						MovePlayer();

					if(Input.GetKeyDown(KeyCode.Mouse1))
						lastClickTimeR = Time.time;
					
					if(Input.GetKeyUp(KeyCode.Mouse1) && Time.time < lastClickTimeR + delay)
						StopPlayer();
				}
				
				
				if (agent.hasPath)
				{		
					if(state == State.Idle)
						state = State.Walk;
				}
				if(agent.isOnOffMeshLink && state != State.Climb)
				{
					StopCoroutine(SelectLinkAnimation());
					StartCoroutine(SelectLinkAnimation());
				}
			}
			
			animator.SetInteger("MoveState", (int)state);
		}
		
		void OnAnimatorMove ()
		{
			if (state != State.Idle && state != State.Climb && plState != PlayerState.Caught)
			{
				agent.velocity = animator.deltaPosition / Time.deltaTime;
				
				if(agent.desiredVelocity != Vector3.zero)
				{
					Quaternion lookRotation = Quaternion.LookRotation(agent.desiredVelocity);
					transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, agent.angularSpeed * Time.deltaTime);
				}
//				transform.rotation = Quaternion.LookRotation(agent.desiredVelocity);
			}		
		}

		#endregion
		
		#region Private
		
		private void MovePlayer()
		{
			agent.SetDestination(RetrieveMousePosition());
			
			if(state != State.Run && Time.time - lastClickTimeL < delay)
			{
				state = State.Run;
			}
			else if(state != State.Walk && Time.time - lastClickTimeL >= delay && Input.GetKeyDown(KeyCode.Mouse0))
			{
				state = State.Walk;
			}

			if(Input.GetKeyDown(KeyCode.Mouse0))
				lastClickTimeL = Time.time;
		}
		
		private void StopPlayer()
		{
			agent.SetDestination(transform.position);
			state = State.Idle;
		}

		public void isCaught()
		{
			if(state != State.Climb)
				agent.Warp(initDes.position);
		}

		private Vector3 RetrieveMousePosition()
		{			
			Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			
			if (Physics.Raycast(mouseRay, out hit,Mathf.Infinity,1 << 8))
			{
				PointerProjector.Instance.Project(hit.point,Color.white);
				return hit.point;
			}
			return transform.position;
		}


		private IEnumerator Locomotion_ClimbAnimation() 
		{
			agent.Stop();

			state = State.Climb;		
			animator.SetInteger("MoveState", (int)state);

			animator.Play("Idle_ToJumpUpHigh");	
			
			Quaternion startRot = transform.rotation;
			Vector3 startPos = transform.position;
			float blendTime = 0.2F;
			float tblend = 0F;



			do {
				transform.position = Vector3.Lerp(startPos, linkStart_, tblend/blendTime);
				transform.rotation = Quaternion.Slerp(startRot, linkRot_, tblend/blendTime);
				yield return null;
				tblend += Time.deltaTime;
			} while(tblend < blendTime);
			transform.position = linkStart_;
			
//			anim_.CrossFade(linkAnimationName, 0.1, PlayMode.StopAll);
//			agent.ActivateCurrentOffMeshLink(false);
			do {
				transform.rotation = linkRot_;
				transform.position += new Vector3(animator.deltaPosition.x,animator.deltaPosition.y * 2.1F / 3,animator.deltaPosition.z);
				
				yield return null;
			} while(animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1);

//			agent.ActivateCurrentOffMeshLink(true);

			state = State.Idle;
			animator.Play("Idle");

			agent.CompleteOffMeshLink();
			agent.Resume();
		}

		private IEnumerator Locomotion_JumpAnimation() 
		{
			agent.Stop();
			state = State.Climb;

			do{
				transform.position = Vector3.Slerp(transform.position, linkEnd_,agent.speed);
					yield return null;
			} while(transform.position != linkEnd_);
			
			agent.CompleteOffMeshLink();
			agent.Resume();

			state = State.Idle;
			animator.Play("Idle");
		}

		private string SelectLinkAnimation() {
			OffMeshLinkData link  = agent.currentOffMeshLinkData;
			float distS  = (transform.position - link.startPos).magnitude;
			float distE  = (transform.position - link.endPos).magnitude;
			if(distS < distE) {
				linkStart_ = link.startPos;
				linkEnd_ = link.endPos;
			} else {
				linkStart_ = link.endPos;
				linkEnd_ = link.startPos;
			}
		
			Vector3 alignDir  = linkEnd_ - linkStart_;
			alignDir.y = 0;
			linkRot_ = Quaternion.LookRotation(alignDir);

			if(link.linkType == OffMeshLinkType.LinkTypeManual && linkStart_.y < linkEnd_.y) {
				return "Locomotion_ClimbAnimation";
			} else {
				return "Locomotion_JumpAnimation";
			}
		}
		#endregion
		
		#region Editor API
		#endregion 
	}
}