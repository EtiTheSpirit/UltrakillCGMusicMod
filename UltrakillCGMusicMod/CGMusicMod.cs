using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Reflection;
using UnityEngine.Events;

namespace UltrakillCGMusicMod {

	/// <summary>
	/// Here it is, the main mod class.
	/// </summary>
	public class CGMusicMod : MelonMod {

		#region Initialization

		/// <summary>
		/// The text for the instructions document.
		/// </summary>
		private const string INSTRUCTIONS_TXT = @"Instructions:
Step 1: Create a new folder in this folder (you know, the one containing this instructions file). It can be named anything you want, I would go with the title of the song personally, but whatever floats your boat.
Step 2: In your new folder, you need to place .wav files for your music. This can be one of three things:
#1: ""intro.wav"" - This file, if defined, will override the intro track which plays right when you jump into the arena.
#2: ""loop.wav"" - This file, if defined, will override the loop track, which plays throughout the duration of your run.
#3: ""outro.wav"" - This file, if defined, will override the ending track, which plays after you die.

Extra:
- Having multiple folders in the Music directory will make the mod pick a random song folder and use that, picking a new one per run.
- You can add a folder named ""Default"" inside of the Music directory to allow default cybergrind music to be part of the selection. THIS FOLDER MUST BE EMPTY, AND FILES INSIDE WILL BE IGNORED.
- All files (intro, loop, and outro) are optional, **BUT THEY DEFAULT TO THE NORMAL TRACKS FOR CYBERGRIND.** If you want nothing to play, you need to make an empty audio file yourself.
- You can remove a folder from the selection by creating a new text document named ""disable"" (n.b. for users that show file extensions, it *should* have .txt, average joe tends to hide extensions).

IMPORTANT INFORMATION:
- Adding new music folders requires a game restart! This might be changed in the future, I don't know.
- Adding or removing disable.txt to a folder does NOT require a restart.

(PRESUMABLY) COMMON STUFFS:
Q: ""I just want to replace the main music. How do I do that?""
A: In *most* cases, the best option is to create intro.wav and loop.wav (make them the same exact file, just copied) and have no outro.wav

Q: ""Our table! It's broken!""
A: If you found an issue or bug, my contact info is on my website @ https://etithespir.it (preferrably use Discord).";

		/// <summary>
		/// A lookup of all known audio folders.
		/// </summary>
		private static AudioFolderRepresentation[] AudioFolders;

		public override void OnApplicationStart() {
			MelonLogger.Msg("Loading up all of your music...");
			DirectoryInfo gameDir = new DirectoryInfo(Application.dataPath).Parent;
			DirectoryInfo musicDir = gameDir.CreateSubdirectory("Cybergrind").CreateSubdirectory("Music");
			string instructionsTXT = Path.Combine(musicDir.FullName, "instructions.txt");
			if (!File.Exists(instructionsTXT)) {
				File.WriteAllText(instructionsTXT, INSTRUCTIONS_TXT);
				MelonLogger.Warning("You seem to be new to this mod (or deleted the instructions)! A new directory has been created in the game's Cybergrind folder (the same place where patterns and textures go) named \"Music\". Find instructions.txt in that folder and read it!");
			}

			DirectoryInfo[] songFolders = musicDir.GetDirectories();
			AudioFolders = new AudioFolderRepresentation[songFolders.Length];
			for (int index = 0; index < songFolders.Length; index++) {
				DirectoryInfo dir = songFolders[index];
				MelonLogger.Msg($"Loading {dir.Name}...");
				if (dir.Name.ToLower() == "default") {
					AudioFolders[index] = new AudioFolderRepresentation();
				} else {
					AudioFolders[index] = new AudioFolderRepresentation(dir);
				}
			}

			MelonLogger.Msg("Done loading the music!");
		}

		#endregion

		#region Main Stuffs

		/// <summary>
		/// Access Ultrakill's Music Changer as part of the scene hierarchy in The Cybergrind.
		/// It will attempt to look in a known path, but if that fails, it will search the entire hierarchy.
		/// </summary>
		/// <param name="buildIndex"></param>
		/// <returns></returns>
		public MusicChanger GetMusicChanger(in Scene scene) {
			// Try the known path.
			GameObject everything = scene.GetRootGameObjects().FirstOrDefault(obj => obj.name == "Everything");
			if (everything != null) {
				GameObject ctr = everything.FindFirstChild("Timer").FindFirstChild("Intro").FindFirstChild("MusicChanger");
				if (ctr != null) {
					// woo
					return ctr.GetComponent<MusicChanger>();
				}
			}

			// SCHET
			MelonLogger.Warning("Failed to find MusicChanger in a known path for the version this mod was developed for. Attempting fallback (this might lag)...");
			foreach (GameObject obj in scene.GetRootGameObjects()) {
				MusicChanger changer = obj.GetComponentInChildren<MusicChanger>();
				if (changer != null) return changer;
			}

			return null;
		}

