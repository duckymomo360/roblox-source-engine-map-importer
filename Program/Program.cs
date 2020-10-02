using RobloxMeshFormat;
using Program;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Linq;
using System.Threading;

namespace SourceEngine
{
	class Program
	{
		static void Write(Stream stream, object o)
		{
			byte[] buffer = new byte[Marshal.SizeOf(o.GetType())];
			GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			Marshal.StructureToPtr(o, gcHandle.AddrOfPinnedObject(), true);
			stream.Write(buffer, 0, buffer.Length);
			gcHandle.Free();
		}

		static string outFile = @"output.mesh";
		static string gamefiles = @"D:\Games\INFRA Complete Edition\infra\pak01_dir\";
		static string mdl_dir = @"models\props_elevator\construction_elevator.mdl";

		static void Main(string[] args)
		{
			string mdlname = Path.GetFileNameWithoutExtension(mdl_dir.Split("\\").Last());

			Process.Start("Crowbar.exe", "-p \"" + gamefiles + mdl_dir + "\" -o \"tmp\"");
			
			while (!Directory.Exists($"tmp\\{mdlname}"))
			{
				Thread.Sleep(100);
			}

			while (!File.Exists($"tmp\\{mdlname}\\{mdlname}.smd"))
			{
				Thread.Sleep(100);
			}

			List<smdVert> verts = smd.GetSmdData($"tmp\\{mdlname}\\{mdlname}.smd");
			FileStream file = File.Open(outFile, FileMode.Create);

			StreamWriter toiletjoke = new StreamWriter(file);
			toiletjoke.WriteLine("version 2.00");
			toiletjoke.Flush();//hahhaha

			MeshHeader meshHeader = new MeshHeader();
			meshHeader.sizeof_MeshHeader = (short)Marshal.SizeOf(typeof(MeshHeader));
			meshHeader.sizeof_Vertex = (byte)Marshal.SizeOf(typeof(Vertex));
			meshHeader.sizeof_Face = (byte)Marshal.SizeOf(typeof(Face));
			meshHeader.numVerts = (uint)verts.Count;
			meshHeader.numFaces = (uint)verts.Count / 3;
			Write(file, meshHeader);
			
			for (int i = 0; i < meshHeader.numVerts; i++)
			{
				Vertex vertex = new Vertex();
				vertex.px = verts[i].PosX;
				vertex.py = verts[i].PosY;
				vertex.pz = verts[i].PosZ;
				vertex.nx = verts[i].NormX;
				vertex.ny = verts[i].NormY;
				vertex.nz = verts[i].NormZ;
				vertex.tu = verts[i].U;
				vertex.tv = verts[i].V;
				vertex.tw = 0;
				vertex.r = 255;
				vertex.g = 255;
				vertex.b = 255;
				vertex.a = 255;

				Write(file, vertex);
			}


			for (uint i = 0; i < meshHeader.numFaces; i++)
			{
				Face face = new Face();
				face.a = i*3+0;
				face.b = i*3+1;
				face.c = i*3+2;

				Write(file, face);
			}

			file.Dispose();
			Directory.Delete("tmp", true);
			Console.ReadKey();
		}
	}
}
