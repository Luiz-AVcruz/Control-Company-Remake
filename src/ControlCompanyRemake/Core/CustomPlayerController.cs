using System.Collections.Generic;
using System.Linq;
using ControlCompanyRemake.Core.Input;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace ControlCompanyRemake.Core;

internal class CustomPlayerController : MonoBehaviour
{
	public enum CameraMode
	{
		THIRD_PERSON,
		FIRST_PERSON
	}

	protected bool isInitialized = false;

	protected bool hasControl = false;

	protected bool canMove = false;

	protected bool canRotate = false;

	protected bool canJump = false;

	protected CameraMode camMode;

	protected CharacterController characterController;

	protected GameObject cameraGameObjectParent;

	protected GameObject cameraGameObject;

	protected Camera camera;

	protected Light light;

	protected PlayerActions playerActions;

	protected DoorOpener doorOpener;

	private AudioListener audioListener;

	private float runSpeed = 9f;

	private float walkSpeed = 7f;

	private float camDistance = -3.5f;

	private float camHeight = 2f;

	private float gravity = -20f;

	private float jumpHeight = 4.5f;

	private float currentYVelocity;

	private List<Collider> collidersToIgnore = new List<Collider>();

	public float RunSpeed
	{
		get
		{
			return runSpeed;
		}
		set
		{
			runSpeed = value;
		}
	}

	public float WalkSpeed
	{
		get
		{
			return walkSpeed;
		}
		set
		{
			walkSpeed = value;
		}
	}

	public float Gravity
	{
		get
		{
			return gravity;
		}
		set
		{
			gravity = value;
		}
	}

	public AudioListener AudioListener
	{
		get
		{
			return audioListener;
		}
		set
		{
			audioListener = value;
		}
	}

	public bool IsInitialized
	{
		get
		{
			return isInitialized;
		}
		set
		{
			isInitialized = value;
		}
	}

	public void Initialize(PlayerActions playerActions, CameraMode camMode, List<Collider> collidersToIgnore = null)
	{
		if (!isInitialized)
		{
			this.playerActions = playerActions;
			this.camMode = camMode;
			if (collidersToIgnore != null)
			{
				this.collidersToIgnore = collidersToIgnore;
			}
			characterController = InitializeCharacterController();
			camera = InitializeCamera();
			light = InitializeLight(camera.gameObject);
			doorOpener = base.gameObject.AddComponent<DoorOpener>();
			EnableControl(enable: true);
			isInitialized = true;
		}
	}

	public void EnableLight(bool enable)
	{
		light.enabled = enable;
	}

	public void EnableCamera(bool enable)
	{
		camera.enabled = enable;
	}

	public void ToggleLight()
	{
		light.enabled = !light.enabled;
	}

	public void EnableControl(bool enable)
	{
		hasControl = enable;
		canMove = enable;
		canRotate = enable;
		canJump = enable;
	}

	public bool IsMoving()
	{
		return characterController.velocity.sqrMagnitude > 0f;
	}

	public void SetCamHeight(float camHeight)
	{
		this.camHeight = camHeight;
		cameraGameObjectParent.transform.localPosition = new Vector3(0f, camHeight, 0f);
	}

	public void SetCamDistance(float camDistance)
	{
		this.camDistance = camDistance;
		cameraGameObject.transform.localPosition = new Vector3(0f, 0f, (camMode == CameraMode.THIRD_PERSON) ? camDistance : 0f);
	}

	protected virtual void LateUpdate()
	{
		if (isInitialized && hasControl && !ControlCenter.LocalInputBlocked)
		{
			if (canMove)
			{
				Move();
			}
			if (canRotate)
			{
				Rotate();
			}
			if (InputKeys.CONTROLLER_USE_KEY.wasPressedThisFrame)
			{
				doorOpener.DetectAndOpenDoors(3f);
			}
		}
	}

	protected virtual Vector3 MoveInput()
	{
		return IngamePlayerSettings.Instance.playerInput.actions.FindAction("Move").ReadValue<Vector2>();
	}

	protected virtual bool IsSprinting()
	{
		return IngamePlayerSettings.Instance.playerInput.actions.FindAction("Sprint").ReadValue<float>() > 0f;
	}

	protected virtual void Move()
	{
		bool flag = IsSprinting();
		Vector3 vector = MoveInput();
		Vector3 vector2 = base.transform.right * vector.x;
		Vector3 vector3 = base.transform.forward * vector.y;
		bool isGrounded = characterController.isGrounded;
		if (isGrounded && currentYVelocity < 0f)
		{
			currentYVelocity = 0f;
		}
		if (canJump && isGrounded && InputKeys.CONTROLLER_JUMP_KEY.wasPressedThisFrame)
		{
			currentYVelocity += Mathf.Sqrt(jumpHeight * (0f - gravity));
		}
		currentYVelocity += gravity * Time.deltaTime;
		characterController.Move(((vector2 + vector3) * (flag ? runSpeed : walkSpeed) + new Vector3(0f, currentYVelocity, 0f)) * Time.deltaTime);
	}

	protected virtual void Rotate()
	{
		Vector2 vector = playerActions.Movement.Look.ReadValue<Vector2>() * 0.008f * IngamePlayerSettings.Instance.settings.lookSensitivity;
		if (IngamePlayerSettings.Instance.settings.invertYAxis)
		{
			vector.y *= -1f;
		}
		base.transform.Rotate(new Vector3(0f, vector.x, 0f));
		cameraGameObjectParent.transform.Rotate(new Vector3(0f - vector.y, 0f, 0f));
	}

