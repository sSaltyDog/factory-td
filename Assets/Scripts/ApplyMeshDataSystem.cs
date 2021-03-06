﻿using UnityEngine;

using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

using UnityEditor;

using ECSMesh;

public class ApplyMeshDataSystem : ComponentSystem
{
    EntityManager entityManager;

    EntityQuery applyMeshGroup;

	public static Material terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain.mat");
	public static Material waterMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Water.mat");

    protected override void OnCreate()
    {
        entityManager = World.Active.EntityManager;

        EntityQueryDesc applyMeshQuery = new EntityQueryDesc{
			All = new ComponentType[] { typeof(Vertex) },
			None = new ComponentType[] { typeof(RenderMesh) }
		};
        applyMeshGroup = GetEntityQuery(applyMeshQuery);
    }

    protected override void OnUpdate()
    {
        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
		var chunks = applyMeshGroup.CreateArchetypeChunkArray(Allocator.TempJob);

		var entityType = GetArchetypeChunkEntityType();
        var vertType = GetArchetypeChunkBufferType<Vertex>(true);
        var triType = GetArchetypeChunkBufferType<Triangle>(true);
        var colorType = GetArchetypeChunkBufferType<VertColor>(true);

		for(int c = 0; c < chunks.Length; c++)
		{
			var chunk = chunks[c];

			var entities = chunk.GetNativeArray(entityType);
            var vertArrays = chunk.GetBufferAccessor<Vertex>(vertType);
            var triArrays = chunk.GetBufferAccessor<Triangle>(triType);
            var colorArrays = chunk.GetBufferAccessor<VertColor>(colorType);
		    
            for(int e = 0; e < entities.Length; e++)
			{
				Entity entity = entities[e];

                Mesh mesh = MakeMesh(vertArrays[e], triArrays[e], colorArrays[e]);
                SetMeshComponent(mesh, entity, commandBuffer);

				commandBuffer.RemoveComponent(entity, typeof(Vertex));
				commandBuffer.RemoveComponent(entity, typeof(Triangle));
				commandBuffer.RemoveComponent(entity, typeof(VertColor));
				commandBuffer.RemoveComponent(entity, typeof(WorleyNoise.PointData));
            }
        }

        commandBuffer.Playback(entityManager);
        commandBuffer.Dispose();

        chunks.Dispose();
    }

    Mesh MakeMesh(DynamicBuffer<Vertex> vertices, DynamicBuffer<Triangle> triangles, DynamicBuffer<VertColor> colors)
	{
		Vector3[] verticesArray = new Vector3[vertices.Length];
        int[] trianglesArray = new int[triangles.Length];
        Color[] colorsArray 	= new Color[vertices.Length];
		for(int i = 0; i < vertices.Length; i++)
		{
			verticesArray[i] 	= vertices[i].vertex;
			colorsArray[i] 		= new Color(colors[i].color.x, colors[i].color.y, colors[i].color.z, colors[i].color.w);
		}

		for(int i = 0; i < triangles.Length; i++)
		{
            trianglesArray[i]   = triangles[i].triangle;
		}

		Mesh mesh 		= new Mesh();
		mesh.vertices	= verticesArray;
		mesh.colors 	= colorsArray;
		mesh.SetTriangles(trianglesArray, 0);

		mesh.RecalculateNormals();

		return mesh;
	}

	void SetMeshComponent(Mesh mesh, Entity entity, EntityCommandBuffer commandBuffer)
	{
		RenderMesh renderer = new RenderMesh();
		renderer.mesh = mesh;

		bool water = entityManager.HasComponent<Tags.WaterEntity>(entity);

		renderer.material = water ? waterMaterial : terrainMaterial;

		commandBuffer.AddSharedComponent(entity, renderer);
	}
}