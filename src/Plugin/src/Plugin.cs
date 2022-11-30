using BepInEx;
using FistVR;
using Sodalite.Api;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sigmar
{
	[BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
	[BepInDependency(PluginInfo.DEP)]
	[BepInProcess("h3vr.exe")]
	public class Plugin : BaseUnityPlugin
	{
		private IDisposable _leaderboardLock;

		public void Awake()
		{
			Hook();
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		private void OnDestroy()
		{
			Unhook();

			_leaderboardLock?.Dispose();
			_leaderboardLock = null;
		}

		public void Hook()
		{
			On.FistVR.FlintlockWeapon.Awake += FlintlockWeapon_Awake;
			On.FistVR.FVRPhysicalObject.DuplicateFromSpawnLock += FVRPhysicalObject_DuplicateFromSpawnLock;
		}

		public void Unhook()
		{
			On.FistVR.FlintlockWeapon.Awake -= FlintlockWeapon_Awake;
			On.FistVR.FVRPhysicalObject.DuplicateFromSpawnLock -= FVRPhysicalObject_DuplicateFromSpawnLock;
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			if (_leaderboardLock != null)
			{
				Logger.LogDebug("Saltzpyre is no longer preventing TNH score submission");
				_leaderboardLock?.Dispose();
				_leaderboardLock = null;
			}
		}

		private void FlintlockWeapon_Awake(On.FistVR.FlintlockWeapon.orig_Awake orig, FistVR.FlintlockWeapon self)
		{
			orig(self);

			self.SpawnLockable = true;
		}

		private GameObject FVRPhysicalObject_DuplicateFromSpawnLock(On.FistVR.FVRPhysicalObject.orig_DuplicateFromSpawnLock orig, FistVR.FVRPhysicalObject self, FistVR.FVRViveHand hand)
		{
			var dupedItem = orig(self, hand);

			if (self is FlintlockWeapon flintWepOG)
			{
				// Disable scoring
				if (_leaderboardLock == null)
				{
					Logger.LogDebug("Saltzpyre disables TNH score submission");
					_leaderboardLock ??= LeaderboardAPI.LeaderboardDisabled.TakeLock();
				}

				var flintWepDuped = dupedItem.GetComponent<FlintlockWeapon>();

				// Flint
				flintWepDuped.AddFlint(flintWepOG.m_flintUses);
				flintWepDuped.SetFlintState(flintWepOG.FState);

				// Hammer
				if (flintWepOG.HammerState == FlintlockWeapon.HState.Fullcock)
				{
					flintWepDuped.MoveToFullCock();
				}
				else if (flintWepOG.HammerState == FlintlockWeapon.HState.Halfcock)
				{
					flintWepDuped.MoveToHalfCock();
				}

				// Screw/Holder
				flintWepDuped.FlintlockScrew.SState = flintWepOG.FlintlockScrew.SState;
				flintWepDuped.FlintlockHolder.FlintPrefab.GetComponent<FlintlockFlint>().m_flintUses = flintWepOG.FlintlockHolder.FlintPrefab.GetComponent<FlintlockFlint>().m_flintUses;

				// FlashPans
				for (var i = 0; i < flintWepDuped.FlashPans.Count; i++)
				{
					var flashDuped = flintWepDuped.FlashPans[i];
					var flashOG = flintWepOG.FlashPans[i];

					// Grain
					for (var j = 0; j < flashOG.numGrainsPowderOn; j++)
					{
						flashDuped.AddGrain();
					}

					// Barrels
					for (var j = 0; j < flashDuped.Barrels.Count; j++)
					{
						flashDuped.Barrels[j].LoadedElements.AddRange(flashOG.Barrels[j].LoadedElements);
					}

					// Frizen
					if (flashOG.FrizenState == FlintlockFlashPan.FState.Down)
					{
						flashDuped.SetFrizenDown();
					}
					else if (flashOG.FrizenState == FlintlockFlashPan.FState.Up)
					{
						flashDuped.SetFrizenUp();
					}
				}
			}

			return dupedItem;
		}
	}
}