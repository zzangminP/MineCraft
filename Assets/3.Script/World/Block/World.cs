using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
//using System.Diagnostics;
using System.IO;


public class World : MonoBehaviour
{
    /// Chunk를 기반으로 World 생성
    /// 캐릭터 주변 일정 범위 내의 Chunk만 보이게 해서 최적화
    /// 
    /// 
    ///

    public Settings settings;
    // 로딩 완료 플래그
    public bool isLoadingComplete = false;
    private bool isInit = false;
    public float stableFrameDuration = 2.0f; // 프레임 안정성을 확인할 시간 (초)
    public int targetFrameRate = 120; // 목표 프레임 레이트
    public float frameCheckInterval = 0.1f; // 프레임 체크 간격 (초)


    // 플레이어
    public Transform player;
    public Player _player;
    public Vector3 spawnPoint;
    public new Camera camera;
    public bool isWorldGenerate = true; //디버깅용


    
    private Entity_Spawner entitySpawner;

    //  ================================================== //
    //  ================= 월드 생성 관련 ================== //
    //  ================================================== //


    [Header("World Generation Values")]
    //public int seed;
    //public int seedOffset;
    public BiomeAttribute[] biomes;


    //for shader
    [Range(0f, 0.95f)]
    public float globalLightLevel;
    public Color day;
    public Color night;


    // Clouds
    public Clouds clouds;



    // blocks
    public Material material;
    public Material transparentMaterial;
    public Material waterMaterial;

    public BlockType[] blockTypes;
    public GameObject ItemBlock;

    // chunk
    public Chunk[,] chunks = new Chunk[VoxelData.worldSizeInChunks, VoxelData.worldSizeInChunks];
    List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    public Queue<Chunk> ChunksToDraw = new Queue<Chunk>();


    public ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();
    public List<Chunk> chunksToUpdate = new List<Chunk>();

    bool applyingModifications = false;


    Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();


    // 동굴 생성
    public float caveOffset;
    public float caveScale;


    // Water
    public int waterHeight;

    // player에 쓰일 디버그 스크린
    public GameObject debugScreen;
    //private Stopwatch stopwatch;



    // Thread
    Thread chunkUpdateThread;
    public object ChunkUpdateThreadLock = new object();
    public object ChunkListThreadLock = new object();


    //public int cloudHeight = 120;

    public string appPath;
    // 싱글톤
    private static World _instance;
    public static World Instance { get { return _instance; } }
    private void Awake()
    {
        if (_instance != null && _instance != this)
            Destroy(gameObject);
        else
            _instance = this;


        appPath = Application.persistentDataPath;

        _player = player.GetComponent<Player>();

        
        entitySpawner = FindObjectOfType<Entity_Spawner>();
        entitySpawner.gameObject.SetActive(false);

    }

    public WorldData worldData;


    //월드 생성
    private void Start()
    {
        Debug.Log("World Init");
        WorldInit();


    }

    
    private void WorldInit()
    {
        if(isWorldGenerate)
        {
            spawnPoint = new Vector3(VoxelData.WorldCenter, VoxelData.ChunkHeight - 120f, VoxelData.WorldCenter); //플레이어 스폰
        }
        else
        {
            spawnPoint = Vector3.zero;
        }

        
        

        worldData = SaveSystem.LoadWorld("Prototype"); //로드

        string jsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg"); //세팅 읽기
        settings = JsonUtility.FromJson<Settings>(jsonImport);


        UnityEngine.Random.InitState(settings.seed); //시드적용


        Shader.SetGlobalFloat("minGlobalLightLevel", VoxelData.minLightLevel);
        Shader.SetGlobalFloat("maxGlobalLightLevel", VoxelData.maxLightLevel);


        


        player.position = spawnPoint;
        if (settings.enableThreading)
        {
            chunkUpdateThread = new Thread(new ThreadStart(ThreadUpdate));
            chunkUpdateThread.Start();
        }



        CheckViewDistance();


        //UnityEngine.Debug.Log($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");

        playerLastChunkCoord = GetChunkCoordFromVector3(player.transform.position);
        SetGlobalLightValue();

        

        
    }

    

