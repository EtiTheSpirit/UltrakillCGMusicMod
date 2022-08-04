using MelonLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace UltrakillCGMusicMod {

	/// <summary>
	/// An analogue to a user-defined Audio folder.
	/// </summary>
	public class AudioFolderRepresentation {

		public static bool HasDefaults = false;

		/// <summary>Ultrakill's default intro music for Cybergrind.</summary>
		public static AudioClip DefaultIntro = null;

		/// <summary>Ultrakill's default loop music for Cybergrind.</summary>
		public static AudioClip DefaultLoop = null;

		/// <summary>Ultrakill's default end music for Cybergrind.</summary>
		public static AudioClip DefaultOutro = null;

		/// <summary>
		/// The folder encompassing this representation. This will be <see langword="null"/> for default music.
		/// </summary>
		public DirectoryInfo Container { get; }

		/// <summary>
		/// The name of the folder representing this container, or "Default Cybergrind Music" for the default folder.
		/// </summary>
		public string Name => Container?.Name ?? "Default Cybergrind Music";

		/// <summary>
		/// A refernce to the custom Intro song, or the default intro if unspecified.
		/// </summary>
		public AudioClip Intro => _intro ?? DefaultIntro;
		private AudioClip _intro = null;

		/// <summary>
		/// A refernce to the custom Loop song, or the default loop if unspecified.
		/// </summary>
		public AudioClip Loop => _loop ?? DefaultLoop;
		private AudioClip _loop = null;

		/// <summary>
		/// A refernce to the custom Ending song, or the default ending if unspecified.
		/// </summary>
		public AudioClip Outro => _outro ?? DefaultOutro;
		private AudioClip _outro = null;

		/// <summary>
		/// Returns whether or not this folder is allowed to be selected randomly based on whether or not it has been deleted and if disable.txt exists within it.
		/// </summary>
		public bool AllowedToLoad {
			get {
				if (Container == null) {
					// default. Always true
					return true;
				}

				// Otherwise, return true iff:
				// 1: The container exists (was not deleted)
				// 2: disable.txt does NOT exist in the container.
				return Container.Exists && !File.Exists(Path.Combine(Container.FullName, "disable.txt"));
			}
		}

		/// <summary>
		/// Create a new default audio folder.
		/// </summary>
		public AudioFolderRepresentation() {
			Container = null;
		}

		/// <summary>
		/// Wrap the given directory in this class.
		/// </summary>
		/// <param name="container"></param>
		public AudioFolderRepresentation(DirectoryInfo container) {
			string full = container.FullName;
			FileInfo intro = new FileInfo(Path.Combine(full, "intro.wav"));
			FileInfo loop = new FileInfo(Path.Combine(full, "loop.wav"));
			FileInfo outro = new FileInfo(Path.Combine(full, "outro.wav"));

			if (intro.Exists) {
				_intro = LoadFromWav(intro);
			}
			if (loop.Exists) {
				_loop = LoadFromWav(loop);
			}
			if (outro.Exists) {
				_outro = LoadFromWav(outro);
			}

			Container = container;
		}

		/// <summary>
		/// Dynamically loads and instantiates an AudioClip instance from a wav file.
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		// Sourced from Unity Forums, which will cease to exist soon. :(
		private AudioClip LoadFromWav(FileInfo file) {
			AudioClip clip = null;
			try {
				using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(file.FullName, AudioType.WAV)) {
					uwr.SendWebRequest();

					// wrap tasks in try/catch, otherwise it'll fail silently
					try {
						while (!uwr.isDone) Task.Delay(10).Wait();

						if (uwr.isNetworkError || uwr.isHttpError) {
							MelonLogger.Error($"{uwr.error}");
						} else {
							clip = DownloadHandlerAudioClip.GetContent(uwr);
						}
					} catch (Exception err) {
						MelonLogger.Error($"{err.Message}, {err.StackTrace}");
					}
				}
			} catch (Exception err) {
				MelonLogger.Error($"{err.Message}, {err.StackTrace}");
			}

			return clip;
		}

	}
}
