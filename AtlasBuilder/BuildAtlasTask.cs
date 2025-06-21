using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace AtlasBuilder
{
	public class BuildAtlasTask : Task
	{
		[DllImport("..\\..\\..\\native\\win-x64\\CutlassWrapper.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool GenerateAtlas(string fontFileName, double fontSize, double distanceRange, string outputPng, string outputJson);

		[Required]
		public string ContentFolder { get; set; }

		[Required]
		public string OutputFolder { get; set; }

		[Required]
		public double FontSize { get; set; }

		[Required]
		public double DistanceRange { get; set; }

		public override bool Execute()
		{
			try
			{
				Log.LogMessage(MessageImportance.High, $"Running Build Atlas Task...");

				// Find all font files in content folder
				var fontExtensions = new[] { ".ttf", ".otf" };
				var fontFiles = Directory.EnumerateFiles(ContentFolder, "*.*", SearchOption.AllDirectories)
					.Where(f => fontExtensions.Contains(Path.GetExtension(f).ToLower()));

				if (!fontFiles.Any())
				{
					Log.LogMessage(MessageImportance.High, "No font files found.");
					return true;
				}

				if (!Directory.Exists(OutputFolder))
				{
					Directory.CreateDirectory(OutputFolder);
				}
				
				var areAllUpToDate = true;

				foreach (var fontFile in fontFiles)
				{

					string fontName = Path.GetFileNameWithoutExtension(fontFile);
					var outputJsonPath = Path.Combine(OutputFolder, $"{fontName}.json");
					var outputPngPath = Path.Combine(OutputFolder, $"{fontName}.png");


					// Check if rebuild is needed
					if (ShouldRebuildAtlas(fontFile, outputJsonPath, outputPngPath))
					{
						Log.LogMessage(MessageImportance.High, "Font atlas needs rebuilding...");

						// Call your interop library here
						var success = GenerateAtlas(fontFile, outputPngPath, outputJsonPath);

                        if (!success)
						{
							Log.LogError("Failed to generate font atlas");
							return false;
						}

						areAllUpToDate = false;

                        Log.LogMessage(MessageImportance.High, $"{fontName} atlas generated successfully");
					}
					else
					{
						Log.LogMessage(MessageImportance.Normal, $"{fontName} atlas is up to date");
					}

                }
				
				if (areAllUpToDate)
                    Log.LogMessage(MessageImportance.High, "All atlases are up to date");


				return true;
			}
			catch (Exception ex)
			{
				Log.LogError($"Error generating font atlas: {ex.Message}");
				return false;
			}
		}

		private bool ShouldRebuildAtlas(string fontFile, string jsonPath, string pngPath)
		{
			// Check if output files exist
			if (!File.Exists(jsonPath) || !File.Exists(pngPath))
				return true;

			// Get the oldest output file time
			var jsonTime = File.GetLastWriteTime(jsonPath);
			var pngTime = File.GetLastWriteTime(pngPath);
			var oldestOutputTime = jsonTime < pngTime ? jsonTime : pngTime;

			// Check if any font file is newer than output
			return File.GetLastWriteTime(fontFile) > oldestOutputTime;
		}

		private bool GenerateAtlas(string fontFile, string pngPath, string jsonPath)
		{
			try
			{
				if (GenerateAtlas(fontFile, FontSize, DistanceRange, pngPath, jsonPath))
				{
					Log.LogMessage(MessageImportance.High, $"Generated atlas for {fontFile}");
					return true;
				}
				Log.LogError($"Failed to create atlas for {fontFile}");
				return false;
			}
			catch (Exception ex)
			{
				Log.LogError($"Atlas generation failed: {ex.Message}");
				return false;
			}
		}
	}
}