    public void SetGlobalLightValue()
    {
        camera.backgroundColor = Color.Lerp(day, night, globalLightLevel);

        // 쉐이더 글로벌라이트 연결
        Shader.SetGlobalFloat("GlobalLightLevel", globalLightLevel);
    }



    private void Update()
    {
        
        if(isWorldGenerate)
        {
            WorldGenerate();
        }

        if (Input.GetKeyDown(KeyCode.F1))
            SaveSystem.SaveWorld(worldData);

        if (Input.GetKeyDown(KeyCode.F3))
            debugScreen.SetActive(!debugScreen.activeSelf);
    
    
    }

    private void WorldGenerate()
    {
        playerChunkCoord = GetChunkCoordFromVector3(player.position);


        //camera.backgroundColor = Color.Lerp(day, night, globalLightLevel);
        //
        //// 쉐이더 글로벌라이트 연결
        Shader.SetGlobalFloat("GlobalLightLevel", globalLightLevel);


        if (!playerChunkCoord.Equals(playerLastChunkCoord))
            CheckViewDistance();

        if (chunksToCreate.Count > 0)
        {
            CreateChunk();
        }
        else
        {
            isLoadingComplete = true;
            entitySpawner.gameObject.SetActive(true);
            
        }



        if (ChunksToDraw.Count > 0)
        {
            ChunksToDraw.Dequeue().CreateMesh();

        }

        if (!settings.enableThreading)
        {


            if (!applyingModifications)
            {
                ApplyModifications();
            }
            if (chunksToUpdate.Count > 0)
            {
                UpdateChunks();
            }


        }
    }




    public void AddChunkToUpdate(Chunk chunk)
    {

        AddChunkToUpdate(chunk, false);

    }

    public void AddChunkToUpdate(Chunk chunk, bool insert)
    {


        lock (ChunkUpdateThreadLock)
        {


            if (!chunksToUpdate.Contains(chunk))
            {

                if (insert)
                    chunksToUpdate.Insert(0, chunk);
                else
                    chunksToUpdate.Add(chunk);

            }
        }
    }




    void CreateChunk()
    {
        ChunkCoord c = chunksToCreate[0];
        chunksToCreate.RemoveAt(0);
        //activeChunks.Add(c);
        chunks[c.x, c.z].Init();
    }


