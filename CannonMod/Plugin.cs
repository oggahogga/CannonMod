using BepInEx;
using CannonMod;
using GorillaLocomotion.Gameplay;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;
using UnityEngine.XR;
using Utilla;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace CannonMod
{
    /// <summary>
    /// This is your mod's main class.
    /// </summary>

    /* This attribute tells Utilla to look for [ModdedGameJoin] and [ModdedGameLeave] */
    [ModdedGamemode]
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [BepInPlugin(CannonMod.PluginInfo.GUID, CannonMod.PluginInfo.Name, CannonMod.PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        bool inRoom = false;

        public AssetBundle LoadAssetBundle(string path)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            AssetBundle bundle = AssetBundle.LoadFromStream(stream);
            stream.Close();
            return bundle;
        }

        void Start()
        {
            Utilla.Events.GameInitialized += OnGameInitialized;
        }

        void OnEnable()
        {
            /* Set up your mod here */
            /* Code here runs at the start and whenever your mod is enabled*/
            isEnabled = true;
            HarmonyPatches.ApplyHarmonyPatches();
        }

        void OnDisable()
        {
            /* Undo mod setup here */
            /* This provides support for toggling mods with ComputerInterface, please implement it :) */
            /* Code here runs whenever your mod is disabled (including if it disabled on startup)*/
            isEnabled = false;
            HarmonyPatches.RemoveHarmonyPatches();
        }

        GameObject MainAsset;

        GameObject CannonBall;

        GameObject trail;

        bool isEnabled = true;

        static bool isGripPressed = false;

        void OnGameInitialized(object sender, EventArgs e)
        {
            AssetBundle bundle = LoadAssetBundle("CannonMod.Assets.cannon");
            GameObject asset = bundle.LoadAsset<GameObject>("cannon");
            GameObject cannonBall = bundle.LoadAsset<GameObject>("cannonball");
            MainAsset = Instantiate(asset);
            MainAsset.transform.position = GorillaLocomotion.Player.Instance.rightControllerTransform.position;
            CannonBall = Instantiate(cannonBall);
            CannonBall.transform.position = GorillaLocomotion.Player.Instance.rightControllerTransform.position;
        }

        bool wasTriggerPressed = false;
        bool wasGripAndTriggerPressed = false;

        void Update()
        {
            try
            {
                if (inRoom && isEnabled)
                {
                    MainAsset.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    MainAsset.SetActive(true);

                    CannonBall.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    CannonBall.SetActive(true);

                    Transform trail = CannonBall.transform.Find("trail");
                    ParticleSystem trailParticles = trail.gameObject.GetComponent<ParticleSystem>();
                    trail.localScale = new Vector3(0.2f, 0.2f, 0.2f);

                    Transform audio = CannonBall.transform.Find("cannonsound");
                    AudioSource audioSource = audio.gameObject.GetComponent<AudioSource>();
                    audioSource.loop = false;
                    audioSource.playOnAwake = false;

                    InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.triggerButton, out bool isTriggerPressed);
                    InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.gripButton, out bool isGripPressed);

                    if(isTriggerPressed)
                    {
                        Vector3 directionToPlayer = GorillaTagger.Instance.offlineVRRig.transform.position - MainAsset.transform.position;
                        MainAsset.transform.rotation = Quaternion.LookRotation(-directionToPlayer) * Quaternion.Euler(0, 90, 0);
                        CannonBall.transform.LookAt(GorillaTagger.Instance.offlineVRRig.transform.position);
                        CannonBall.transform.rotation = CannonBall.transform.rotation * Quaternion.Euler(0, -90, 0);
                    }

                    if (!isTriggerPressed)
                    {
                        MainAsset.transform.position = GorillaTagger.Instance.offlineVRRig.transform.position + GorillaTagger.Instance.offlineVRRig.transform.forward;
                        CannonBall.transform.position = MainAsset.transform.position;
                        MainAsset.transform.rotation = GorillaTagger.Instance.offlineVRRig.transform.rotation * Quaternion.Euler(0, 90, 0);
                    }

                    if (isTriggerPressed && !wasTriggerPressed)
                    {
                        trailParticles.Play();
                    }
                    wasTriggerPressed = isTriggerPressed;

                    if (isTriggerPressed && isGripPressed && !wasGripAndTriggerPressed)
                    {
                        if (CannonBall.GetComponent<Rigidbody>() == null)
                        {
                            CannonBall.AddComponent<Rigidbody>();
                        }
                        Rigidbody rb = CannonBall.GetComponent<Rigidbody>();
                        rb.AddExplosionForce(3000, GorillaTagger.Instance.offlineVRRig.transform.position, 100);
                        audioSource.Play();
                    }
                    wasGripAndTriggerPressed = isTriggerPressed && isGripPressed;

                    if (!isTriggerPressed && !isGripPressed)
                    {
                        trailParticles.Stop();
                        if (CannonBall.GetComponent<Rigidbody>() != null)
                        {
                            GameObject.Destroy(CannonBall.GetComponent<Rigidbody>());
                        }
                    }
                }
                else
                {
                    MainAsset.SetActive(false);
                    CannonBall.SetActive(false);
                }
            }
            catch (Exception e)
            {

            }
        }

        GameObject pointer = null;

        /* This attribute tells Utilla to call this method when a modded room is joined */
        [ModdedGamemodeJoin]
        public void OnJoin(string gamemode)
        {
            /* Activate your mod here */
            /* This code will run regardless of if the mod is enabled*/
            inRoom = true;
        }

        /* This attribute tells Utilla to call this method when a modded room is left */
        [ModdedGamemodeLeave]
        public void OnLeave(string gamemode)
        {
            /* Deactivate your mod here */
            /* This code will run regardless of if the mod is enabled*/

            inRoom = false;
        }
    }
}
