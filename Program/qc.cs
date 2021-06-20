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

		private StreamReader readBlock(StreamReader reader)
        {
			string content = "";
			int depth = 0;
			while (true)
            {
				int cint = reader.Read();
				if (cint == -1)
					return null;

				string c = Convert.ToChar(cint).ToString();

				if (c == "{")
				{
					depth++;
					if (depth == 1)
						continue;
				}
				else if (c == "}")
				{
					depth--;
					if (depth == 0)
						break;
				}

				if (depth > 0)
					content += c;
			}
			Debug.WriteLine(content);
			byte[] byteArray = Encoding.UTF8.GetBytes(content);
			MemoryStream stream = new MemoryStream(byteArray);
			return new StreamReader(stream);
		}

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
						// this is awful. end me.
						texturegroup = new List<List<string>>();
						StreamReader block = readBlock(reader);
						while (true)
                        {
							StreamReader subBlock = readBlock(block);
							if (subBlock == null)
								break;
							List<string> grp = new List<string>();
							while (true)
                            {
								int cint = subBlock.Read();
								if (cint == -1) break;
								string c = Convert.ToChar(cint).ToString();

								if (c == "\"")
								{
									string capture = "";
									while (true)
									{
										cint = subBlock.Read();
										if (cint == -1) break;
										c = Convert.ToChar(cint).ToString();
										if (c == "\"") break;
										capture += c;
									}
									grp.Add(capture);
								}
							}
							texturegroup.Add(grp);
						}
					}

					//Debug.WriteLine($"parsing qc: {current} {capturing}");
				}
			}
		}
	}
}
