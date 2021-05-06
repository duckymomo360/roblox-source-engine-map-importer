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
		public string matLocation;

		public qc(string modelName)
		{
			using (StreamReader reader = File.OpenText($"tmp\\{modelName}\\{modelName}.qc"))
			{
				bool capturing = false;
				string current = null;
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					line = line.Trim();

					if (line.StartsWith("$"))
					{
						if (line.IndexOf(" ") > 0)
							current = line.Substring(1, line.IndexOf(" ") - 1);
						else
							current = line.Substring(1);
					}
					else if (line == "{")
					{
						capturing = true;
						line = reader.ReadLine();//skip this line for the code ahead
					}
					else if (line == "}")
						capturing = false;
					
					if (current == "bodygroup" && capturing)
					{
						string smd = line.Split("\"")[1];//whatever lets just assume its this one
						smdFile = $"tmp\\{modelName}\\{smd}";
					}

					if (line.StartsWith("$cdmaterials"))
						matLocation = line.Split("\"")[1];

					//Debug.WriteLine($"parsing qc: {current} {capturing}");
				}
			}
		}
	}
}
