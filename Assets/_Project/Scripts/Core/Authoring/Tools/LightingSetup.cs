using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Reflection;

namespace Project.Core.Authoring
{
	/// 运行时一键设置更有质感的灯光与后期
	public class LightingSetup : MonoBehaviour
	{
		[Header("Apply")]
		[SerializeField] bool applyOnStart = true;

		[Header("Sky & Ambient")]
		[SerializeField] Material skyboxMaterial;
		[SerializeField] float ambientIntensity = 1.0f;

		[Header("Main (Key) Light")]
		[SerializeField] bool configureDirectionalLight = true;
		[SerializeField] Light existingMainLight;
		[SerializeField] Vector3 keyLightEuler = new Vector3(50f, -30f, 0f);
		[SerializeField] Color keyLightColor = new Color(1.0f, 0.956f, 0.84f);
		[SerializeField] float keyLightIntensity = 1.35f;
		[SerializeField] LightShadows keyLightShadows = LightShadows.Soft;
		[SerializeField] LightShadowResolution keyShadowResolution = LightShadowResolution.VeryHigh;
		[SerializeField] float shadowBias = 0.05f;
		[SerializeField] float shadowNormalBias = 0.4f;

		[Header("Fill & Rim Lights")]
		[SerializeField] bool addFillLight = true;
		[SerializeField] Vector3 fillLightEuler = new Vector3(15f, 140f, 0f);
		[SerializeField] Color fillLightColor = new Color(0.8f, 0.9f, 1.0f);
		[SerializeField] float fillLightIntensity = 0.3f;

		[SerializeField] bool addRimLight = true;
		[SerializeField] Vector3 rimLightEuler = new Vector3(30f, -160f, 0f);
		[SerializeField] Color rimLightColor = new Color(0.85f, 0.95f, 1.0f);
		[SerializeField] float rimLightIntensity = 0.2f;

		[Header("Fog (RenderSettings)")]
		[SerializeField] bool enableFog = true;
		[SerializeField] FogMode fogMode = FogMode.Exponential;
		[SerializeField] Color fogColor = new Color(0.65f, 0.74f, 0.86f);
		[SerializeField] float fogDensity = 0.012f; // for Exponential
		[SerializeField] float fogStart = 20f;      // for Linear
		[SerializeField] float fogEnd = 120f;       // for Linear

		[Header("Global Volume (URP)")]
		[SerializeField] bool createGlobalVolume = true;
		[SerializeField] bool addBloom = true;
		[SerializeField] float bloomIntensity = 0.08f;
		[SerializeField] float bloomThreshold = 1.1f;
		[SerializeField] float bloomScatter = 0.55f;

		[SerializeField] bool addColorAdjustments = true;
		[SerializeField] float postExposure = 0.0f;
		[SerializeField] float contrast = 10f;
		[SerializeField] float saturation = -5f;
		[SerializeField] Color colorFilter = Color.white;

		[SerializeField] bool addTonemapping = true;
		[SerializeField] TonemappingMode tonemappingMode = TonemappingMode.ACES;

		[SerializeField] bool addVignette = true;
		[SerializeField] float vignetteIntensity = 0.12f;
		[SerializeField] float vignetteSmoothness = 0.4f;

		[SerializeField] bool addSSAO = true;
		[SerializeField] float ssaoIntensity = 0.85f;
		[SerializeField] float ssaoRadius = 0.3f;

		void Start()
		{
			if (applyOnStart) Apply();
		}

		[ContextMenu("Apply Lighting Setup")]
		public void Apply()
		{
			ApplySkyAndAmbient();
			SetupLights();
			SetupFog();
			SetupVolume();
			Debug.Log("[LightingSetup] Applied.");
		}

		void ApplySkyAndAmbient()
		{
			if (skyboxMaterial != null)
			{
				RenderSettings.skybox = skyboxMaterial;
			}
			RenderSettings.ambientMode = AmbientMode.Skybox;
			RenderSettings.ambientIntensity = Mathf.Clamp01(ambientIntensity);
			RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
		}

		void SetupLights()
		{
			if (!configureDirectionalLight) return;

			var key = existingMainLight != null ? existingMainLight : FindOrCreateDirectional("Key Light");
			ConfigureDirectional(key, keyLightEuler, keyLightColor, keyLightIntensity, keyLightShadows, keyShadowResolution);

			if (addFillLight)
			{
				var fill = FindOrCreateDirectional("Fill Light");
				ConfigureDirectional(fill, fillLightEuler, fillLightColor, fillLightIntensity, LightShadows.None, LightShadowResolution.FromQualitySettings);
			}

			if (addRimLight)
			{
				var rim = FindOrCreateDirectional("Rim Light");
				ConfigureDirectional(rim, rimLightEuler, rimLightColor, rimLightIntensity, LightShadows.None, LightShadowResolution.FromQualitySettings);
			}
		}

