using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Program
{
	class vmt
	{
		public string basetexture;

		public vmt(string fileName)
		{
			basetexture = fileName.Replace(".vmt", ".vtf");
			/*
			using (StreamReader reader = File.OpenText(fileName))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					line = line.Trim();
					if (line.StartsWith("$basetexture"))
						basetexture = line.Split("\"")[1] + ".vtf";
				}
			}
			*/
		}
	}
}