    ChunkCoord GetChunkCoordFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return new ChunkCoord(x, z);
    }

    public Chunk GetChunkFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return chunks[x, z];
    }

    void UpdateChunks()
    {


        lock (ChunkUpdateThreadLock)
        {


            chunksToUpdate[0].UpdateChunk();
            activeChunks.Add(chunksToUpdate[0].coord);
            chunksToUpdate.RemoveAt(0);




        }
    }

    void ThreadUpdate()
    {
        while (true)
        {

            if (!applyingModifications)
            {
                ApplyModifications();
            }
            if (chunksToUpdate.Count > 0)
            {
                UpdateChunks();
            }

        }
    }

    private void OnDisable()
    {
        if (settings.enableThreading)
        {
            chunkUpdateThread.Abort();
        }
    }


    void ApplyModifications()
    {
        applyingModifications = true;

        //UnityEngine.Debug.Log("Applying modifications...");

        while (modifications.Count > 0)
        {
            Queue<VoxelMod> queue = modifications.Dequeue();

            if (queue == null)
            {
                //UnityEngine.Debug.LogError("Dequeued queue is null");
                continue;
            }

            while (queue.Count > 0)
            {
                VoxelMod v = queue.Dequeue();

                worldData.SetVoxel(v.position, v.id, 1);


            }
        }

        applyingModifications = false;

    }


    // 보여할 chunk 만 활성화해서 최적화

    void CheckViewDistance()
    {

        clouds.UpdateClouds();
        // 플레이어의 현재 chunk 좌표
        ChunkCoord coord = GetChunkCoordFromVector3(player.position);

        // 플레이어의 이전 chunk 좌표 업데이트
        playerLastChunkCoord = playerChunkCoord;

        // 현재 활성화 된 chunk 목록을 복사하여 저장
        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        activeChunks.Clear();

        // 플레이어 시야 거리 내의 청크 검사
        for (int x = coord.x - settings.viewDistance; x < coord.x + settings.viewDistance; x++)
        {
            for (int z = coord.z - settings.viewDistance; z < coord.z + settings.viewDistance; z++)
            {

                ChunkCoord thisChunkCoord = new ChunkCoord(x, z);
                // chunk 가 월드 내에 있는지 확인
                if (IsChunkInWorld(thisChunkCoord))
                {
                    // chunk가 없으면 새로운 chunk 생성
                    if (chunks[x, z] == null)
                    {
                        chunks[x, z] = new Chunk(thisChunkCoord);
                        chunksToCreate.Add(thisChunkCoord);

                    }
                    // 
                    else if (!chunks[x, z].isActive)
                    {
                        chunks[x, z].isActive = true;
                        //activeChunks.Add(new ChunkCoord(x, z));
                    }
                    //chunks[x, z].isActive = true;
                    activeChunks.Add(thisChunkCoord);
                }

                // 이전 활성화 된 chunk 목록에서 제거
                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(thisChunkCoord))
                        previouslyActiveChunks.RemoveAt(i);
                }
            }
        }



        // 시야에 벗어난 chunk를 비활성화
        foreach (ChunkCoord c in previouslyActiveChunks)
            chunks[c.x, c.z].isActive = false;
    }



    // CheckVoxel과는 다르게 월드 내부에서 검사 -> 범위가 더 큼
    public bool CheckForVoxel(Vector3 pos)
    {
        VoxelState voxel = worldData.GetVoxel(pos);
        if (blockTypes[voxel.id].isSolid)
            return true;
        else
            return false;

    }
    public bool CheckWaterForVoxel(Vector3 pos)
    {
        VoxelState voxel = worldData.GetVoxel(pos);
        if (blockTypes[voxel.id].isWater)
            return true;
        else
            return false;

    }
    public VoxelState GetVoxelState(Vector3 pos)
    {
        return worldData.GetVoxel(pos);
    }




    // 아마 맵 생성할때 동굴이라던가 바이옴이라던가 등등 주로 여기서 쓰일것
    public byte GetVoxel(Vector3 pos)
    {


        // =============================================================================================================================================== //
        // ========================================================     첫번째 생성    ==================================================================== //
        // =============================================================================================================================================== //



        int yPos = Mathf.FloorToInt(pos.y);


        // 월드 밖이라면
        if (!IsVoxelInWorld(pos))
            return 0;


        // 청크 제일 밑바닥
        if (yPos == 0)
            return 1;


        ///
        /// 바이옴
        ///

        int solidGroundHeight = 64;
        float sumOfHeights = 0f;
        int count = 0;

        float strongestWeight = 0f;
        int strongestBiomeIndex = 0;
        for (int i = 0; i < biomes.Length; i++)
        {
            float weight = Noise.Get2DPerlin(new Vector2(pos.x, pos.z), biomes[i].offset, biomes[i].scale);

            // Keep track of which weight is strongest
            if (weight > strongestWeight)
            {
                strongestWeight = weight;
                strongestBiomeIndex = i;
            }

            float height = biomes[i].terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biomes[i].terrainScale) * weight;


            if (height > 0)
            {
                sumOfHeights += height;
                count++;
            }




        }

        BiomeAttribute biome = biomes[strongestBiomeIndex];

        sumOfHeights /= count;

        int terrainHeight = Mathf.FloorToInt(sumOfHeights + solidGroundHeight);


        //BiomeAttribute biome = biomes[index];

        // =============================================================================================================================================== //
        // ========================================================     두번째 생성    ==================================================================== //
        // =============================================================================================================================================== //

        // 펄린 노이즈 사용해서...

        byte voxelValue = 0;



        if (yPos == terrainHeight)
        {
            // grass
            voxelValue = biome.surfaceBlock;
        }
        else if


        (yPos < terrainHeight && yPos > terrainHeight - 4) // - 4);
        {

            voxelValue = biome.subSurfaceBlock; //return 5; 
        }
        else if (yPos > terrainHeight)
        {
            if (yPos <= waterHeight)
                return 20;
            else
                return 0;
        }
        else
        {
            voxelValue = 2;//return 2;

        }
        // =============================================================================================================================================== //
        // ========================================================     세번째 생성    ==================================================================== //
        // =============================================================================================================================================== //
        if (yPos < terrainHeight + 20)
        {

            float airPocket = Noise.Get3DPerlin(pos, caveOffset, caveScale);
            float airPocketBorder = Noise.Get3DPerlin(pos, caveOffset + 0.1f, caveScale);
            if (airPocket > 0.6f)
            {
                return 0;

            }
            if (airPocketBorder > 0.4f && airPocketBorder < 0.59f)
            {
                return 0;
            }
        }

        if (voxelValue == 2)
        {



            foreach (Lode lode in biome.lodes)
            {
                if (yPos > lode.minHeight && yPos < lode.maxHeight)
                {
                    if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshold))
                    {
                        //if(lode.blockID == 0)
                        //{
                        //    return voxelValue = 0;
                        //}
                        voxelValue = lode.blockID;
                    }


                }
            }



        }


        // =============================================================================================================================================== //
        // ========================================================     네번째 생성    ==================================================================== //
        // =============================================================================================================================================== //


        if (yPos == terrainHeight && biome.placeMajorFlora)
        {
            if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.majorFloraZoneScale) > biome.majorFloraZoneThreshold)
            {

                if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.majorFloraPlacementScale) > biome.majorFloraPlacementThreshold)
                {
                    //modifications.Enqueue(new VoxelMod(new Vector3(pos.x, pos.y + 1, pos.z), 6));
                    modifications.Enqueue(Structure.GenerateMajorFlora(biome.majorFloraIndex, pos, biome.minHeight, biome.maxHeight));
                }
            }
        }


        return voxelValue;

    }



    bool IsChunkInWorld(ChunkCoord coord)
    {

        return coord.x >= 0 && coord.x < VoxelData.worldSizeInChunks &&
               coord.z >= 0 && coord.z < VoxelData.worldSizeInChunks;

    }


    public bool IsVoxelInWorld(Vector3 pos)
    {


        return pos.x >= 0 && pos.x < VoxelData.worldSizeInBlocks &&
               pos.y >= 0 && pos.y < VoxelData.ChunkHeight &&
               pos.z >= 0 && pos.z < VoxelData.worldSizeInBlocks;
    }
}




