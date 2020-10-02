using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Program
{
	struct smdVert
	{
		public float PosX;
		public float PosY;
		public float PosZ;
		public float NormX;
		public float NormY;
		public float NormZ;
		public float U;
		public float V;

		public smdVert(string data)
		{
			string[] d = data.Trim().Split(" ");
			PosX = float.Parse(d[1]);
			PosY = float.Parse(d[2]);
			PosZ = float.Parse(d[3]);
			NormX = float.Parse(d[4]);
			NormY = float.Parse(d[5]);
			NormZ = float.Parse(d[6]);
			U = float.Parse(d[7]);
			V = float.Parse(d[8]);
		}
	}

	class smd
	{
		public static List<smdVert> GetSmdData(string fileName)
		{
			List<smdVert> verts = new List<smdVert>();

			using (StreamReader reader = File.OpenText(fileName))
			{
				string current = null;
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					if (current != null && line == "end")
					{
						current = null;
					}
					else if (current == null && line == "triangles")
					{
						current = "triangles";
					}
					else if (current == "triangles")
					{
						for (int i = 0; i < 3; i++)
						{
							if ((line = reader.ReadLine()) == null)
								throw new Exception();

							verts.Add(new smdVert(line));
						}
					}
				}
			}

			return verts;
		}
	}
}
