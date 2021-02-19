using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    //base settings
    public bool autoUpdateMap;
    public int mapWidth;
    public int mapHeight;
    public int seed;
    public int contNum;
    public float landRatio;
    public float waterLevel;
    public bool useContinentHeights;

    //perlin noise settings
    public NoiseMap perlinMap;
    public NoiseMap ridgedMap;
    public NoiseMap valleyMap;

    //plate edge settings
    public int tectonicRadius;
    public int shoreSmoothingRadius;

    //arrays used for visiting the 4 directly adjacent tiles
    readonly int[] xAdd = new int[] { 1, 0, -1, 0 };             
    readonly int[] yAdd = new int[] { 0, 1, 0, -1 };

    float maxHeight = 0;
    float minHeight = 0;

    //arrays to store the info per tile
    public float[,] finalHeightArray;
    public int[,] contCoreDistArray;
    public int[,] continentIDsArray;
    public float[,] volcanismArray;

    public float[,] contHeightArray;

    Continent[] continents;

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public TerrainColor[] regions;

    public MapDisplay display;

    public GameObject water;

    public System.Random rnd;

    public void GenerateMap()
    {
        Initialize();
        CreateContinentHeightMap();

        CreatePerlinMap();
        CreateRidgedMap();
        CreateVelleyMap();

        CombineHeightMaps();


        RecalculateMinMax();

        float[,] heightMap = CreateHeightMap();
        Color[] colorMap = GenerateColorMap(heightMap);

        display.DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap), TextureGenerator.TextureFromColourMap(colorMap, mapWidth, mapHeight));
    }

    public void Initialize()
    {
        rnd = new System.Random(seed);

        water.gameObject.transform.localScale = new Vector3(-mapWidth / 10, 1, mapHeight / 10); //sets the water plane to the map size
        water.gameObject.transform.localPosition = new Vector3(water.gameObject.transform.localPosition.x, waterLevel, water.gameObject.transform.localPosition.z);

        finalHeightArray = new float[mapWidth, mapHeight];                //the main arrays for storing the tile data
        volcanismArray = new float[mapWidth, mapHeight];
        contCoreDistArray = new int[mapWidth, mapHeight];
        continentIDsArray = new int[mapWidth, mapHeight];
        contHeightArray = new float[mapWidth, mapHeight];

        for (int i = 0; i < mapWidth; i++)                           //sets the initial values for contIDs to int.maxvalue
        {
            for (int j = 0; j < mapHeight; j++)
            {
                continentIDsArray[i, j] = int.MaxValue;
            }
        }
        continents = new Continent[contNum];
    }

    public void CreateContinentHeightMap()
    {
        CreateContinents();
        CalculateBaseHeights();
        FixContinentEdges();
    }
    public void CreateContinents()
    {
        List<int> nextTiles = new List<int>();              //List of tiles to be processed by the while loop (stored as an int that describes its location)

        for (int i = 0; i < contNum;)                       //creates the starting seeds randomly for each continent and adds them to the list
        {
            int tempx = (int)(rnd.NextDouble() * mapWidth);
            int tempy = (int)(rnd.NextDouble() * mapHeight);
            bool isOcean = rnd.NextDouble() >= landRatio;
            Vector2 movement = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());

            if (continentIDsArray[tempx, tempy] == int.MaxValue)
            {
                Continent c = new Continent(i, movement, isOcean);
                continents[i] = c;
                continentIDsArray[tempx, tempy] = i;
                contCoreDistArray[tempx, tempy] = 0;
                nextTiles.Add(tempx + tempy * mapWidth);
                i++;
            }
        }

        while (nextTiles.Count > 0)
        {
            int temp = (int)(rnd.NextDouble() * nextTiles.Count);        //picks a random tile from the list
            int curTile = nextTiles[temp];
            nextTiles.RemoveAt(temp);                                   //and removes it from the list

            int curX = curTile % mapWidth;                              //gets the x and y of the tile
            int curY = curTile / mapWidth;

            int curContID = continentIDsArray[curX, curY];
            int curContCoreDist = contCoreDistArray[curX, curY];

            for (int i = 0; i < 4; i++) //loops through each of the 4 neighbour tiles
            {
                int newX = (((xAdd[i] + curX) % mapWidth) + mapWidth) % mapWidth; //mods it so the world loops around
                int newY = (((yAdd[i] + curY) % mapHeight) + mapHeight) % mapHeight;

                if (continentIDsArray[newX, newY] == int.MaxValue)    //if the neighbour is currently empty
                {
                    nextTiles.Add(newX + newY * mapWidth);  //add it to the list
                    continentIDsArray[newX, newY] = curContID;
                    contCoreDistArray[newX, newY] = curContCoreDist + 1;
                }
            }
        }
    }

    //gives continents and oceans their base heights
    public void CalculateBaseHeights()
    {
        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapHeight; j++)
            {
                Continent cont = continents[continentIDsArray[i, j]];
                if (cont.getIsOcean())
                {
                    contHeightArray[i, j] = -10;
                }
                else
                {
                    contHeightArray[i, j] = 10;
                }

            }
        }
    }

    //Adds Volcanism to continent edges and also fixes the jump from ocean to land plates
    public void FixContinentEdges()
    {
        List<int> nextTiles = new List<int>();
        int[,] distFromOcean = new int[mapWidth, mapHeight];

        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapHeight; j++)
            {
                int curID = continentIDsArray[i, j];

                for (int n = 0; n < 4; n++) //loops through each of the 4 neighbour tiles
                {
                    int newX = (((xAdd[n] + i) % mapWidth) + mapWidth) % mapWidth; //mods it so the world loops around
                    int newY = (((yAdd[n] + j) % mapHeight) + mapHeight) % mapHeight;

                    if (continentIDsArray[newX, newY] != curID)
                    {
                        //if a land plate is next to an ocean one, smooth out the transition
                        if (continents[continentIDsArray[newX, newY]].isOcean != continents[continentIDsArray[i, j]].isOcean)
                        {
                            if(!continents[continentIDsArray[i, j]].isOcean) 
                            {
                                if (!nextTiles.Contains(i + j * mapWidth))
                                {
                                    nextTiles.Add(i + j * mapWidth);
                                    distFromOcean[i, j] = 1;
                                    contHeightArray[i, j] = 0;
                                }
                            }
                        }
                        else
                        {
                            //adds volcanism
                            for (int p = -tectonicRadius; p <= tectonicRadius; p++)
                            {
                                for (int q = -tectonicRadius + Mathf.Abs(p); q <= tectonicRadius - Mathf.Abs(p); q++)
                                {
                                    int x = (((newX + p) % mapWidth) + mapWidth) % mapWidth;
                                    int y = (((newY + q) % mapHeight) + mapHeight) % mapHeight;

                                    volcanismArray[x, y] += (1f / (Mathf.Abs(p) + Mathf.Abs(q) + 1));
                                }
                            }
                        }
                    }
                }
            }
        }
        while (nextTiles.Count > 0)
        {
            int curTile = nextTiles[0];
            nextTiles.RemoveAt(0);                                   //and removes it from the list

            int curX = curTile % mapWidth;                              //gets the x and y of the tile
            int curY = curTile / mapWidth;

            for (int i = 0; i < 4; i++) //loops through each of the 4 neighbour tiles
            {
                int newX = (((xAdd[i] + curX) % mapWidth) + mapWidth) % mapWidth; //mods it so the world loops around
                int newY = (((yAdd[i] + curY) % mapHeight) + mapHeight) % mapHeight;

                if (distFromOcean[newX,newY] == 0 && distFromOcean[curX, curY] < shoreSmoothingRadius)  
                {
                    nextTiles.Add(newX + newY * mapWidth);  //add it to the list
                    distFromOcean[newX, newY] = distFromOcean[curX, curY] + 1;
                    float h = Mathf.Lerp(0, contHeightArray[newX, newY], Mathf.InverseLerp(0, shoreSmoothingRadius, distFromOcean[curX, curY]));
                    contHeightArray[newX, newY] = h;
                }
            }
        }
    }

    public void CreatePerlinMap()
    {
        perlinMap.HeightArray = Noise.GenerateNoiseMap(mapWidth, mapHeight, rnd.Next(), perlinMap.Scale, perlinMap.Octaves, perlinMap.Persistance, perlinMap.Lacunarity, Vector2.zero, NoiseMapType.perlin);
    }

    public void CreateRidgedMap()
    {
        ridgedMap.HeightArray = Noise.GenerateNoiseMap(mapWidth, mapHeight, rnd.Next(), ridgedMap.Scale, ridgedMap.Octaves, ridgedMap.Persistance, ridgedMap.Lacunarity, Vector2.zero, NoiseMapType.ridged);
    }

    public void CreateVelleyMap()
    {
        valleyMap.HeightArray = Noise.GenerateNoiseMap(mapWidth, mapHeight, rnd.Next(), valleyMap.Scale, valleyMap.Octaves, valleyMap.Persistance, valleyMap.Lacunarity, Vector2.zero, NoiseMapType.valley);
    }

    public void CombineHeightMaps()
    {
        List<NoiseMap> heightMaps = new List<NoiseMap>();
        heightMaps.Add(ridgedMap);
        heightMaps.Add(perlinMap);
        heightMaps.Add(valleyMap);

        List<NoiseMap> heightMapsToAdd = new List<NoiseMap>();
        List<NoiseMap> heightMapsForVolcanism = new List<NoiseMap>();

        foreach (NoiseMap map in heightMaps)
        {
            if (map.useEverywhere)
            {
                heightMapsToAdd.Add(map);
            }
            if (map.addOnVolcanism)
            {
                heightMapsForVolcanism.Add(map);
            }
        }


        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapHeight; j++)
            {
                if (useContinentHeights)
                {
                    finalHeightArray[i,j] += contHeightArray[i, j];
                }
                foreach (NoiseMap map in heightMapsToAdd)
                {
                    finalHeightArray[i, j] += map.HeightArray[i, j] * map.Influence;
                }
                foreach (NoiseMap map in heightMapsForVolcanism)
                {
                    finalHeightArray[i, j] += (map.HeightArray[i, j]+1) * map.volcanicInfluence * volcanismArray[i,j]/10f;
                }
            }
        }
    }
    public void RecalculateMinMax()
    {
        maxHeight = 0;
        minHeight = 0;
        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapHeight; j++)
            {
                if (finalHeightArray[i, j] > maxHeight)
                {
                    maxHeight = finalHeightArray[i, j];
                }
                if (finalHeightArray[i, j] < minHeight)
                {
                    minHeight = finalHeightArray[i, j];
                }
            }
        }
    }
    public float[,] CreateHeightMap()
    { 
        float[,] result = new float[mapWidth, mapHeight];

        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapHeight; j++)
            {
                //result[i, j] = Mathf.InverseLerp(minHeight, maxHeight, finalHeightArray[i, j])*20;
                result[i, j] = finalHeightArray[i, j];
            }
        }
        return result;
    }
    public Color[] GenerateColorMap(float[,] noiseMap)
    {
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        Color[] colorMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int k = 0; k < regions.Length; k++)
                {
                    if (noiseMap[x, y] <= regions[k].height)
                    {
                        colorMap[y * width + x] = regions[k].color;
                        break;
                    }
                }
            }
        }
        return colorMap;
    }
}
[System.Serializable]
public struct TerrainColor
{
    public Color color;
    public float height;
}

[System.Serializable]
public class NoiseMap
{
    public float Scale;
    public int Octaves;
    public float Persistance;

    [Range(1f, 3f)]
    public float Lacunarity;
    public float Influence;
    public float volcanicInfluence;
    public bool useEverywhere;
    public bool addOnVolcanism;

    public float[,] HeightArray;
}