[System.Serializable]

public class BlockType
{
    public string blockName;
    public VoxelMeshData meshData;
    public bool isSolid;
    public bool isWater;
    public bool renderNeighborFaces;
    public float opacity;
    public int blockDurability;

    [Header("Texture IDs")]
    public int topFaceTexture;
    public int frontFaceTexture;
    public int backFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;
    public int bottomFaceTexture;


    public int GetTextureID(int faceIndex)
    {
        switch (faceIndex)
        {
            case 0: return backFaceTexture;
            case 1: return frontFaceTexture;
            case 2: return topFaceTexture;
            case 3: return bottomFaceTexture;
            case 4: return leftFaceTexture;
            case 5: return rightFaceTexture;

            default:
                return 0;
        }

    }




}

public class VoxelMod
{
    public Vector3 position;
    public byte id;

    public VoxelMod()
    {
        position = new Vector3();
        id = 0;
    }

    public VoxelMod(Vector3 _position, byte _id)
    {
        position = _position;
        id = _id;
    }
}


[System.Serializable]
public class Settings
{
    [Header("Game Data")]
    public string version;


    [Header("Performance")]
    public int loadDistance = 16;
    public int viewDistance;
    public bool enableThreading;

    [Header("Controls")]
    [Range(0.1f, 10f)]
    public float mouseSensitivity;

    [Header("World Gen")]
    public int seed;
}
