﻿using System;
using System.Reflection;

namespace KERBALISM
{
	public static class RemoteTech
	{
		static RemoteTech()
		{
			foreach (var a in AssemblyLoader.loadedAssemblies)
			{
				if (a.name == "RemoteTech")
				{
					API = a.assembly.GetType("RemoteTech.API.API");
					IsEnabled = API.GetMethod("IsRemoteTechEnabled");
					EnabledInSPC = API.GetMethod("EnableInSPC");
					IsConnected = API.GetMethod("HasAnyConnection");
					IsConnectedKSC = API.GetMethod("HasConnectionToKSC");
					IsTargetKSC = API.GetMethod("HasGroundStationTarget");
					SignalDelay = API.GetMethod("GetSignalDelayToKSC");
					SetRadioBlackout = API.GetMethod("SetRadioBlackoutGuid");
					GetRadioBlackout = API.GetMethod("GetRadioBlackoutGuid");
					SetPowerDown = API.GetMethod("SetPowerDownGuid");
					GetPowerDown = API.GetMethod("GetPowerDownGuid");
					break;
				}
			}
		}

		// return true if RemoteTech is enabled for the current game
		public static bool Enabled
		{
			get { return API != null && (bool)IsEnabled.Invoke(null, new Object[] { }); }
		}

		public static void EnableInSPC()
		{
			if (API != null && EnabledInSPC != null)
				EnabledInSPC.Invoke(null, new Object[] { true });
		}

		public static bool ConnectedToKSC(Guid id)
		{
			return API != null && (bool)IsConnectedKSC.Invoke(null, new Object[] { id });
		}

		public static bool TargetsKSC(Guid id)
		{
			return API != null && (bool)IsTargetKSC.Invoke(null, new Object[] { id });
		}

		// return true if the vessel is connected according to RemoteTech
		public static bool Connected(Guid id)
		{
			return API != null && (bool)IsConnected.Invoke(null, new Object[] { id });
		}

		public static double GetSignalDelay(Guid id)
		{
			return (API != null ? (double)SignalDelay.Invoke(null, new Object[] { id }) : 0);
		}

		public static Object SetCommsBlackout(Guid id, bool flag, string origin)
		{
			if (API != null && SetRadioBlackout != null)
				return SetRadioBlackout.Invoke(null, new Object[] { id, flag, origin });
			return null;
		}

		public static bool GetCommsBlackout(Guid id)
		{
			return API != null && GetRadioBlackout != null && (bool)GetRadioBlackout.Invoke(null, new Object[] { id });
		}

		public static Object SetPoweredDown(Guid id, bool flag, string origin)
		{
			if (API != null && SetPowerDown != null)
				return SetPowerDown.Invoke(null, new Object[] { id, flag, origin });
			return SetCommsBlackout(id, flag, origin);  // Workaround for earlier versions of RT
		}

		public static bool IsPoweredDown(Guid id)
		{
			return API != null && GetPowerDown != null && (bool)GetPowerDown.Invoke(null, new Object[] { id });
		}

		public static void SetBroken(PartModule antenna, bool broken)
		{
			Lib.ReflectionValue(antenna, "IsRTBroken", broken);
		}

		public static bool IsAntenna(PartModule m)
		{
			// we test for moduleName, but could use the boolean IsRTAntenna here
			return (m.moduleName == "ModuleRTAntenna" || m.moduleName == "ModuleRTAntennaPassive");
		}

		static Type API;
		static MethodInfo IsEnabled;
		static MethodInfo EnabledInSPC;
		static MethodInfo IsConnected;
		static MethodInfo IsConnectedKSC;
		static MethodInfo IsTargetKSC;
		static MethodInfo SignalDelay;
		static MethodInfo SetRadioBlackout;
		static MethodInfo GetRadioBlackout;
		static MethodInfo SetPowerDown;
		static MethodInfo GetPowerDown;
	}


} // KERBALISM