		Light FindOrCreateDirectional(string name)
		{
			// Prefer an existing directional light if present
			var all = Object.FindObjectsOfType<Light>();
			for (int i = 0; i < all.Length; i++)
			{
				var l = all[i];
				if (l != null && l.type == LightType.Directional && l.name == name) return l;
			}

			var go = new GameObject(name);
			go.transform.SetParent(transform, false);
			var light = go.AddComponent<Light>();
			light.type = LightType.Directional;
			return light;
		}

		void ConfigureDirectional(Light light, Vector3 euler, Color color, float intensity, LightShadows shadows, LightShadowResolution resolution)
		{
			if (light == null) return;
			light.transform.rotation = Quaternion.Euler(euler);
			light.color = color;
			light.intensity = Mathf.Max(0f, intensity);
			light.shadows = shadows;
			light.shadowResolution = resolution;
			light.shadowBias = Mathf.Clamp(shadowBias, 0f, 2f);
			light.shadowNormalBias = Mathf.Clamp(shadowNormalBias, 0f, 3f);
		}

		void SetupFog()
		{
			RenderSettings.fog = enableFog;
			if (!enableFog) return;
			RenderSettings.fogMode = fogMode;
			RenderSettings.fogColor = fogColor;
			switch (fogMode)
			{
				case FogMode.Linear:
					RenderSettings.fogStartDistance = Mathf.Max(0f, fogStart);
					RenderSettings.fogEndDistance = Mathf.Max(RenderSettings.fogStartDistance + 1f, fogEnd);
					break;
				default:
					RenderSettings.fogDensity = Mathf.Max(0f, fogDensity);
					break;
			}
		}

		void SetupVolume()
		{
			if (!createGlobalVolume) return;

			var volumeGo = new GameObject("Global PostProcess Volume");
			volumeGo.transform.SetParent(transform, false);
			var volume = volumeGo.AddComponent<Volume>();
			volume.isGlobal = true;
			volume.priority = 0f;
			var profile = ScriptableObject.CreateInstance<VolumeProfile>();
			volume.sharedProfile = profile;

			if (addBloom)
			{
				var bloom = profile.Add<Bloom>(true);
				bloom.active = true;
				bloom.intensity.overrideState = true;
				bloom.intensity.value = Mathf.Clamp(bloomIntensity, 0f, 1.5f);
				bloom.threshold.overrideState = true;
				bloom.threshold.value = Mathf.Clamp(bloomThreshold, 0f, 5f);
				bloom.scatter.overrideState = true;
				bloom.scatter.value = Mathf.Clamp01(bloomScatter);
			}

			if (addColorAdjustments)
			{
				var ca = profile.Add<ColorAdjustments>(true);
				ca.active = true;
				ca.postExposure.overrideState = true;
				ca.postExposure.value = postExposure;
				ca.contrast.overrideState = true;
				ca.contrast.value = Mathf.Clamp(contrast, -100f, 100f);
				ca.saturation.overrideState = true;
				ca.saturation.value = Mathf.Clamp(saturation, -100f, 100f);
				ca.colorFilter.overrideState = true;
				ca.colorFilter.value = colorFilter;
			}

			if (addTonemapping)
			{
				var tm = profile.Add<Tonemapping>(true);
				tm.active = true;
				tm.mode.overrideState = true;
				tm.mode.value = tonemappingMode;
			}

			if (addVignette)
			{
				var vg = profile.Add<Vignette>(true);
				vg.active = true;
				vg.intensity.overrideState = true;
				vg.intensity.value = Mathf.Clamp01(vignetteIntensity);
				vg.smoothness.overrideState = true;
				vg.smoothness.value = Mathf.Clamp01(vignetteSmoothness);
			}

			if (addSSAO) TryAddSSAO(profile);
		}

		void TryAddSSAO(VolumeProfile profile)
		{
			// Some URP versions expose SSAO type publicly; others keep it internal. Use reflection safely.
			var ssaoType = System.Type.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion, Unity.RenderPipelines.Universal.Runtime");
			if (ssaoType == null) return;

			// Use VolumeProfile.Add(Type,bool) to avoid generic reference to an inaccessible type
			var comp = profile.Add(ssaoType, true);
			if (comp == null) return;

			// Enable component (VolumeComponent.active)
			var activeProp = comp.GetType().GetProperty("active", BindingFlags.Public | BindingFlags.Instance);
			if (activeProp != null) activeProp.SetValue(comp, true);

			// Set parameters if present
			SetFloatParameter(comp, "intensity", Mathf.Clamp01(ssaoIntensity));
			SetFloatParameter(comp, "radius", Mathf.Clamp(ssaoRadius, 0.05f, 1.0f));
		}

		static void SetFloatParameter(object component, string fieldName, float value)
		{
			var field = component.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
			if (field == null) return;
			var param = field.GetValue(component);
			if (param == null) return;
			var valueProp = param.GetType().GetProperty("value", BindingFlags.Public | BindingFlags.Instance);
			var overrideProp = param.GetType().GetProperty("overrideState", BindingFlags.Public | BindingFlags.Instance);
			if (overrideProp != null) overrideProp.SetValue(param, true);
			if (valueProp != null) valueProp.SetValue(param, value);
		}
	}
}


