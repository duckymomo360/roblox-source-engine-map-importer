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

		static bool IsFileLocked(FileInfo file)
		{
			try
			{
				using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
				{
					stream.Close();
				}
			}
			catch (IOException)
			{
				//the file is unavailable because it is:
				//still being written to
				//or being processed by another thread
				//or does not exist (has already been processed)
				return true;
			}

			//file is not locked
			return false;
		}

		static string gamefiles = @"D:\Games\INFRA Complete Edition\infra\pak01_dir\";
		static string robloxContent = @"C:\Users\tosch\AppData\Local\Roblox\Versions\version-4990a8ad45cb49f9\content\sourceimport\";

		public static HttpListener listener;
		public static string url = "http://localhost:1337/";
		public static string vmfDirectory = @"D:\Downloads\INFRA vmf\";

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
						string[] files = Directory.GetFiles(vmfDirectory, req.QueryString["q"], SearchOption.AllDirectories);
						if (files.Length > 0)
							data = File.ReadAllBytes(files[0]);
						break;
					case "/getmodel":
						string mdl_dir = req.QueryString["q"];

						if (!File.Exists(gamefiles + mdl_dir)) // Does this model even exist?
						{
							data = Encoding.UTF8.GetBytes("bad model");
							break;
						}

						string mdlname = Path.GetFileNameWithoutExtension(mdl_dir.Split("/").Last());
						string outFileName = mdlname + ".mesh";
						string outFile = robloxContent + outFileName;

						if (File.Exists(outFile)) // Has this model already been extracted?
						{
							data = Encoding.UTF8.GetBytes("rbxasset://sourceimport/" + outFileName);
							break;
						}

						Process.Start("Crowbar.exe", $"-p \"{gamefiles + mdl_dir}\" -o \"tmp\\{mdlname}\"");
						
						while (!Directory.Exists($"tmp\\{mdlname}"))
							Thread.Sleep(100);

						while (!File.Exists($"tmp\\{mdlname}\\{mdlname}.smd"))
							Thread.Sleep(100);

						while (IsFileLocked(new FileInfo($"tmp\\{mdlname}\\{mdlname}.smd")))
                        {
							Console.WriteLine("File in use");
							Thread.Sleep(100);
						}

						List<smdVert> verts = smd.GetSmdData($"tmp\\{mdlname}\\{mdlname}.smd");

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
								face.a = i * 3 + 0;
								face.b = i * 3 + 1;
								face.c = i * 3 + 2;

								Write(file, face);
							}

							writer.Close();
						}
						data = Encoding.UTF8.GetBytes("rbxasset://sourceimport/" + outFileName);
						Console.WriteLine("Exported.");
						//Directory.Delete("tmp", true);

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
