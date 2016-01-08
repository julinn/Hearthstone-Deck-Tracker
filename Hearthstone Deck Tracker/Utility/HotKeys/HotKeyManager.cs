﻿#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

#endregion

namespace Hearthstone_Deck_Tracker.Utility.HotKeys
{
	public class HotKeyManager
	{
		private static readonly KeyboardHook KeyboardHook = new KeyboardHook();
		private static readonly Dictionary<HotKey, int> HotKeyIds = new Dictionary<HotKey, int>();
		private static readonly Dictionary<HotKey, Action> RegisteredHotKeys = new Dictionary<HotKey, Action>();

		public static ObservableCollection<KeyValuePair<HotKey, string>> RegisteredHotKeysInfo { get; } = new ObservableCollection<KeyValuePair<HotKey, string>>();

		static HotKeyManager()
		{
			KeyboardHook.KeyPressed += KeyboardHookOnKeyPressed;
		}

		public static bool RegisterHotkey(HotKey hotKey, Action action, string name)
		{
			if(RegisteredHotKeys.ContainsKey(hotKey))
			{
				Logger.WriteLine($"HotKey {hotKey} already registered.", "HotKeyManager");
				return false;
			}
			try
			{
				var id = KeyboardHook.RegisterHotKey(hotKey.Mod, hotKey.Key);
				HotKeyIds.Add(hotKey, id);
				RegisteredHotKeys.Add(hotKey, action);

				var predefined = PredefinedHotKeyActions.PredefinedActionNames.FirstOrDefault(x => x.MethodName == name);
				var title = predefined != null ? predefined.Title : name;
				RegisteredHotKeysInfo.Add(new KeyValuePair<HotKey, string>(hotKey, title));
				return true;
			}
			catch(Exception ex)
			{
				Logger.WriteLine(ex.ToString(), "HotKeyManager");
				return false;
			}
		}

		private static void KeyboardHookOnKeyPressed(object sender, KeyPressedEventArgs e)
		{
			Action action;
			if(RegisteredHotKeys.TryGetValue(HotKey.FromKeyPressedEventArgs(e), out action))
				action.Invoke();
		}

		public static void Load()
		{
			foreach(var item in HotKeyConfig.Instance.HotKeys)
				LoadPredefinedHotkeyAction(item.HotKey, item.Action);
		}

		public static bool AddPredefinedHotkey(HotKey hotKey, string actionName)
		{
			if(LoadPredefinedHotkeyAction(hotKey, actionName))
			{
				HotKeyConfig.Instance.AddHotKey(hotKey, actionName);
				return true;
			}
			return false;
		}

		public static void RemovePredefinedHotkey(HotKey hotKey)
		{
			HotKeyConfig.Instance.RemoveHotKey(hotKey);
			if(RegisteredHotKeys.ContainsKey(hotKey))
				RegisteredHotKeys.Remove(hotKey);
			var info = RegisteredHotKeysInfo.FirstOrDefault(x => x.Key.Equals(hotKey));
			if(!info.Equals(default(KeyValuePair<HotKey, string>)))
				RegisteredHotKeysInfo.Remove(info);
			try
			{
				KeyboardHook.UnRegisterHotKey(HotKeyIds[hotKey]);
				HotKeyIds.Remove(hotKey);
			}
			catch(Exception ex)
			{
				Logger.WriteLine("Error removing hotkey: " + ex, "HotKeyManager");
			}
		}

		private static bool LoadPredefinedHotkeyAction(HotKey hotKey, string actionName)
		{
			var action = typeof(PredefinedHotKeyActions).GetMethods().FirstOrDefault(x => x.Name == actionName);
			if(action != null)
				return RegisterHotkey(hotKey, () => action.Invoke(null, null), actionName);
			Logger.WriteLine($"Could not find predefined action \"{actionName}\"", "HotKeyManager");
			HotKeyConfig.Instance.RemoveHotKey(hotKey);
			return false;
		}
	}
}