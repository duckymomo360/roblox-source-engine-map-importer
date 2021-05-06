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
using System.Net;
using System.Threading.Tasks;
using System.Text;
using TgaSharp;
using System.Drawing;
using System.Drawing.Imaging;

namespace SourceEngine
{
	class Program
	{
		static string vtf2tga(string vtf)
		{
			string matname = Path.GetFileNameWithoutExtension(vtf.Split("/").Last());
			string tga = $"tmp\\{matname}.tga";
			if (File.Exists(tga))
				return tga;
			Process.Start("sourcetools\\vtf2tga.exe", $"-i \"{vtf}\" -o \"{tga}\"");

			int giveup = 0;
			while (!File.Exists(tga) || IsFileLocked(tga)) // some vtf files fail to read (mobile_phone_001.vtf)
			{
				if (giveup++ > 200)
					break;
				Thread.Sleep(10);
			}
			return tga;
		}

		static string vtf2png(string vtf)
		{
			string matname = Path.GetFileNameWithoutExtension(vtf.Split("/").Last());
			string png = $"tmp\\{matname}.png";
			if (File.Exists(png))
				return png;
			try
			{
				string tga = vtf2tga(vtf);
				Bitmap bmp = new TGA(tga).ToBitmap();
				bmp.Save(png, ImageFormat.Png);
				File.Delete(tga);
			}
			catch { }
			return png;
		}

		static void Write(Stream stream, object o)
		{
			byte[] buffer = new byte[Marshal.SizeOf(o.GetType())];
			GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			Marshal.StructureToPtr(o, gcHandle.AddrOfPinnedObject(), true);
			stream.Write(buffer, 0, buffer.Length);
			gcHandle.Free();
		}

		static bool IsFileLocked(string file)
		{
			try
			{
				using (FileStream stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None))
				{
					stream.Close();
				}
			}
			catch (IOException)
			{
				return true;
			}

			return false;
		}

		public static void WaitForExtract(string file)
		{
			int time = 0;
			while (!File.Exists(file) || IsFileLocked(file))
			{
				if (time++ > 100)
					Console.WriteLine("still waiting for " + file);
				Thread.Sleep(10);
			}
		}

		static string gamefiles;
		static string robloxContent;
		static string sourceImport;

		public static HttpListener listener;
		public static string url = "http://localhost:1337/";
		
