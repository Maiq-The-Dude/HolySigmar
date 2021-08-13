using BepInEx;
using FistVR;
using Sodalite.Api;
using System;
using UnityEngine;

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

		private void FlintlockWeapon_Awake(On.FistVR.FlintlockWeapon.orig_Awake orig, FistVR.FlintlockWeapon self)
		{
			orig(self);

			self.SpawnLockable = true;
		}

		private GameObject FVRPhysicalObject_DuplicateFromSpawnLock(On.FistVR.FVRPhysicalObject.orig_DuplicateFromSpawnLock orig, FistVR.FVRPhysicalObject self, FistVR.FVRViveHand hand)
		{
			var dupedGun = orig(self, hand);

			if (self is FlintlockWeapon flintWepOG)
			{
				_leaderboardLock ??= LeaderboardAPI.GetLeaderboardDisableLock();

				var flintWepDuped = dupedGun.GetComponent<FlintlockWeapon>();

				// Flint
				flintWepDuped.m_hasFlint = flintWepOG.m_hasFlint;
				flintWepDuped.m_flintUses = flintWepOG.m_flintUses;
				flintWepDuped.FState = flintWepOG.FState;

				// Hammer
				if (flintWepOG.HammerState == FlintlockWeapon.HState.Fullcock)
				{
					flintWepDuped.MoveToFullCock();
				}
				else if (flintWepOG.HammerState == FlintlockWeapon.HState.Halfcock)
				{
					flintWepDuped.MoveToHalfCock();
				}

				// Barrel
				var barrelDuped = flintWepDuped.MuzzlePos.parent.GetComponent<FlintlockBarrel>();
				var barrelOG = flintWepOG.MuzzlePos.parent.GetComponent<FlintlockBarrel>();

				foreach (var loadedElementes in barrelOG.LoadedElements)
				{
					barrelDuped.LoadedElements.Add(loadedElementes);
				}

				// Screw/Holder
				flintWepDuped.FlintlockScrew.SState = flintWepOG.FlintlockScrew.SState;
				flintWepDuped.FlintlockHolder.FlintPrefab.GetComponent<FlintlockFlint>().m_flintUses = flintWepOG.FlintlockHolder.FlintPrefab.GetComponent<FlintlockFlint>().m_flintUses;

				// FlashPans
				for (var i = 0; i < flintWepDuped.FlashPans.Count; i++)
				{
					var flashDuped = flintWepDuped.FlashPans[i];
					var flashOG = flintWepOG.FlashPans[i];

					for (var j = 0; j < flashOG.numGrainsPowderOn; j++)
					{
						flashDuped.AddGrain();
					}

					flashDuped.FrizenState = flashOG.FrizenState;
					flashDuped.Frizen.transform.rotation = flashOG.Frizen.transform.rotation;
				}
			}

			return dupedGun;
		}
	}
}