	private Light InitializeLight(GameObject lightParent)
	{
		GameObject gameObject = new GameObject("controller_light");
		gameObject.transform.SetParent(lightParent.transform);
		gameObject.transform.localPosition = new Vector3(0f, 0.5f, 0f);
		gameObject.transform.localRotation = Quaternion.identity;
		Light light = gameObject.AddComponent<Light>();
		light.range = 15f;
		light.intensity = 225f;
		light.cullingMask = -1;
		return light;
	}

	private CharacterController InitializeCharacterController()
	{
		CharacterController controller = base.gameObject.AddComponent<CharacterController>();
		controller.excludeLayers = (int)Constants.PLAYER_LOS_LAYERMASK | (int)Constants.ITERACTABLE_OBJ_LAYERMASK | (int)Constants.ENEMIES_LAYERMASK | (int)Constants.PROPS_LAYERMASK | (int)Constants.PLAYER_RAGDOLL_LAYERMASK;
		controller.center = new Vector3(0f, 0.55f, 0f);
		controller.height = 0.4f;
		ControlCenter.Instance.GetAllPlayers().ForEach(delegate(PlayerControllerB p)
		{
			if (p != null)
			{
				p.GetComponentsInChildren<Collider>().ToList().ForEach(delegate(Collider c)
				{
					Physics.IgnoreCollision(c, controller);
				});
			}
		});
		if (collidersToIgnore.Count() > 0)
		{
			foreach (Collider item in collidersToIgnore)
			{
				Physics.IgnoreCollision(item, controller);
			}
		}
		controller.slopeLimit = 60f;
		controller.stepOffset = 0.4f;
		return controller;
	}

	private Camera InitializeCamera()
	{
		GameObject gameObject = new GameObject("controller_cam_parent");
		gameObject.transform.SetParent(base.transform);
		gameObject.transform.localRotation = Quaternion.identity;
		gameObject.transform.localPosition = new Vector3(0f, camHeight, 0f);
		GameObject gameObject2 = new GameObject("controller_cam");
		gameObject2.transform.SetParent(gameObject.transform);
		gameObject2.transform.localRotation = Quaternion.identity;
		gameObject2.transform.localPosition = new Vector3(0f, 0f, (camMode == CameraMode.THIRD_PERSON) ? camDistance : 0f);
		Camera camera = gameObject2.AddComponent<Camera>();
		audioListener = gameObject2.AddComponent<AudioListener>();
		cameraGameObject = gameObject2;
		cameraGameObjectParent = gameObject;
		Camera gameplayCamera = ControlCenter.Instance.GetHostPlayer().gameplayCamera;
		camera.cullingMask = gameplayCamera.cullingMask;
		camera.farClipPlane = gameplayCamera.farClipPlane;
		HDAdditionalCameraData component = gameplayCamera.gameObject.GetComponent<HDAdditionalCameraData>();
		CopyHDCameraData(camera, component);
		return camera;
	}

	private void CopyHDCameraData(Camera cam, HDAdditionalCameraData dataToCopy)
	{
		if (dataToCopy != null)
		{
			HDAdditionalCameraData hDAdditionalCameraData = cam.gameObject.GetComponent<HDAdditionalCameraData>();
			if (hDAdditionalCameraData == null)
			{
				hDAdditionalCameraData = cam.gameObject.AddComponent<HDAdditionalCameraData>();
			}
			if (hDAdditionalCameraData != null)
			{
				hDAdditionalCameraData.customRenderingSettings = true;
				hDAdditionalCameraData.renderingPathCustomFrameSettingsOverrideMask.mask = dataToCopy.renderingPathCustomFrameSettingsOverrideMask.mask;
				SetCustomPass(hDAdditionalCameraData, dataToCopy);
				SetVolumetricFog(hDAdditionalCameraData, dataToCopy);
				SetLOD(hDAdditionalCameraData, dataToCopy);
				SetAliasing(hDAdditionalCameraData, dataToCopy);
				SetShadowQuality(hDAdditionalCameraData, dataToCopy);
			}
		}
	}

	private void SetCustomPass(HDAdditionalCameraData camData, HDAdditionalCameraData dataToCopy)
	{
		camData.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.CustomPass, dataToCopy.renderingPathCustomFrameSettings.IsEnabled(FrameSettingsField.CustomPass));
	}

	private void SetVolumetricFog(HDAdditionalCameraData camData, HDAdditionalCameraData dataToCopy)
	{
		camData.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.Volumetrics, dataToCopy.renderingPathCustomFrameSettings.IsEnabled(FrameSettingsField.Volumetrics));
	}

	private void SetLOD(HDAdditionalCameraData camData, HDAdditionalCameraData dataToCopy)
	{
		camData.renderingPathCustomFrameSettings.lodBiasMode = dataToCopy.renderingPathCustomFrameSettings.lodBiasMode;
		camData.renderingPathCustomFrameSettings.lodBias = dataToCopy.renderingPathCustomFrameSettings.lodBias;
	}

	private void SetAliasing(HDAdditionalCameraData camData, HDAdditionalCameraData dataToCopy)
	{
		camData.antialiasing = dataToCopy.antialiasing;
	}

	private void SetShadowQuality(HDAdditionalCameraData camData, HDAdditionalCameraData dataToCopy)
	{
		camData.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.ShadowMaps, dataToCopy.renderingPathCustomFrameSettings.IsEnabled(FrameSettingsField.ShadowMaps));
	}
}
