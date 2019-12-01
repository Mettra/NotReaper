﻿using System;
using System.Collections;
using System.IO;
using System.Text;
using Ionic.Zip;
using Melanchall.DryWetMidi.Smf;
using Melanchall.DryWetMidi.Smf.Interaction;
using NotReaper.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace NotReaper.IO {

	public class AudicaHandler : MonoBehaviour {

		public static AudicaFile LoadAudicaFile(string path) {

			AudicaFile audicaFile = new AudicaFile();
			ZipFile audicaZip = ZipFile.Read(path);

			string appPath = Application.dataPath;
			bool easy = false, standard = false, advanced = false, expert = false;


			HandleCache.CheckCacheFolderValid();
			HandleCache.ClearCueCache();

			//Figure out what files we need to extract by getting the song.desc.
			foreach (ZipEntry entry in audicaZip.Entries) {
				if (entry.FileName == "song.desc") {
					MemoryStream ms = new MemoryStream();
					entry.Extract(ms);
					string tempDesc = Encoding.UTF8.GetString(ms.ToArray());

					JsonUtility.FromJsonOverwrite(tempDesc, audicaFile.desc);


					ms.Dispose();
					continue;
				}

				//Extract the cues files.
				else if (entry.FileName == "expert.cues") {
					entry.Extract($"{appPath}/.cache");
					expert = true;

				} else if (entry.FileName == "advanced.cues") {
					entry.Extract($"{appPath}/.cache");
					advanced = true;

				} else if (entry.FileName == "moderate.cues") {
					entry.Extract($"{appPath}/.cache");
					standard = true;

				} else if (entry.FileName == "beginner.cues") {
					entry.Extract($"{appPath}/.cache");
					easy = true;
				} 
			}

			//Now we fill the audicaFile var with all the things it needs.
			//Remember, all props in audicaFile.desc refer to either moggsong or the name of the mogg.
			//Real clips are stored in main audicaFile object.

			//Load the cues files.
			if (expert) {
				audicaFile.diffs.expert = JsonUtility.FromJson<CueFile>(File.ReadAllText($"{appPath}/.cache/expert.cues"));
			}
			if (advanced) {
				audicaFile.diffs.advanced = JsonUtility.FromJson<CueFile>(File.ReadAllText($"{appPath}/.cache/advanced.cues"));
			}
			if (standard) {
				audicaFile.diffs.moderate = JsonUtility.FromJson<CueFile>(File.ReadAllText($"{appPath}/.cache/moderate.cues"));
			}
			if (easy) {
				audicaFile.diffs.beginner = JsonUtility.FromJson<CueFile>(File.ReadAllText($"{appPath}/.cache/beginner.cues"));
			}

			MemoryStream temp = new MemoryStream();

			//Load the names of the moggs
			foreach (ZipEntry entry in audicaZip.Entries) {

				if (entry.FileName == audicaFile.desc.moggSong) {
					entry.Extract(temp);
					audicaFile.desc.moggMainSong = MoggSongParser.parse_metadata(Encoding.UTF8.GetString(temp.ToArray())) [0];

				} else if (entry.FileName == audicaFile.desc.sustainSongLeft) {
					entry.Extract(temp);
					audicaFile.desc.moggSustainSongLeft = MoggSongParser.parse_metadata(Encoding.UTF8.GetString(temp.ToArray())) [0];

				} else if (entry.FileName == audicaFile.desc.sustainSongRight) {
					entry.Extract(temp);
					audicaFile.desc.moggSustainSongRight = MoggSongParser.parse_metadata(Encoding.UTF8.GetString(temp.ToArray())) [0];
					
				} else if (entry.FileName == "song.mid" || entry.FileName == audicaFile.desc.midiFile) {
					string midiFiileName = $"{appPath}/.cache/song.mid";

					entry.Extract($"{appPath}/.cache", ExtractExistingFileAction.OverwriteSilently);

					if(entry.FileName != "song.mid") {
						File.Delete(midiFiileName);
						File.Move($"{appPath}/.cache/" + audicaFile.desc.midiFile, midiFiileName);

						//Sometimes these midi files get marked with strange attributes. Reset them to normal so we don't have problems deleting them
						File.SetAttributes(midiFiileName, FileAttributes.Normal);
					}

					audicaFile.song_mid = MidiFile.Read(midiFiileName);
				}

				temp.SetLength(0);

			}


			bool mainSongCached = false, sustainRightCached = false, sustainLeftCached = false;

			if (File.Exists($"{appPath}/.cache/{audicaFile.desc.cachedMainSong}.ogg"))
				mainSongCached = true;

			if (File.Exists($"{appPath}/.cache/{audicaFile.desc.cachedSustainSongRight}.ogg"))
				sustainRightCached = true;

			if (File.Exists($"{appPath}/.cache/{audicaFile.desc.cachedSustainSongLeft}.ogg"))
				sustainLeftCached = true;


			//If all the songs were already cached, skip this and go to the finish.
			if (mainSongCached && sustainRightCached && sustainLeftCached) {
				Debug.Log("Audio files were already cached and will be loaded.");
				goto Finish;
			}

			Debug.Log("Files not cached... Loading...");

			//If the files weren't cached, we now need to cache them manually then load them.
			MemoryStream tempMogg = new MemoryStream();

			foreach (ZipEntry entry in audicaZip.Entries) {

				if (!mainSongCached && entry.FileName == audicaFile.desc.moggMainSong) {
					entry.Extract(tempMogg);
					MoggToOgg(tempMogg.ToArray(), audicaFile.desc.cachedMainSong);

				} else if (!sustainRightCached && entry.FileName == audicaFile.desc.moggSustainSongRight) {
					entry.Extract(tempMogg);
					MoggToOgg(tempMogg.ToArray(), audicaFile.desc.cachedSustainSongRight);

				} else if (!sustainLeftCached && entry.FileName == audicaFile.desc.moggSustainSongLeft) {
					entry.Extract(tempMogg);
					MoggToOgg(tempMogg.ToArray(), audicaFile.desc.cachedSustainSongLeft);

				}

				tempMogg.SetLength(0);

			}

			Finish:

				audicaFile.filepath = path;
			audicaZip.Dispose();

			return audicaFile;
		}

		public static void MoggToOgg(byte[] bytes, string name) {
			byte[] oggStartLocation = new byte[4];

			oggStartLocation[0] = bytes[4];
			oggStartLocation[1] = bytes[5];
			oggStartLocation[2] = bytes[6];
			oggStartLocation[3] = bytes[7];

			int start = BitConverter.ToInt32(oggStartLocation, 0);

			byte[] dst = new byte[bytes.Length - start];
			Array.Copy(bytes, start, dst, 0, dst.Length);
			File.WriteAllBytes($"{Application.dataPath}/.cache/{name}.ogg", dst);

		}


	}
}