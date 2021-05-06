using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Program
{
	class qc
	{
		public string smdFile;
		public string cdmaterials; // $cdmaterials defines the folders in which the game will search for the model's materials.
		public List<List<string>> texturegroup = null;

		public qc(string modelName)
		{
			using (StreamReader reader = File.OpenText($"tmp\\{modelName}\\{modelName}.qc"))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					line = line.Trim();

					if (line.StartsWith("$cdmaterials"))
					{
						cdmaterials = line.Split(" ")[1].Replace("\"", "");
					}
					else if (line.StartsWith("$bodygroup"))
					{
						reader.ReadLine(); // skip "{"
						line = reader.ReadLine().Trim(); // this is the first body in the group
						smdFile = line.Split(" ")[1].Replace("\"", "");
					}
					else if (line.StartsWith("$texturegroup") && texturegroup == null) // lets parse only one texture group
					{
						texturegroup = new List<List<string>>();
						reader.ReadLine();

						while (true)
						{
							line = reader.ReadLine().Trim();
							if (line == "}")
								break;

							if (line == "{")
							{
								List<string> textures = new List<string>();
								while (true)
								{
									line = reader.ReadLine().Trim();
									if (line == "}")
										break;

									textures.Add(line.Replace("\"", ""));
								}

								texturegroup.Add(textures);
							}
						}
					}

					//Debug.WriteLine($"parsing qc: {current} {capturing}");
				}
			}
		}
	}
}
