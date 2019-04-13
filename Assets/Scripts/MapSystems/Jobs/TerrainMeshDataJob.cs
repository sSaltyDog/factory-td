﻿using Unity.Jobs;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;

using ECSMesh;

namespace MapGeneration
{
    public struct TerrainMeshDataJob : IJob
    {
        public EntityCommandBuffer commandBuffer;

        DynamicBuffer<Vertex> vertices;
        DynamicBuffer<VertColor> colors;
        DynamicBuffer<Triangle> triangles;

        [ReadOnly] public Entity sectorEntity;
        [ReadOnly] public CellSystem.MatrixComponent matrix;
        [ReadOnly] public DynamicBuffer<WorleyNoise.PointData> worley;
        [ReadOnly] public DynamicBuffer<TopologySystem.Height> topology;
        [ReadOnly] public ArrayUtil arrayUtil;

        public void Execute()
        {
            vertices = commandBuffer.AddBuffer<Vertex>(sectorEntity);
            colors = commandBuffer.AddBuffer<VertColor>(sectorEntity);
            triangles = commandBuffer.AddBuffer<Triangle>(sectorEntity);

            int indexOffset = 0;

            for(int x = 0; x < matrix.width-1; x++)
                for(int z = 0; z < matrix.width-1; z++)
                {
                    int2 bl = new int2(x,   z  );
                    int2 tl = new int2(x,   z+1);
                    int2 tr = new int2(x+1, z+1);
                    int2 br = new int2(x+1, z  );

                    WorleyNoise.PointData bottomLeftWorley  = matrix.GetItem<WorleyNoise.PointData>(bl, worley, arrayUtil);
                    WorleyNoise.PointData topLeftWorley     = matrix.GetItem<WorleyNoise.PointData>(tl, worley, arrayUtil);
                    WorleyNoise.PointData topRightWorley    = matrix.GetItem<WorleyNoise.PointData>(tr, worley, arrayUtil);
                    WorleyNoise.PointData bottomRightWorley = matrix.GetItem<WorleyNoise.PointData>(br, worley, arrayUtil);
                    
                    if( bottomLeftWorley.isSet  == 0 ||
                        topLeftWorley.isSet     == 0 ||
                        topRightWorley.isSet    == 0 ||
                        bottomRightWorley.isSet == 0 )
                    {
                        continue;
                    }

                    GetVertexDataForVertex(indexOffset, bl);
                    GetVertexDataForVertex(indexOffset, tl);
                    GetVertexDataForVertex(indexOffset, tr);
                    GetVertexDataForVertex(indexOffset, br);

                    GetTriangleDataForPoint(indexOffset);

                    indexOffset += 4;
                }
        }

        void GetVertexDataForVertex(int indexOffset, int2 pointIndex)
        {
            TopologySystem.Height height = matrix.GetItem<TopologySystem.Height>(pointIndex, topology, arrayUtil);
            vertices.Add(new Vertex{ vertex = new float3(pointIndex.x, height.height, pointIndex.y) });
            colors.Add(new VertColor{ color = new float4(0.6f) });
        }

        void GetTriangleDataForPoint(int indexOffset)
        {
            triangles.Add(new Triangle{ triangle = 0 + indexOffset });
            triangles.Add(new Triangle{ triangle = 1 + indexOffset });
            triangles.Add(new Triangle{ triangle = 2 + indexOffset });
            triangles.Add(new Triangle{ triangle = 0 + indexOffset });
            triangles.Add(new Triangle{ triangle = 2 + indexOffset });
            triangles.Add(new Triangle{ triangle = 3 + indexOffset });
        }
    }
}