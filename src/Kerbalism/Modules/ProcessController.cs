﻿using System.Collections.Generic;
using UnityEngine;


namespace KERBALISM
{


	public sealed class ProcessController: PartModule, IModuleInfo, IAnimatedModule, ISpecifics, IConfigurable
	{
		// config
		[KSPField] public string resource = string.Empty; // pseudo-resource to control
		[KSPField] public string title = string.Empty;    // name to show on ui
		[KSPField] public string desc = string.Empty;     // description to show on tooltip
		[KSPField] public double capacity = 1.0;          // amount of associated pseudo-resource
		[KSPField] public bool toggle = true;             // show the enable/disable toggle button

		// persistence/config
		// note: the running state doesn't need to be serialized, as it can be deduced from resource flow
		// but we find useful to set running to true in the cfg node for some processes, and not others
		[KSPField(isPersistant = true)] public bool running = true;		// processes default to on

		// amount of times to multiply capacity, used so configure can have the same process in more than one slot
		[KSPField(isPersistant = true)] public int multiple = 1;

		// index of currently active dump valve
		[KSPField(isPersistant = true)] public int valve_i = 0;

		private DumpSpecs dump_specs;
		private bool broken = false;

		public void Start()
		{
			// don't break tutorial scenarios
			if (Lib.DisableScenario(this))
				return;

			// configure on start, must be executed with enabled true on parts first load.
			Configure(true, multiple);

			// get dump specs for associated process
			dump_specs = Profile.processes.Find(x => x.modifiers.Contains(resource)).dump;

			// set dump valve ui button
			Events["DumpValve"].active = dump_specs.AnyValves;

			// set active dump valve
			dump_specs.ValveIndex = valve_i;
			valve_i = dump_specs.ValveIndex;

			// set action group ui
			Actions["Action"].guiName = Lib.BuildString("Start/Stop ", title);

			// hide toggle if specified
			Events["Toggle"].active = toggle;
			Actions["Action"].active = toggle;

			// deal with non-togglable processes
			if (!toggle)
				running = true;

			// set processes enabled state
			Lib.SetProcessEnabledDisabled(part, resource, broken ? false : running, capacity * multiple);
		}

		///<summary> Called by Configure.cs. Configures the controller to settings passed from the configure module,
		/// the multiple parameter is used to indicate the number of slots used by the process </summary>
		public void Configure(bool enable, int multiple = 1)
		{
			// make sure multiple is not zero
			multiple = multiple == 0 ? 1 : multiple;

			if (enable)
			{
				// if never set
				// - this is the case in the editor, the first time, or in flight
				//   in the case the module was added post-launch, or EVA kerbals
				if (!part.Resources.Contains(resource))
				{
					// add the resource
					// - always add the specified amount, even in flight
					Lib.AddResource(part, resource, (!broken && running) ? capacity * multiple : 0.0, capacity * multiple);
				}
				// has multiple changed
				else if (this.multiple != multiple)
				{
					// multiple has increased
					if (this.multiple < multiple)
					{
						Lib.AddResource(part, resource, (!broken && running) ? capacity * (multiple - this.multiple) : 0.0, capacity * (multiple - this.multiple));
					}
					// multiple has decreased
					else
					{
						Lib.RemoveResource(part, resource, 0.0, capacity * (this.multiple - multiple));
					}
				}
				this.multiple = multiple;
			}
			else
				Lib.RemoveResource(part, resource, 0.0, capacity * this.multiple);
		}

		///<summary> Call this when process controller breaks down or is repaired </summary>
		public void ReliablityEvent(bool breakdown)
		{
			broken = breakdown;
			Lib.SetProcessEnabledDisabled(part, resource, broken ? false : running, capacity * multiple);
		}

		public void Update()
		{
			// update rmb ui
			Events["Toggle"].guiName = Lib.StatusToggle(title, broken ? "broken" : running ? "running" : "stopped");
			Events["DumpValve"].guiName = Lib.StatusToggle("Dump", dump_specs.valves[valve_i]);
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "_", active = true)]
		public void Toggle()
		{
			if (broken)
				return;
			// switch status
			running = !running;
			Lib.SetProcessEnabledDisabled(part, resource, running, capacity * multiple);
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Dump", active = true)]
		public void DumpValve() { valve_i = dump_specs.NextValve; }

		// action groups
		[KSPAction("_")] public void Action(KSPActionParam param) { Toggle(); }


		// part tooltip
		public override string GetInfo()
		{
			return Specs().Info(desc);
		}


		// specifics support
		public Specifics Specs()
		{
			Specifics specs = new Specifics();
			Process process = Profile.processes.Find(k => k.modifiers.Contains(resource));
			if (process != null)
			{
				foreach (KeyValuePair<string, double> pair in process.inputs)
				{
					if (!process.modifiers.Contains(pair.Key))
						specs.Add(pair.Key, Lib.BuildString("<color=#ffaa00>", Lib.HumanReadableRate(pair.Value * capacity), "</color>"));
					else
						specs.Add("Half-life", Lib.HumanReadableDuration(0.5 / pair.Value));
				}
				foreach (KeyValuePair<string, double> pair in process.outputs)
				{
					specs.Add(pair.Key, Lib.BuildString("<color=#00ff00>", Lib.HumanReadableRate(pair.Value * capacity), "</color>"));
				}
			}
			return specs;
		}

		// module info support
		public string GetModuleTitle() { return Lib.BuildString("<size=1><color=#00000000>01</color></size>", title); }  // Display after config widget
		public override string GetModuleDisplayName() { return Lib.BuildString("<size=1><color=#00000000>01</color></size>", title); }  // Display after config widget
		public string GetPrimaryField() { return string.Empty; }
		public Callback<Rect> GetDrawModulePanelCallback() { return null; }


		// animation group support
		public void EnableModule() { }
		public void DisableModule() { }
		public bool ModuleIsActive() { return broken ? false : running; }
		public bool IsSituationValid() { return true; }
	}


} // KERBALISM