		/// <summary>
		/// Sets all of the song files in the current wave to the desired music.
		/// </summary>
		/// <param name="introSrc"></param>
		/// <param name="outroSrc"></param>
		/// <param name="loopSrcCtr"></param>
		public void UpdateToRandomSong(AudioSource introSrc, AudioSource outroSrc, MusicChanger loopSrcCtr) {
			AudioFolderRepresentation[] options = AudioFolders.Where(folder => folder.AllowedToLoad).ToArray();
			if (options.Length == 0) {
				// This is still possible! Default doesn't have to exist, and if it doesn't, then this may very well be zero.
				// In this case, just do nothing.
				MelonLogger.Msg("Loaded nothing; there are no songs available.");
				return;
			}

			AudioFolderRepresentation selected = options.Random();
			MelonLogger.Msg("Selected track: " + selected.Name);
			if (introSrc != null) {
				introSrc.clip = selected.Intro;
			}
			loopSrcCtr.battle = selected.Loop;
			loopSrcCtr.boss = selected.Loop;
			loopSrcCtr.clean = selected.Loop;
			if (outroSrc != null) {
				outroSrc.clip = selected.Outro;
			}
		}

		#endregion

		#region Melonloader Hooks

		/// <inheritdoc/>
		public override void OnSceneWasLoaded(int buildIndex, string sceneName) {
			MelonLogger.Msg($"A scene was loaded. (Scene #{buildIndex}: {sceneName})");
			if (sceneName == "Endless") {
				MelonLogger.Msg("...which is the Cybergrind level. Loading music up!");
				Scene scene = SceneManager.GetSceneByName(sceneName);
				// name will be "Endless" in this case.
				MusicChanger changer = GetMusicChanger(scene);

				if (changer != null) {
					AudioSource introSrc = changer.transform.parent.GetComponent<AudioSource>();
					if (introSrc == null) {
						MelonLogger.Warning("Failed to find an instance of AudioSource on Intro - intro tracks won't work!");
					}

					GameObject endCtr = scene.GetRootGameObjects().FirstOrDefault(obj => obj.name == "EndMusic");
					AudioSource outroSrc = endCtr.GetComponent<AudioSource>();

					if (outroSrc == null) {
						MelonLogger.Error("Failed to find an instance of AudioSource on EndMusic! This is REQUIRED to figure out when to switch to a new track, so the mod will not work at all!");
						return;
					}

					// Late-populate the defaults.
					if (!AudioFolderRepresentation.HasDefaults) {
						AudioFolderRepresentation.HasDefaults = true;
						MelonLogger.Msg("Loading defaults into the audio tracker's memory...");
						AudioFolderRepresentation.DefaultIntro = introSrc?.clip;
						AudioFolderRepresentation.DefaultLoop = changer.battle ?? changer.boss ?? changer.clean; 
						// No clue which is which and they always get set to the same thing it seems.
						// If you, the reader, know how this works, please let me know.

						AudioFolderRepresentation.DefaultOutro = outroSrc.clip;
						MelonLogger.Msg($"Done loading defaults:\n> Intro: {AudioFolderRepresentation.DefaultIntro}\n> Loop: {AudioFolderRepresentation.DefaultLoop}\n> Outro: {AudioFolderRepresentation.DefaultOutro}");
					}

					UpdateToRandomSong(introSrc, outroSrc, changer);

					// For the initial loadup of the CG gamemode, create a new dummy GameObject with one of Ultrakill's built in
					// event handlers to execute code when a sound ends.
					// This will be used to detect when the ending song stops so that a new trackset is picked after restarting.

					GameObject secondaryEnd = new GameObject();
					secondaryEnd.transform.SetParent(endCtr.transform);
					ActivateOnSoundEnd evt = secondaryEnd.AddComponent<ActivateOnSoundEnd>();
					UltrakillEvent onEndedEvent = new UltrakillEvent();
					onEndedEvent.onActivate = new UnityEvent();
					onEndedEvent.onActivate.AddListener(() => {
						UpdateToRandomSong(introSrc, outroSrc, changer);
					});
					FieldInfo eventsField = typeof(ActivateOnSoundEnd).GetField("events", BindingFlags.Instance | BindingFlags.NonPublic);
					eventsField.SetValue(evt, onEndedEvent);

				} else {
					MelonLogger.Error("Failed to find music changer. The mod cannot function.");
				}
			}
		}

		#endregion

	}
}
