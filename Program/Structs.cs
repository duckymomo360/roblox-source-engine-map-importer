using System;

namespace RobloxMeshFormat
{
	// https://devforum.roblox.com/t/roblox-mesh-format/326114

	[Serializable]
	public struct MeshHeader
	{
		public short sizeof_MeshHeader;
		public byte sizeof_Vertex;
		public byte sizeof_Face;

		public uint numVerts;
		public uint numFaces;
	}

	[Serializable]
	public struct Vertex
	{
		public float px, py, pz; // XYZ position of the vertex's position
		public float nx, ny, nz; // XYZ unit vector of the vertex's normal vector.
		public float tu, tv, tw; // UV coordinate of the vertex (tw is reserved)
		public byte r, g, b, a; // RGBA color of the vertex
	}

	[Serializable]
	public struct Face
	{
		public uint a; // 1st Vertex Index
		public uint b; // 2nd Vertex Index
		public uint c; // 3rd Vertex Index
	}
}