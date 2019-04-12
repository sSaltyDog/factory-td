﻿using Unity.Jobs;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;

namespace MapGeneration
{
    public struct FloodFillCellGroupJob : IJob
    {
        public EntityCommandBuffer commandBuffer;

        public Matrix<WorleyNoise.PointData> matrix;

        [ReadOnly] public WorleyNoise.CellData startCell;

        [ReadOnly] public Entity sectorEntity;

        [ReadOnly] public WorleyNoise worley;
        [ReadOnly] public TopologyUtil topologyUtil;

        public void Execute()
        {
            FloodFillCell();
            
            AddBufferFromMatrix();

            CellSystem.MatrixComponent matrixComponent = AddCellMatrixComponent();

            SetPosition(matrixComponent.root);
        }

        public void FloodFillCell()
        {
            NativeQueue<WorleyNoise.PointData> dataToCheck = new NativeQueue<WorleyNoise.PointData>(Allocator.Temp);

            WorleyNoise.PointData initialPointData = GetPointData(startCell.position);
            dataToCheck.Enqueue(initialPointData);
            matrix.AddItem(initialPointData, initialPointData.pointWorldPosition);

            float startCellGrouping = topologyUtil.CellGrouping(startCell.index);

            while(dataToCheck.Count > 0)
            {
                WorleyNoise.PointData data = dataToCheck.Dequeue();

                bool currentPointInCell = topologyUtil.CellGrouping(data.currentCellIndex) == startCellGrouping;

                for(int x = -1; x <= 1; x++)
                    for(int z = -1; z <= 1; z++)
                    {
                        float3 adjacentPosition = new float3(x, 0, z) + data.pointWorldPosition;
                        WorleyNoise.PointData adjacentData = GetPointData(adjacentPosition);

                        bool adjacentPointInCell = topologyUtil.CellGrouping(adjacentData.currentCellIndex) == startCellGrouping;
                        if(matrix.ItemIsSet(adjacentPosition) || (!currentPointInCell && !adjacentPointInCell)) continue;

                        dataToCheck.Enqueue(adjacentData);
                        matrix.AddItem(adjacentData, adjacentData.pointWorldPosition);
                    }

            }

            dataToCheck.Dispose();
        }

        WorleyNoise.PointData GetPointData(float3 position)
        {
            WorleyNoise.PointData data = worley.GetPointData(position.x, position.z);
            data.pointWorldPosition = position;
            data.isSet = 1;
            return data;
        }

        void AddBufferFromMatrix()
        {
            DynamicBuffer<WorleyNoise.PointData> worleyBuffer = commandBuffer.AddBuffer<WorleyNoise.PointData>(sectorEntity);
            worleyBuffer.CopyFrom(matrix.matrix);
        }

        CellSystem.MatrixComponent AddCellMatrixComponent()
        {
            CellSystem.MatrixComponent cellMatrix = new CellSystem.MatrixComponent{
                root = matrix.rootPosition,
                width = matrix.width
            };
            commandBuffer.AddComponent<CellSystem.MatrixComponent>(sectorEntity, cellMatrix);
            return cellMatrix;
        }

        void SetPosition(float3 position)
        {
            float3 pos = new float3(position.x, 0, position.z);
            commandBuffer.SetComponent(sectorEntity, new Translation{ Value = pos });
        }
    }
}