		public static async Task HandleIncomingConnections()
		{
			while (true)
			{
				// Will wait here until we hear from a connection
				HttpListenerContext ctx = await listener.GetContextAsync();

				// Peel out the requests and response objects
				HttpListenerRequest req = ctx.Request;
				HttpListenerResponse resp = ctx.Response;

				byte[] data = { };

				switch (req.Url.AbsolutePath)
				{
					case "/getvmf":
						string[] files = Directory.GetFiles(@"D:\Downloads\INFRA vmf\", req.QueryString["q"], SearchOption.AllDirectories);
						if (files.Length > 0)
							data = File.ReadAllBytes(files[0]);
						break;
					case "/getmat":
						string mat_dir = "materials/" + req.QueryString["q"] + ".vmt";
						
						if (!File.Exists(gamefiles + mat_dir)) // check if this material even exists
						{
							Console.Write("material doesnt exist " + mat_dir);
							data = Encoding.UTF8.GetBytes("bad material " + mat_dir);
							break;
						}

						string matname = Path.GetFileNameWithoutExtension(mat_dir.Split("\\").Last());
						string outfile = $"{matname}.png";

						if (File.Exists(sourceImport + outfile)) // check if this material has already been extracted
						{
							data = Encoding.UTF8.GetBytes("rbxasset://sourceimport/" + outfile);
							break;
						}

						vmt vmtdata = new vmt(gamefiles + mat_dir);
						string png = vtf2png(vmtdata.basetexture);

						if (File.Exists(png)) // if it doesn't exist lets send back an invalid asset anyways.
							File.Copy(png, sourceImport + outfile, true);

						data = Encoding.UTF8.GetBytes("rbxasset://sourceimport/" + outfile);
						break;
					case "/getmodel":
						string mdl_dir = req.QueryString["q"];

						if (!mdl_dir.StartsWith("models/") || !File.Exists(gamefiles + mdl_dir)) // check if this model even exists
						{
							data = Encoding.UTF8.GetBytes("bad model");
							break;
						}

						string mdlname = Path.GetFileNameWithoutExtension(mdl_dir.Split("/").Last());
						string outFileName = mdlname + ".mesh";
						string outFile = sourceImport + outFileName;

						if (!File.Exists(outFile)) // check if this model has already been extracted
						{
							Process.Start("Crowbar.exe", $"-p \"{gamefiles + mdl_dir}\" -o \"tmp\"");
						}

						while (!Directory.Exists($"tmp\\{mdlname}"))
							Thread.Sleep(10);

						WaitForExtract($"tmp\\{mdlname}\\{mdlname}.qc");
						qc modelInfo = new qc(mdlname);
						WaitForExtract($"tmp\\{mdlname}\\{modelInfo.smdFile}");

						List <smdVert> verts = smd.GetSmdData($"tmp\\{mdlname}\\{modelInfo.smdFile}");
						string probablythetexture = modelInfo.cdmaterials + modelInfo.texturegroup[0][0];

						using (FileStream file = File.Open(outFile, FileMode.Create))
						{
							StreamWriter writer = new StreamWriter(file);
							writer.WriteLine("version 2.00");
							writer.Flush();

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
								vertex.tv = 1 - verts[i].V;
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
								face.a = i * 3 + 0;
								face.b = i * 3 + 1;
								face.c = i * 3 + 2;

								Write(file, face);
							}

							writer.Close();
						}

						data = Encoding.UTF8.GetBytes("rbxasset://sourceimport/" + outFileName + "|" + probablythetexture);
						Console.WriteLine("Exported.");

						break;
				}

				// Write the response info
				resp.ContentType = "text/html";
				resp.ContentEncoding = Encoding.UTF8;
				resp.ContentLength64 = data.LongLength;

				// Write out to the response stream (asynchronously), then close it
				await resp.OutputStream.WriteAsync(data, 0, data.Length);
				resp.Close();
			}
		}
		
		static void Main(string[] args)
		{
			if (!File.Exists("config.txt"))
			{
				using (StreamWriter writer = new StreamWriter("config.txt"))
				{
					while (true)
					{
						Console.WriteLine("enter roblox studio content directory");
						string rbxfolder = Console.ReadLine();
						if (Directory.Exists(rbxfolder))
						{
							writer.WriteLine(rbxfolder);
							break;
						}
						else Console.WriteLine("invalid directory");
					}
					while (true)
					{
						Console.WriteLine("enter source game content directory");
						string srcfolder = Console.ReadLine();
						if (Directory.Exists(srcfolder))
						{
							writer.WriteLine(srcfolder);
							break;
						}
						else Console.WriteLine("invalid directory");
					}
				}
			}

			using (StreamReader reader = new StreamReader("config.txt"))
			{
				robloxContent = reader.ReadLine();
				sourceImport = robloxContent + "\\sourceimport\\";
				gamefiles = reader.ReadLine() + "\\";
			}

			if (!Directory.Exists(sourceImport))
				Directory.CreateDirectory(sourceImport);

			if (!Directory.Exists("tmp"))
				Directory.CreateDirectory("tmp");

			// Create a Http server and start listening for incoming connections
			listener = new HttpListener();
			listener.Prefixes.Add(url);
			listener.Start();
			Console.WriteLine("Listening for connections on {0}", url);

			// Handle requests
			Task listenTask = HandleIncomingConnections();
			listenTask.GetAwaiter().GetResult();

			// Close the listener
			listener.Close();
			
			Console.ReadKey();
		}
	}
}