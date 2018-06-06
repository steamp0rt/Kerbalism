﻿using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;

namespace KERBALISM
{


	public static class Communications
	{
		// default transmission rate, strength and cost
		private const double ext_rate = 0.064;        // 64 KB/s
		private const double ext_strength = 1.0;      // 100 %
		private const double ext_cost = 0.05;         // 50 W/s

		public static bool NetworkInitialized = false;
		private static ConnectionInfo vessel_connection = null;

		public static void Update(Vessel v, Vessel_info vi, VesselData vd, double elapsed_s)
		{
			// do nothing if signal mechanic is disabled or CommNet is not ready
			if ((!HighLogic.fetch.currentGame.Parameters.Difficulty.EnableCommNet && !RemoteTech.Enabled()) || !NetworkInitialized)
				return;

			// get connection info
			vessel_connection = vi.connection;

			// maintain and send messages
			// - do not send messages for vessels without an antenna
			// - do not send messages during/after solar storms
			// - do not send messages for EVA kerbals
			if (vessel_connection.status != LinkStatus.no_antenna && !v.isEVA && v.situation != Vessel.Situations.PRELAUNCH)
			{
				if (!vd.msg_signal && !vessel_connection.linked)
				{
					vd.msg_signal = true;
					if (vd.cfg_signal && vessel_connection.status != LinkStatus.blackout)
					{
						string subtext = "Data transmission disabled";
						if (vi.crew_count == 0)
						{
							switch (Settings.UnlinkedControl)
							{
								case UnlinkedCtrl.none:
									subtext = Localizer.Format("#KERBALISM_UI_noctrl");
									break;
								case UnlinkedCtrl.limited:
									subtext = Localizer.Format("#KERBALISM_UI_limitedcontrol");
									break;
							}
						}
						Message.Post(Severity.warning, Lib.BuildString(Localizer.Format("#KERBALISM_UI_signallost"), " <b>", v.vesselName, "</b>"), subtext);
					}
				}
				else if (vd.msg_signal && vessel_connection.linked)
				{
					vd.msg_signal = false;
					if (vd.cfg_signal && !Storm.JustEnded(v, elapsed_s))
					{
						var path = vessel_connection.path;
						Message.Post(Severity.relax, Lib.BuildString("<b>", v.vesselName, "</b> ", Localizer.Format("#KERBALISM_UI_signalback")),
						  path.Count == 0 ? Localizer.Format("#KERBALISM_UI_directlink") : Lib.BuildString(Localizer.Format("#KERBALISM_UI_relayby"), " <b>", path[path.Count - 1].vesselName, "</b>"));
					}
				}
			}
		}

		public static ConnectionInfo Connection(Vessel v)
		{
			// if RemoteTech is present and enabled
			if (RemoteTech.Enabled())
			{
				if (RemoteTech.Connected(v.id) && !RemoteTech.ConnectedToKSC(v.id))
				{
					return new ConnectionInfo(LinkStatus.indirect_link, ext_rate, ext_strength, ext_cost);
				}
				else if (RemoteTech.ConnectedToKSC(v.id))
				{
					return new ConnectionInfo(LinkStatus.direct_link, ext_rate, ext_strength, ext_cost);
				}
				else
				{
					return new ConnectionInfo(LinkStatus.no_link);
				}
			}
			// if CommNet is enabled and ready
			else if (HighLogic.fetch.currentGame.Parameters.Difficulty.EnableCommNet && NetworkInitialized)
			{
				return v.connection != null && v.connection.IsConnected
				  ? new ConnectionInfo(LinkStatus.direct_link, ext_rate * v.connection.SignalStrength, v.connection.SignalStrength, ext_cost)
				  : new ConnectionInfo(LinkStatus.no_link);
			}
			// the simple stupid signal system
			else
			{
				return new ConnectionInfo(LinkStatus.direct_link, ext_rate, ext_strength, ext_cost);
			}
		}
	}


} // KERBALISM

